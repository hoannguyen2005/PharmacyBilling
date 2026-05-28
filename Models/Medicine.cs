using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyBillingService.Models
{
    public class Medicine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MedicineId { get; set; }

        [Required]
        [MaxLength(150)]
        public string MedicineName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? ActiveIngredient { get; set; }

        [Required]
        [MaxLength(50)]
        public string Unit { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int StockQuantity { get; set; } = 0;

        [Required]
        public int MinStockLevel { get; set; } = 10;

        public DateTime? ExpiryDate { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Active"; // Active, Inactive, OutOfStock

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
