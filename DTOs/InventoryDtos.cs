using System;
using System.ComponentModel.DataAnnotations;

namespace PharmacyBillingService.DTOs
{
    public class StockImportDto
    {
        [Required(ErrorMessage = "Mã thuốc là bắt buộc")]
        public int MedicineId { get; set; }

        [Required(ErrorMessage = "Số lượng nhập là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập phải lớn hơn 0")]
        public int Quantity { get; set; }

        [MaxLength(255, ErrorMessage = "Lý do tối đa 255 ký tự")]
        public string? Reason { get; set; }
    }

    public class StockAdjustDto
    {
        [Required(ErrorMessage = "Mã thuốc là bắt buộc")]
        public int MedicineId { get; set; }

        [Required(ErrorMessage = "Số lượng tồn kho mới là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho mới phải lớn hơn hoặc bằng 0")]
        public int NewQuantity { get; set; }

        [MaxLength(255, ErrorMessage = "Lý do tối đa 255 ký tự")]
        public string? Reason { get; set; }
    }

    public class StockTransactionDto
    {
        public int TransactionId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Import, Export, Adjust
        public int Quantity { get; set; }
        public int BeforeQuantity { get; set; }
        public int AfterQuantity { get; set; }
        public string? Reason { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
