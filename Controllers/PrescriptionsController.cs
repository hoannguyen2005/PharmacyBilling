using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyBillingService.Services;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Route("api/prescriptions")]
    public class PrescriptionsController : ControllerBase
    {
        private readonly IPrescriptionService _prescriptionService;

        public PrescriptionsController(IPrescriptionService prescriptionService)
        {
            _prescriptionService = prescriptionService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Nurse,Receptionist")]
        public async Task<IActionResult> GetAllPrescriptions([FromQuery] string? status)
        {
            var result = await _prescriptionService.GetAllPrescriptionsAsync(status);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Nurse,Receptionist,Doctor,Patient")]
        public async Task<IActionResult> GetPrescriptionById(int id)
        {
            var prescription = await _prescriptionService.GetPrescriptionByIdAsync(id);
            if (prescription == null) return NotFound(new { Message = "Không tìm thấy đơn thuốc yêu cầu." });

            // BR03: Bệnh nhân chỉ được xem hóa đơn và đơn thuốc của chính mình
            if (User.IsInRole("Patient"))
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdStr != prescription.PatientId.ToString())
                {
                    return Forbid();
                }
            }

            return Ok(prescription);
        }

        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Admin,Nurse,Receptionist,Patient")]
        public async Task<IActionResult> GetPrescriptionsByPatient(int patientId)
        {
            // BR03: Bệnh nhân chỉ được xem hóa đơn và đơn thuốc của chính mình
            if (User.IsInRole("Patient"))
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdStr != patientId.ToString())
                {
                    return Forbid();
                }
            }

            var result = await _prescriptionService.GetPrescriptionsByPatientIdAsync(patientId);
            return Ok(result);
        }

        [HttpPost("{id}/dispense")]
        [Authorize(Roles = "Nurse,Receptionist")]
        public async Task<IActionResult> Dispense(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            try
            {
                var success = await _prescriptionService.DispensePrescriptionAsync(id, userId);
                if (!success) return BadRequest(new { Message = "Xuất thuốc không thành công." });
                return Ok(new { Message = "Xuất thuốc thành công và đã trừ tồn kho." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
