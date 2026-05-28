using System;
using System.Collections.Generic;

namespace PharmacyBillingService.DTOs
{
    public class PrescriptionItemDto
    {
        public int PrescriptionItemId { get; set; }
        public int PrescriptionId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Dosage { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class PrescriptionDto
    {
        public int PrescriptionId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int? AppointmentId { get; set; }
        public int? MedicalRecordId { get; set; }
        public string Status { get; set; } = string.Empty; // Pending, ReadyToDispense, Dispensed, OutOfStock, PartiallyAvailable
        public DateTime CreatedAt { get; set; }
        public List<PrescriptionItemDto> PrescriptionItems { get; set; } = new List<PrescriptionItemDto>();
    }
}
