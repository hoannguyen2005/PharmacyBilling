using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyBillingService.Events;
using PharmacyBillingService.Services;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventSimulatorController : ControllerBase
    {
        private readonly IPrescriptionService _prescriptionService;

        public EventSimulatorController(IPrescriptionService prescriptionService)
        {
            _prescriptionService = prescriptionService;
        }

        /// <summary>
        /// Giả lập nhận Event prescription.created từ Medical Record Service.
        /// </summary>
        [HttpPost("simulate-prescription-created")]
        [AllowAnonymous]
        public async Task<IActionResult> SimulatePrescriptionCreated([FromBody] PrescriptionCreatedEvent ev)
        {
            try
            {
                if (ev == null)
                {
                    return BadRequest(new { Message = "Payload sự kiện không hợp lệ." });
                }

                ev.EventName = "prescription.created";
                if (ev.CreatedAt == default)
                {
                    ev.CreatedAt = DateTime.UtcNow;
                }

                var result = await _prescriptionService.ProcessPrescriptionCreatedEventAsync(ev);
                return Ok(new
                {
                    Message = "Giả lập nhận Event prescription.created thành công.",
                    Prescription = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi hệ thống.", Detail = ex.Message });
            }
        }
    }
}
