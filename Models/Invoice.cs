using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyBillingService.Models
{
    public class Invoice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InvoiceId { get; set; }

        [Required]
        public int PatientId { get; set; }

        public int? AppointmentId { get; set; }

        public int? PrescriptionId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ExaminationFee { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MedicineTotal { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0;

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Unpaid"; // Draft, Unpaid, Paid, Cancelled, Refunded

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }

        // Navigation properties
        [ForeignKey("PrescriptionId")]
        public Prescription? Prescription { get; set; }

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
