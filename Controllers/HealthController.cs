using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyBillingService.Data;
using System;
using System.Threading.Tasks;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Route("api/health")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly PharmacyDbContext _context;

        public HealthController(PharmacyDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Kiểm tra trạng thái hoạt động của hệ thống và kết nối cơ sở dữ liệu.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                // Kiểm tra kết nối tới database SQL Server
                bool canConnect = await _context.Database.CanConnectAsync();
                
                if (!canConnect)
                {
                    return StatusCode(503, new
                    {
                        Status = "Unhealthy",
                        Database = "Disconnected",
                        Timestamp = DateTime.UtcNow
                    });
                }

                return Ok(new
                {
                    Status = "Healthy",
                    Database = "Connected",
                    Service = "Pharmacy & Billing Service (N3)",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    Status = "Unhealthy",
                    Database = "Error",
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
