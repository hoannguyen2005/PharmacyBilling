using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Services;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Route("api/invoices")]
    public class InvoicesController : ControllerBase
    {
        private readonly IBillingService _billingService;

        public InvoicesController(IBillingService billingService)
        {
            _billingService = billingService;
        }

        [HttpPost]
        [Authorize(Roles = "Receptionist,Nurse")]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto createDto)
        {
            try
            {
                var result = await _billingService.CreateInvoiceAsync(createDto);
                return CreatedAtAction(nameof(GetInvoiceById), new { id = result.InvoiceId }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Receptionist,Nurse")]
        public async Task<IActionResult> GetAllInvoices([FromQuery] string? status)
        {
            var result = await _billingService.GetAllInvoicesAsync(status);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Receptionist,Nurse,Patient")]
        public async Task<IActionResult> GetInvoiceById(int id)
        {
            var invoice = await _billingService.GetInvoiceByIdAsync(id);
            if (invoice == null) return NotFound(new { Message = "Không tìm thấy hóa đơn yêu cầu." });

            // BR03: Bệnh nhân chỉ được xem hóa đơn và đơn thuốc của chính mình
            if (User.IsInRole("Patient"))
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdStr != invoice.PatientId.ToString())
                {
                    return Forbid();
                }
            }

            return Ok(invoice);
        }

        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Admin,Receptionist,Patient")]
        public async Task<IActionResult> GetInvoicesByPatient(int patientId)
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

            var result = await _billingService.GetInvoicesByPatientIdAsync(patientId);
            return Ok(result);
        }

        [HttpPost("{id}/pay")]
        [Authorize(Roles = "Receptionist,Nurse")]
        public async Task<IActionResult> PayInvoice(int id, [FromBody] PayInvoiceDto payDto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await _billingService.PayInvoiceAsync(id, payDto, userId);
                return Ok(result);
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

        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> CancelInvoice(int id)
        {
            try
            {
                var success = await _billingService.CancelInvoiceAsync(id);
                if (!success) return NotFound(new { Message = "Không tìm thấy hóa đơn yêu cầu." });
                return Ok(new { Message = "Hủy hóa đơn viện phí thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
