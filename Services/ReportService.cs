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
    public interface IReportService
    {
        Task<List<RevenueDailyDto>> GetDailyRevenueAsync(int daysCount);
        Task<List<RevenueMonthlyDto>> GetMonthlyRevenueAsync(int monthsCount);
        Task<List<TopMedicineDto>> GetTopMedicinesAsync(int count);
        Task<List<InvoiceDto>> GetUnpaidInvoicesAsync();
        Task<List<LowStockReportDto>> GetLowStockReportAsync();
    }

    public class ReportService : IReportService
    {
        private readonly PharmacyDbContext _context;

        public ReportService(PharmacyDbContext context)
        {
            _context = context;
        }

        public async Task<List<RevenueDailyDto>> GetDailyRevenueAsync(int daysCount)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-daysCount);

            // Nhóm theo ngày thanh toán của các hóa đơn đã thanh toán (Paid)
            var paidInvoices = await _context.Invoices
                .Where(i => i.Status == "Paid" && i.PaidAt != null && i.PaidAt >= startDate)
                .ToListAsync();

            var result = paidInvoices
                .GroupBy(i => i.PaidAt!.Value.Date)
                .Select(g => new RevenueDailyDto
                {
                    Date = g.Key,
                    TotalRevenue = g.Sum(i => i.TotalAmount),
                    InvoiceCount = g.Count()
                })
                .OrderByDescending(r => r.Date)
                .ToList();

            return result;
        }

        public async Task<List<RevenueMonthlyDto>> GetMonthlyRevenueAsync(int monthsCount)
        {
            var startDate = DateTime.UtcNow.Date.AddMonths(-monthsCount);

            var paidInvoices = await _context.Invoices
                .Where(i => i.Status == "Paid" && i.PaidAt != null && i.PaidAt >= startDate)
                .ToListAsync();

            var result = paidInvoices
                .GroupBy(i => new { i.PaidAt!.Value.Month, i.PaidAt.Value.Year })
                .Select(g => new RevenueMonthlyDto
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    TotalRevenue = g.Sum(i => i.TotalAmount),
                    InvoiceCount = g.Count()
                })
                .OrderByDescending(r => r.Year)
                .ThenByDescending(r => r.Month)
                .ToList();

            return result;
        }

        public async Task<List<TopMedicineDto>> GetTopMedicinesAsync(int count)
        {
            // Thống kê các loại thuốc đã được cấp phát qua các đơn thuốc "Dispensed"
            var items = await _context.PrescriptionItems
                .Include(pi => pi.Prescription)
                .Where(pi => pi.Prescription!.Status == "Dispensed")
                .ToListAsync();

            var result = items
                .GroupBy(pi => new { pi.MedicineId, pi.MedicineName })
                .Select(g => new TopMedicineDto
                {
                    MedicineId = g.Key.MedicineId,
                    MedicineName = g.Key.MedicineName,
                    QuantitySold = g.Sum(pi => pi.Quantity),
                    TotalRevenue = g.Sum(pi => pi.TotalPrice)
                })
                .OrderByDescending(r => r.QuantitySold)
                .Take(count)
                .ToList();

            return result;
        }

        public async Task<List<InvoiceDto>> GetUnpaidInvoicesAsync()
        {
            var invoices = await _context.Invoices
                .Include(i => i.Payments)
                .Where(i => i.Status == "Unpaid")
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();

            return invoices.Select(MapToInvoiceDto).ToList();
        }

        public async Task<List<LowStockReportDto>> GetLowStockReportAsync()
        {
            var medicines = await _context.Medicines
                .Where(m => m.StockQuantity <= m.MinStockLevel && m.Status != "Inactive")
                .ToListAsync();

            return medicines.Select(m => new LowStockReportDto
            {
                MedicineId = m.MedicineId,
                MedicineName = m.MedicineName,
                StockQuantity = m.StockQuantity,
                MinStockLevel = m.MinStockLevel,
                Status = m.Status
            }).ToList();
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
