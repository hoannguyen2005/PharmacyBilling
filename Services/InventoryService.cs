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
    public interface IInventoryService
    {
        Task<StockTransactionDto> ImportStockAsync(StockImportDto importDto, int userId);
        Task<StockTransactionDto> AdjustStockAsync(StockAdjustDto adjustDto, int userId);
        Task<List<StockTransactionDto>> GetTransactionsAsync();
        Task<List<StockTransactionDto>> GetTransactionsByMedicineIdAsync(int medicineId);
    }

    public class InventoryService : IInventoryService
    {
        private readonly PharmacyDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(PharmacyDbContext context, IEventPublisher eventPublisher, ILogger<InventoryService> logger)
        {
            _context = context;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task<StockTransactionDto> ImportStockAsync(StockImportDto importDto, int userId)
        {
            var medicine = await _context.Medicines.FindAsync(importDto.MedicineId);
            if (medicine == null)
            {
                throw new ArgumentException("Không tìm thấy thuốc yêu cầu");
            }

            int beforeQty = medicine.StockQuantity;
            int afterQty = beforeQty + importDto.Quantity;

            // BR13: Không cho tồn kho âm
            if (afterQty < 0)
            {
                throw new InvalidOperationException("Không cho phép tồn kho âm.");
            }

            medicine.StockQuantity = afterQty;
            medicine.UpdatedAt = DateTime.UtcNow;

            // BR14: Thuốc có StockQuantity = 0 chuyển trạng thái OutOfStock, ngược lại Active nếu đang OutOfStock
            if (medicine.StockQuantity == 0)
            {
                medicine.Status = "OutOfStock";
            }
            else if (medicine.Status == "OutOfStock")
            {
                medicine.Status = "Active";
            }

            // BR15: Khi tồn kho dưới MinStockLevel, hệ thống cần cảnh báo
            if (medicine.StockQuantity <= medicine.MinStockLevel)
            {
                _logger.LogWarning("CẢNH BÁO TỒN KHO THẤP: Thuốc '{Name}' hiện có {Qty} đơn vị, dưới mức tối thiểu {Min}", medicine.MedicineName, medicine.StockQuantity, medicine.MinStockLevel);
            }

            // Tạo StockTransaction (BR10)
            var transaction = new StockTransaction
            {
                MedicineId = medicine.MedicineId,
                Type = "Import",
                Quantity = importDto.Quantity,
                BeforeQuantity = beforeQty,
                AfterQuantity = afterQty,
                Reason = importDto.Reason ?? "Nhập thêm thuốc vào kho",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Publish event
            await _eventPublisher.PublishAsync("medicine.stock.updated", new MedicineStockUpdatedEvent
            {
                MedicineId = medicine.MedicineId,
                BeforeQuantity = beforeQty,
                AfterQuantity = afterQty,
                UpdatedAt = DateTime.UtcNow
            });

            return MapToTransactionDto(transaction, medicine.MedicineName);
        }

        public async Task<StockTransactionDto> AdjustStockAsync(StockAdjustDto adjustDto, int userId)
        {
            var medicine = await _context.Medicines.FindAsync(adjustDto.MedicineId);
            if (medicine == null)
            {
                throw new ArgumentException("Không tìm thấy thuốc yêu cầu");
            }

            int beforeQty = medicine.StockQuantity;
            int afterQty = adjustDto.NewQuantity;
            int diffQty = Math.Abs(afterQty - beforeQty);

            if (diffQty == 0)
            {
                throw new InvalidOperationException("Số lượng tồn kho mới bằng tồn kho cũ. Không có gì thay đổi.");
            }

            // BR13: Không cho tồn kho âm
            if (afterQty < 0)
            {
                throw new InvalidOperationException("Không cho phép tồn kho âm.");
            }

            medicine.StockQuantity = afterQty;
            medicine.UpdatedAt = DateTime.UtcNow;

            // BR14: Thuốc có StockQuantity = 0 chuyển trạng thái OutOfStock
            if (medicine.StockQuantity == 0)
            {
                medicine.Status = "OutOfStock";
            }
            else if (medicine.Status == "OutOfStock")
            {
                medicine.Status = "Active";
            }

            // BR15: Khi tồn kho dưới MinStockLevel, hệ thống cần cảnh báo
            if (medicine.StockQuantity <= medicine.MinStockLevel)
            {
                _logger.LogWarning("CẢNH BÁO TỒN KHO THẤP: Thuốc '{Name}' hiện có {Qty} đơn vị, dưới mức tối thiểu {Min}", medicine.MedicineName, medicine.StockQuantity, medicine.MinStockLevel);
            }

            // Tạo StockTransaction (BR10)
            var transaction = new StockTransaction
            {
                MedicineId = medicine.MedicineId,
                Type = "Adjust",
                Quantity = diffQty,
                BeforeQuantity = beforeQty,
                AfterQuantity = afterQty,
                Reason = adjustDto.Reason ?? $"Điều chỉnh kho bởi người dùng {userId}",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Publish event
            await _eventPublisher.PublishAsync("medicine.stock.updated", new MedicineStockUpdatedEvent
            {
                MedicineId = medicine.MedicineId,
                BeforeQuantity = beforeQty,
                AfterQuantity = afterQty,
                UpdatedAt = DateTime.UtcNow
            });

            return MapToTransactionDto(transaction, medicine.MedicineName);
        }

        public async Task<List<StockTransactionDto>> GetTransactionsAsync()
        {
            var transactions = await _context.StockTransactions
                .Include(t => t.Medicine)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return transactions.Select(t => MapToTransactionDto(t, t.Medicine?.MedicineName ?? "N/A")).ToList();
        }

        public async Task<List<StockTransactionDto>> GetTransactionsByMedicineIdAsync(int medicineId)
        {
            var transactions = await _context.StockTransactions
                .Include(t => t.Medicine)
                .Where(t => t.MedicineId == medicineId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return transactions.Select(t => MapToTransactionDto(t, t.Medicine?.MedicineName ?? "N/A")).ToList();
        }

        private static StockTransactionDto MapToTransactionDto(StockTransaction t, string medicineName)
        {
            return new StockTransactionDto
            {
                TransactionId = t.TransactionId,
                MedicineId = t.MedicineId,
                MedicineName = medicineName,
                Type = t.Type,
                Quantity = t.Quantity,
                BeforeQuantity = t.BeforeQuantity,
                AfterQuantity = t.AfterQuantity,
                Reason = t.Reason,
                CreatedBy = t.CreatedBy,
                CreatedAt = t.CreatedAt
            };
        }
    }
}
