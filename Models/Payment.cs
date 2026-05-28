using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyBillingService.Models
{
    public class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PaymentId { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(30)]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Banking, Card, Momo, Other

        [Required]
        [MaxLength(30)]
        public string PaymentStatus { get; set; } = "Success"; // Success, Failed, Pending

        [Required]
        public int PaidBy { get; set; } // UserId of user who collected payment

        public DateTime PaidAt { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Note { get; set; }

        // Navigation property
        [ForeignKey("InvoiceId")]
        public Invoice? Invoice { get; set; }
    }
}
