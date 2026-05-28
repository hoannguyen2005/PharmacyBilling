using System;
using System.ComponentModel.DataAnnotations;

namespace PharmacyBillingService.DTOs
{
    public class CreateMedicineDto
    {
        [Required(ErrorMessage = "Tên thuốc là bắt buộc")]
        [MaxLength(150, ErrorMessage = "Tên thuốc tối đa 150 ký tự")]
        public string MedicineName { get; set; } = string.Empty;

        [MaxLength(150, ErrorMessage = "Hoạt chất tối đa 150 ký tự")]
        public string? ActiveIngredient { get; set; }

        [Required(ErrorMessage = "Đơn vị tính là bắt buộc")]
        [MaxLength(50, ErrorMessage = "Đơn vị tính tối đa 50 ký tự")]
        public string Unit { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá bán là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Số lượng tồn kho là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0")]
        public int StockQuantity { get; set; } = 0;

        [Required(ErrorMessage = "Ngưỡng cảnh báo tồn kho là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Ngưỡng cảnh báo phải lớn hơn hoặc bằng 0")]
        public int MinStockLevel { get; set; } = 10;

        public DateTime? ExpiryDate { get; set; }
    }

    public class UpdateMedicineDto
    {
        [Required(ErrorMessage = "Tên thuốc là bắt buộc")]
        [MaxLength(150, ErrorMessage = "Tên thuốc tối đa 150 ký tự")]
        public string MedicineName { get; set; } = string.Empty;

        [MaxLength(150, ErrorMessage = "Hoạt chất tối đa 150 ký tự")]
        public string? ActiveIngredient { get; set; }

        [Required(ErrorMessage = "Đơn vị tính là bắt buộc")]
        [MaxLength(50, ErrorMessage = "Đơn vị tính tối đa 50 ký tự")]
        public string Unit { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá bán là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Số lượng tồn kho là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Ngưỡng cảnh báo tồn kho là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Ngưỡng cảnh báo phải lớn hơn hoặc bằng 0")]
        public int MinStockLevel { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [RegularExpression("^(Active|Inactive|OutOfStock)$", ErrorMessage = "Trạng thái không hợp lệ. Phải là: Active, Inactive, hoặc OutOfStock")]
        public string Status { get; set; } = "Active";
    }

    public class MedicineDto
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string? ActiveIngredient { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
