using System;

namespace PharmacyBillingService.DTOs
{
    public class RevenueDailyDto
    {
        public DateTime Date { get; set; }
        public decimal TotalRevenue { get; set; }
        public int InvoiceCount { get; set; }
    }

    public class RevenueMonthlyDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalRevenue { get; set; }
        public int InvoiceCount { get; set; }
    }

    public class TopMedicineDto
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class LowStockReportDto
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
