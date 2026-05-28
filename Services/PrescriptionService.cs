using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PharmacyBillingService.Data;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Events;
using PharmacyBillingService.Models;

namespace PharmacyBillingService.Services
{
    public interface IPrescriptionService
    {
        Task<PrescriptionDto> ProcessPrescriptionCreatedEventAsync(PrescriptionCreatedEvent ev);
        Task<List<PrescriptionDto>> GetAllPrescriptionsAsync(string? status);
        Task<PrescriptionDto?> GetPrescriptionByIdAsync(int id);
        Task<List<PrescriptionDto>> GetPrescriptionsByPatientIdAsync(int patientId);
        Task<bool> DispensePrescriptionAsync(int prescriptionId, int userId);
    }

    public class PrescriptionService : IPrescriptionService
    {
        private readonly PharmacyDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<PrescriptionService> _logger;

        public PrescriptionService(PharmacyDbContext context, IEventPublisher eventPublisher, ILogger<PrescriptionService> _logger)
        {
            _context = context;
            _eventPublisher = eventPublisher;
            this._logger = _logger;
        }

        public async Task<PrescriptionDto> ProcessPrescriptionCreatedEventAsync(PrescriptionCreatedEvent ev)
        {
            // Rule 22: Tránh trùng lặp - Kiểm tra PrescriptionId trước khi insert
            var existing = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .FirstOrDefaultAsync(p => p.PrescriptionId == ev.PrescriptionId);

            if (existing != null)
            {
                _logger.LogWarning("Event trùng lặp: Đơn thuốc có mã {Id} đã được nhận từ trước.", ev.PrescriptionId);
                return MapToPrescriptionDto(existing);
            }

            var prescription = new Prescription
            {
                PrescriptionId = ev.PrescriptionId,
                PatientId = ev.PatientId,
                DoctorId = ev.DoctorId,
                AppointmentId = ev.AppointmentId,
                MedicalRecordId = ev.MedicalRecordId,
                CreatedAt = ev.CreatedAt != default ? ev.CreatedAt : DateTime.UtcNow,
                Status = "Pending" // Sẽ được tính toán lại ngay sau đây
            };

            var itemsToCreate = new List<PrescriptionItem>();
            bool allAvailable = true;
            bool anyAvailable = false;

            foreach (var item in ev.Items)
            {
                var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                if (medicine == null)
                {
                    throw new ArgumentException($"Không tìm thấy thuốc có ID = {item.MedicineId} trong hệ thống.");
                }

                decimal priceSnapshot = medicine.Price; // Snapshot price

                var pi = new PrescriptionItem
                {
                    PrescriptionId = ev.PrescriptionId,
                    MedicineId = item.MedicineId,
                    MedicineName = medicine.MedicineName,
                    Quantity = item.Quantity,
                    Dosage = item.Dosage,
                    UnitPrice = priceSnapshot,
                    TotalPrice = item.Quantity * priceSnapshot
                };
                itemsToCreate.Add(pi);

                // Kiểm tra lượng tồn kho của thuốc
                if (medicine.StockQuantity < item.Quantity)
                {
                    allAvailable = false;
                }
                else
                {
                    anyAvailable = true;
                }
            }

            // Xác định trạng thái đơn thuốc
            if (allAvailable)
            {
                prescription.Status = "ReadyToDispense";
            }
            else if (anyAvailable)
            {
                prescription.Status = "PartiallyAvailable";
            }
            else
            {
                prescription.Status = "OutOfStock";
            }

            _context.Prescriptions.Add(prescription);
            _context.PrescriptionItems.AddRange(itemsToCreate);
            await _context.SaveChangesAsync();

            // Load đầy đủ items để trả về
            var savedPrescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .FirstAsync(p => p.PrescriptionId == ev.PrescriptionId);

            return MapToPrescriptionDto(savedPrescription);
        }

        public async Task<List<PrescriptionDto>> GetAllPrescriptionsAsync(string? status)
        {
            var query = _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var prescriptions = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return prescriptions.Select(MapToPrescriptionDto).ToList();
        }

        public async Task<PrescriptionDto?> GetPrescriptionByIdAsync(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);

            if (prescription == null) return null;
            return MapToPrescriptionDto(prescription);
        }

        public async Task<List<PrescriptionDto>> GetPrescriptionsByPatientIdAsync(int patientId)
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return prescriptions.Select(MapToPrescriptionDto).ToList();
        }

        public async Task<bool> DispensePrescriptionAsync(int prescriptionId, int userId)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

            if (prescription == null)
            {
                throw new ArgumentException("Không tìm thấy đơn thuốc yêu cầu.");
            }

            // BR06: Một đơn thuốc chỉ được xuất một lần
            if (prescription.Status == "Dispensed")
            {
                throw new InvalidOperationException("Đơn thuốc đã được xuất từ trước.");
            }

            // Kiểm tra hóa đơn của đơn thuốc đã được thanh toán chưa?
            // Lưu ý: Quy trình nghiệp vụ: Kê đơn -> Tạo hóa đơn viện phí -> Tiếp tân thu tiền -> Xác nhận thanh toán -> Xuất thuốc
            // Hãy kiểm tra xem có hóa đơn nào liên kết và đã Paid chưa
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.PrescriptionId == prescriptionId);
            if (invoice != null && invoice.Status != "Paid")
            {
                throw new InvalidOperationException("Hóa đơn viện phí liên quan chưa được thanh toán.");
            }

            // Thực hiện kiểm tra toàn bộ tồn kho và hạn dùng trước khi xuất
            var today = DateTime.Today;
            foreach (var item in prescription.PrescriptionItems)
            {
                var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                if (medicine == null)
                {
                    throw new InvalidOperationException($"Thuốc '{item.MedicineName}' (ID {item.MedicineId}) đã bị xóa khỏi danh mục.");
                }

                // BR04: Không cho xuất thuốc nếu tồn kho không đủ
                if (medicine.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Không đủ tồn kho cho thuốc '{item.MedicineName}'. Hiện có: {medicine.StockQuantity}, Cần: {item.Quantity}");
                }

                // BR05: Không cho xuất thuốc hết hạn
                if (medicine.ExpiryDate != null && medicine.ExpiryDate <= today)
                {
                    throw new InvalidOperationException($"Thuốc '{item.MedicineName}' đã hết hạn sử dụng ({medicine.ExpiryDate:dd/MM/yyyy}).");
                }
            }

            // Nếu tất cả hợp lệ, tiến hành trừ kho và lưu nhật ký kho
            foreach (var item in prescription.PrescriptionItems)
            {
                var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                if (medicine != null)
                {
                    int beforeQty = medicine.StockQuantity;
                    int afterQty = beforeQty - item.Quantity;

                    medicine.StockQuantity = afterQty;
                    medicine.UpdatedAt = DateTime.UtcNow;

                    // BR14: Thuốc có StockQuantity = 0 chuyển OutOfStock
                    if (afterQty == 0)
                    {
                        medicine.Status = "OutOfStock";
                    }

                    // BR10: Tạo StockTransaction khi xuất kho
                    var transaction = new StockTransaction
                    {
                        MedicineId = medicine.MedicineId,
                        Type = "Export",
                        Quantity = item.Quantity,
                        BeforeQuantity = beforeQty,
                        AfterQuantity = afterQty,
                        Reason = $"Xuất thuốc theo đơn #{prescriptionId}",
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.StockTransactions.Add(transaction);

                    // Publish stock update event
                    await _eventPublisher.PublishAsync("medicine.stock.updated", new MedicineStockUpdatedEvent
                    {
                        MedicineId = medicine.MedicineId,
                        BeforeQuantity = beforeQty,
                        AfterQuantity = afterQty,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            // Cập nhật trạng thái đơn thuốc
            prescription.Status = "Dispensed";
            await _context.SaveChangesAsync();

            // Publish medicine dispensed event
            await _eventPublisher.PublishAsync("medicine.dispensed", new MedicineDispensedEvent
            {
                PrescriptionId = prescriptionId,
                PatientId = prescription.PatientId,
                DispensedAt = DateTime.UtcNow
            });

            return true;
        }

        private static PrescriptionDto MapToPrescriptionDto(Prescription p)
        {
            return new PrescriptionDto
            {
                PrescriptionId = p.PrescriptionId,
                PatientId = p.PatientId,
                DoctorId = p.DoctorId,
                AppointmentId = p.AppointmentId,
                MedicalRecordId = p.MedicalRecordId,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                PrescriptionItems = p.PrescriptionItems.Select(pi => new PrescriptionItemDto
                {
                    PrescriptionItemId = pi.PrescriptionItemId,
                    PrescriptionId = pi.PrescriptionId,
                    MedicineId = pi.MedicineId,
                    MedicineName = pi.MedicineName,
                    Quantity = pi.Quantity,
                    Dosage = pi.Dosage,
                    UnitPrice = pi.UnitPrice,
                    TotalPrice = pi.TotalPrice
                }).ToList()
            };
        }
    }
}
