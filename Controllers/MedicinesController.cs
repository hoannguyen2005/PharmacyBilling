using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Services;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Route("api/medicines")]
    public class MedicinesController : ControllerBase
    {
        private readonly IMedicineService _medicineService;

        public MedicinesController(IMedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Doctor,Receptionist,Nurse")]
        public async Task<IActionResult> GetAllMedicines(
            [FromQuery] string? name,
            [FromQuery] string? activeIngredient,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _medicineService.GetAllMedicinesAsync(name, activeIngredient, status, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Doctor,Receptionist,Nurse")]
        public async Task<IActionResult> GetMedicineById(int id)
        {
            var medicine = await _medicineService.GetMedicineByIdAsync(id);
            if (medicine == null) return NotFound(new { Message = "Không tìm thấy thuốc yêu cầu." });
            return Ok(medicine);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateMedicine([FromBody] CreateMedicineDto createDto)
        {
            var result = await _medicineService.CreateMedicineAsync(createDto);
            return CreatedAtAction(nameof(GetMedicineById), new { id = result.MedicineId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMedicine(int id, [FromBody] UpdateMedicineDto updateDto)
        {
            var result = await _medicineService.UpdateMedicineAsync(id, updateDto);
            if (result == null) return NotFound(new { Message = "Không tìm thấy thuốc yêu cầu." });
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMedicine(int id)
        {
            var success = await _medicineService.DeleteMedicineAsync(id);
            if (!success) return NotFound(new { Message = "Không tìm thấy thuốc yêu cầu." });
            return Ok(new { Message = "Xóa/Ngừng bán thuốc thành công." });
        }

        [HttpGet("low-stock")]
        [Authorize(Roles = "Admin,Nurse")]
        public async Task<IActionResult> GetLowStock()
        {
            var result = await _medicineService.GetLowStockMedicinesAsync();
            return Ok(result);
        }

        [HttpGet("expired")]
        [Authorize(Roles = "Admin,Nurse")]
        public async Task<IActionResult> GetExpired()
        {
            var result = await _medicineService.GetExpiredMedicinesAsync();
            return Ok(result);
        }
    }
}
