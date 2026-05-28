using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyBillingService.Models
{
    public class PrescriptionItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PrescriptionItemId { get; set; }

        [Required]
        public int PrescriptionId { get; set; }

        [Required]
        public int MedicineId { get; set; }

        [Required]
        [MaxLength(150)]
        public string MedicineName { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [MaxLength(255)]
        public string? Dosage { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // Navigation properties
        [ForeignKey("PrescriptionId")]
        public Prescription? Prescription { get; set; }

        [ForeignKey("MedicineId")]
        public Medicine? Medicine { get; set; }
    }
}
