using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyBillingService.Services;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("revenue/daily")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDailyRevenue([FromQuery] int days = 30)
        {
            var result = await _reportService.GetDailyRevenueAsync(days);
            return Ok(result);
        }

        [HttpGet("revenue/monthly")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetMonthlyRevenue([FromQuery] int months = 12)
        {
            var result = await _reportService.GetMonthlyRevenueAsync(months);
            return Ok(result);
        }

        [HttpGet("top-medicines")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTopMedicines([FromQuery] int count = 5)
        {
            var result = await _reportService.GetTopMedicinesAsync(count);
            return Ok(result);
        }

        [HttpGet("unpaid-invoices")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> GetUnpaidInvoices()
        {
            var result = await _reportService.GetUnpaidInvoicesAsync();
            return Ok(result);
        }

        [HttpGet("low-stock")]
        [Authorize(Roles = "Admin,Nurse")]
        public async Task<IActionResult> GetLowStockReport()
        {
            var result = await _reportService.GetLowStockReportAsync();
            return Ok(result);
        }
    }
}
