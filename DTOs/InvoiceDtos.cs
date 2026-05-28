using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PharmacyBillingService.DTOs
{
    public class CreateInvoiceDto
    {
        [Required(ErrorMessage = "Mã đơn thuốc là bắt buộc")]
        public int PrescriptionId { get; set; }

        [Required(ErrorMessage = "Phí khám là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Phí khám phải lớn hơn hoặc bằng 0")]
        public decimal ExaminationFee { get; set; } = 150000; // default 150.000đ
    }

    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public int PaidBy { get; set; }
        public string PaidByName { get; set; } = string.Empty;
        public DateTime PaidAt { get; set; }
        public string? Note { get; set; }
    }

    public class InvoiceDto
    {
        public int InvoiceId { get; set; }
        public int PatientId { get; set; }
        public int? AppointmentId { get; set; }
        public int? PrescriptionId { get; set; }
        public decimal ExaminationFee { get; set; }
        public decimal MedicineTotal { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty; // Draft, Unpaid, Paid, Cancelled, Refunded
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public List<PaymentDto> Payments { get; set; } = new List<PaymentDto>();
    }

    public class PayInvoiceDto
    {
        [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc")]
        [RegularExpression("^(Cash|Banking|Card|Momo|Other)$", ErrorMessage = "Phương thức thanh toán không hợp lệ. Phải là: Cash, Banking, Card, Momo, hoặc Other")]
        public string PaymentMethod { get; set; } = "Cash";

        [MaxLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự")]
        public string? Note { get; set; }
    }
}
