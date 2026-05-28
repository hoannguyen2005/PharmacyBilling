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
    [Route("api/inventory")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpPost("import")]
        [Authorize(Roles = "Admin,Nurse")]
        public async Task<IActionResult> ImportStock([FromBody] StockImportDto importDto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await _inventoryService.ImportStockAsync(importDto, userId);
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

        [HttpPost("adjust")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdjustStock([FromBody] StockAdjustDto adjustDto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await _inventoryService.AdjustStockAsync(adjustDto, userId);
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

        [HttpGet("transactions")]
        [Authorize(Roles = "Admin,Nurse")]
        public async Task<IActionResult> GetTransactions()
        {
            var result = await _inventoryService.GetTransactionsAsync();
            return Ok(result);
        }

        [HttpGet("transactions/{medicineId}")]
        [Authorize(Roles = "Admin,Nurse")]
        public async Task<IActionResult> GetTransactionsByMedicine(int medicineId)
        {
            var result = await _inventoryService.GetTransactionsByMedicineIdAsync(medicineId);
            return Ok(result);
        }
    }
}
