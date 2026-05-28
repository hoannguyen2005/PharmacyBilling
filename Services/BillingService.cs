using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PharmacyBillingService.Data;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Events;
using PharmacyBillingService.Models;

namespace PharmacyBillingService.Services
{
    public interface IBillingService
    {
        Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createDto);
        Task<List<InvoiceDto>> GetAllInvoicesAsync(string? status);
        Task<InvoiceDto?> GetInvoiceByIdAsync(int id);
        Task<List<InvoiceDto>> GetInvoicesByPatientIdAsync(int patientId);
        Task<InvoiceDto> PayInvoiceAsync(int invoiceId, PayInvoiceDto payDto, int userId);
        Task<bool> CancelInvoiceAsync(int invoiceId);
    }

    public class BillingService : IBillingService
    {
        private readonly PharmacyDbContext _context;
        private readonly IEventPublisher _eventPublisher;

        public BillingService(PharmacyDbContext context, IEventPublisher eventPublisher)
        {
            _context = context;
            _eventPublisher = eventPublisher;
        }

        public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createDto)
        {
            // BR17: Không tạo hóa đơn trùng cho cùng một đơn thuốc (trừ khi hóa đơn cũ đã bị Hủy)
            var existingInvoice = await _context.Invoices
                .Include(i => i.Payments)
                .Include(i => i.Prescription)
                .ThenInclude(p => p!.PrescriptionItems)
                .FirstOrDefaultAsync(i => i.PrescriptionId == createDto.PrescriptionId && i.Status != "Cancelled");

            if (existingInvoice != null)
            {
                return MapToInvoiceDto(existingInvoice);
            }

            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .FirstOrDefaultAsync(p => p.PrescriptionId == createDto.PrescriptionId);

            if (prescription == null)
            {
                throw new ArgumentException($"Không tìm thấy đơn thuốc có ID = {createDto.PrescriptionId}");
            }

            // BR12: Giá thuốc trong hóa đơn phải lưu cố định tại thời điểm tạo hóa đơn (đã được lưu tại PrescriptionItems.UnitPrice khi nhận đơn)
            decimal medicineTotal = prescription.PrescriptionItems.Sum(pi => pi.TotalPrice);
            
            // BR11: Tổng tiền hóa đơn = phí khám + tổng tiền thuốc
            decimal totalAmount = createDto.ExaminationFee + medicineTotal;

            var invoice = new Invoice
            {
                PatientId = prescription.PatientId,
                AppointmentId = prescription.AppointmentId,
                PrescriptionId = prescription.PrescriptionId,
                ExaminationFee = createDto.ExaminationFee,
                MedicineTotal = medicineTotal,
                TotalAmount = totalAmount,
                Status = "Unpaid", // Ban đầu là Unpaid
                CreatedAt = DateTime.UtcNow
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Load đầy đủ thông tin để trả về DTO
            var savedInvoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstAsync(i => i.InvoiceId == invoice.InvoiceId);

            return MapToInvoiceDto(savedInvoice);
        }

        public async Task<List<InvoiceDto>> GetAllInvoicesAsync(string? status)
        {
            var query = _context.Invoices
                .Include(i => i.Payments)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(i => i.Status == status);
            }

            var invoices = await query
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return invoices.Select(MapToInvoiceDto).ToList();
        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null) return null;
            return MapToInvoiceDto(invoice);
        }

        public async Task<List<InvoiceDto>> GetInvoicesByPatientIdAsync(int patientId)
        {
            var invoices = await _context.Invoices
                .Include(i => i.Payments)
                .Where(i => i.PatientId == patientId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return invoices.Select(MapToInvoiceDto).ToList();
        }

        public async Task<InvoiceDto> PayInvoiceAsync(int invoiceId, PayInvoiceDto payDto, int userId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
            {
                throw new ArgumentException("Không tìm thấy hóa đơn yêu cầu.");
            }

            // BR08: Hóa đơn đã hủy không được thanh toán
            if (invoice.Status == "Cancelled")
            {
                throw new InvalidOperationException("Không được thanh toán hóa đơn đã hủy.");
            }

            // BR08: Chỉ hóa đơn Unpaid mới được thanh toán
            if (invoice.Status == "Paid")
            {
                throw new InvalidOperationException("Hóa đơn đã được thanh toán từ trước.");
            }

            // BR09: Thanh toán thành công phải tạo bản ghi Payment
            var payment = new Payment
            {
                InvoiceId = invoiceId,
                Amount = invoice.TotalAmount,
                PaymentMethod = payDto.PaymentMethod,
                PaymentStatus = "Success",
                PaidBy = userId,
                PaidAt = DateTime.UtcNow,
                Note = payDto.Note ?? "Thanh toán hóa đơn viện phí"
            };

            _context.Payments.Add(payment);

            // Cập nhật hóa đơn
            invoice.Status = "Paid";
            invoice.PaidAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Publish event
            await _eventPublisher.PublishAsync("invoice.paid", new InvoicePaidEvent
            {
                InvoiceId = invoice.InvoiceId,
                PatientId = invoice.PatientId,
                TotalAmount = invoice.TotalAmount,
                PaymentMethod = payDto.PaymentMethod,
                PaidAt = invoice.PaidAt.Value
            });

            // Sau khi hóa đơn được trả, hãy cập nhật trạng thái đơn thuốc sang ReadyToDispense (nếu trước đó là Pending/OutOfStock nhưng nay đủ thuốc)
            // Hoặc giữ nguyên trạng thái. (Chúng ta kiểm tra tồn kho để cập nhật lại nếu cần).
            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .FirstOrDefaultAsync(p => p.PrescriptionId == invoice.PrescriptionId);
            
            if (prescription != null && prescription.Status != "Dispensed")
            {
                // Kiểm tra lại tồn kho
                bool allAvailable = true;
                foreach (var item in prescription.PrescriptionItems)
                {
                    var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                    if (medicine == null || medicine.StockQuantity < item.Quantity)
                    {
                        allAvailable = false;
                        break;
                    }
                }

                if (allAvailable)
                {
                    prescription.Status = "ReadyToDispense";
                }
                else
                {
                    prescription.Status = "OutOfStock";
                }
                await _context.SaveChangesAsync();
            }

            return MapToInvoiceDto(invoice);
        }

        public async Task<bool> CancelInvoiceAsync(int invoiceId)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) return false;

            // Hóa đơn đã thanh toán không được hủy (Rule 21: BR12 - Hủy hóa đơn đã thanh toán cần quy trình hoàn tiền, ở đây chỉ cho phép hủy Unpaid/Draft)
            if (invoice.Status == "Paid")
            {
                throw new InvalidOperationException("Không thể hủy hóa đơn đã thanh toán.");
            }

            invoice.Status = "Cancelled";
            await _context.SaveChangesAsync();
            return true;
        }

        private static InvoiceDto MapToInvoiceDto(Invoice invoice)
        {
            return new InvoiceDto
            {
                InvoiceId = invoice.InvoiceId,
                PatientId = invoice.PatientId,
                AppointmentId = invoice.AppointmentId,
                PrescriptionId = invoice.PrescriptionId,
                ExaminationFee = invoice.ExaminationFee,
                MedicineTotal = invoice.MedicineTotal,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status,
                CreatedAt = invoice.CreatedAt,
                PaidAt = invoice.PaidAt,
                Payments = invoice.Payments.Select(p => new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    InvoiceId = p.InvoiceId,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    PaymentStatus = p.PaymentStatus,
                    PaidBy = p.PaidBy,
                    PaidAt = p.PaidAt,
                    Note = p.Note
                }).ToList()
            };
        }
    }
}
