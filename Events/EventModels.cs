using System;
using System.Collections.Generic;

namespace PharmacyBillingService.Events
{
    public class PrescriptionCreatedItemEvent
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Dosage { get; set; }
    }

    public class PrescriptionCreatedEvent
    {
        public string EventName { get; set; } = "prescription.created";
        public int PrescriptionId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int? AppointmentId { get; set; }
        public int? MedicalRecordId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<PrescriptionCreatedItemEvent> Items { get; set; } = new List<PrescriptionCreatedItemEvent>();
    }

    public class MedicineStockUpdatedEvent
    {
        public string EventName { get; set; } = "medicine.stock.updated";
        public int MedicineId { get; set; }
        public int BeforeQuantity { get; set; }
        public int AfterQuantity { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class MedicineDispensedEvent
    {
        public string EventName { get; set; } = "medicine.dispensed";
        public int PrescriptionId { get; set; }
        public int PatientId { get; set; }
        public DateTime DispensedAt { get; set; } = DateTime.UtcNow;
    }

    public class InvoicePaidEvent
    {
        public string EventName { get; set; } = "invoice.paid";
        public int InvoiceId { get; set; }
        public int PatientId { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    }
}
