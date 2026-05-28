using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PharmacyBillingService.Data;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Models;

namespace PharmacyBillingService.Services
{
    public interface IMedicineService
    {
        Task<List<MedicineDto>> GetAllMedicinesAsync(string? searchName, string? searchActiveIngredient, string? status, int page = 1, int pageSize = 20);
        Task<MedicineDto?> GetMedicineByIdAsync(int id);
        Task<MedicineDto> CreateMedicineAsync(CreateMedicineDto createDto);
        Task<MedicineDto?> UpdateMedicineAsync(int id, UpdateMedicineDto updateDto);
        Task<bool> DeleteMedicineAsync(int id);
        Task<List<MedicineDto>> GetLowStockMedicinesAsync();
        Task<List<MedicineDto>> GetExpiredMedicinesAsync();
    }

    public class MedicineService : IMedicineService
    {
        private readonly PharmacyDbContext _context;

        public MedicineService(PharmacyDbContext context)
        {
            _context = context;
        }

        public async Task<List<MedicineDto>> GetAllMedicinesAsync(string? searchName, string? searchActiveIngredient, string? status, int page = 1, int pageSize = 20)
        {
            var query = _context.Medicines.AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(m => m.MedicineName.Contains(searchName));
            }

            if (!string.IsNullOrEmpty(searchActiveIngredient))
            {
                query = query.Where(m => m.ActiveIngredient != null && m.ActiveIngredient.Contains(searchActiveIngredient));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(m => m.Status == status);
            }

            var medicines = await query
                .OrderBy(m => m.MedicineName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return medicines.Select(MapToMedicineDto).ToList();
        }

        public async Task<MedicineDto?> GetMedicineByIdAsync(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return null;
            return MapToMedicineDto(medicine);
        }

        public async Task<MedicineDto> CreateMedicineAsync(CreateMedicineDto createDto)
        {
            var status = createDto.StockQuantity == 0 ? "OutOfStock" : "Active";
            
            var medicine = new Medicine
            {
                MedicineName = createDto.MedicineName,
                ActiveIngredient = createDto.ActiveIngredient,
                Unit = createDto.Unit,
                Price = createDto.Price,
                StockQuantity = createDto.StockQuantity,
                MinStockLevel = createDto.MinStockLevel,
                ExpiryDate = createDto.ExpiryDate,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();

            return MapToMedicineDto(medicine);
        }

        public async Task<MedicineDto?> UpdateMedicineAsync(int id, UpdateMedicineDto updateDto)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return null;

            medicine.MedicineName = updateDto.MedicineName;
            medicine.ActiveIngredient = updateDto.ActiveIngredient;
            medicine.Unit = updateDto.Unit;
            medicine.Price = updateDto.Price;
            medicine.StockQuantity = updateDto.StockQuantity;
            medicine.MinStockLevel = updateDto.MinStockLevel;
            medicine.ExpiryDate = updateDto.ExpiryDate;
            
            // BR14: Thuốc có StockQuantity = 0 chuyển trạng thái OutOfStock
            if (medicine.StockQuantity == 0)
            {
                medicine.Status = "OutOfStock";
            }
            else
            {
                medicine.Status = updateDto.Status;
            }

            medicine.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToMedicineDto(medicine);
        }

        public async Task<bool> DeleteMedicineAsync(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return false;

            // BR16: Không được xóa cứng thuốc đã phát sinh đơn thuốc
            var hasPrescriptionItems = await _context.PrescriptionItems.AnyAsync(pi => pi.MedicineId == id);
            if (hasPrescriptionItems)
            {
                // Thay vì xóa cứng, ta chuyển trạng thái sang Inactive
                medicine.Status = "Inactive";
                medicine.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true; // Trả về true biểu thị đã vô hiệu hóa thành công
            }

            _context.Medicines.Remove(medicine);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<MedicineDto>> GetLowStockMedicinesAsync()
        {
            // BR15: Khi tồn kho dưới MinStockLevel, hệ thống cần cảnh báo
            var medicines = await _context.Medicines
                .Where(m => m.StockQuantity <= m.MinStockLevel && m.Status != "Inactive")
                .ToListAsync();

            return medicines.Select(MapToMedicineDto).ToList();
        }

        public async Task<List<MedicineDto>> GetExpiredMedicinesAsync()
        {
            var today = DateTime.Today;
            var medicines = await _context.Medicines
                .Where(m => m.ExpiryDate != null && m.ExpiryDate <= today && m.Status != "Inactive")
                .ToListAsync();

            return medicines.Select(MapToMedicineDto).ToList();
        }

        private static MedicineDto MapToMedicineDto(Medicine medicine)
        {
            return new MedicineDto
            {
                MedicineId = medicine.MedicineId,
                MedicineName = medicine.MedicineName,
                ActiveIngredient = medicine.ActiveIngredient,
                Unit = medicine.Unit,
                Price = medicine.Price,
                StockQuantity = medicine.StockQuantity,
                MinStockLevel = medicine.MinStockLevel,
                ExpiryDate = medicine.ExpiryDate,
                Status = medicine.Status,
                CreatedAt = medicine.CreatedAt,
                UpdatedAt = medicine.UpdatedAt
            };
        }
    }
}
