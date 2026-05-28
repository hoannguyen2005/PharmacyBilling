using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyBillingService.Models
{
    public class StockTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TransactionId { get; set; }

        [Required]
        public int MedicineId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Type { get; set; } = string.Empty; // Import, Export, Adjust

        [Required]
        public int Quantity { get; set; }

        [Required]
        public int BeforeQuantity { get; set; }

        [Required]
        public int AfterQuantity { get; set; }

        [MaxLength(255)]
        public string? Reason { get; set; }

        public int? CreatedBy { get; set; } // UserId

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("MedicineId")]
        public Medicine? Medicine { get; set; }
    }
}
