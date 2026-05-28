using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Services;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authService.LoginAsync(loginDto);
                if (result == null)
                {
                    return BadRequest(new { Message = "Email hoặc mật khẩu không chính xác." });
                }
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                var result = await _authService.RegisterAsync(registerDto);
                return CreatedAtAction(nameof(GetProfile), new { id = result.UserId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            var profile = await _authService.GetProfileAsync(userId);
            if (profile == null) return NotFound(new { Message = "Không tìm thấy người dùng." });

            return Ok(profile);
        }


        [HttpGet("users/doctors")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDoctors()
        {
            var doctors = await _authService.GetUsersByRolesAsync(new List<string> { "Doctor" });
            return Ok(doctors);
        }

        [HttpGet("users/nurses")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetNurses()
        {
            // Trả về cả Nurse (Y tá) và Receptionist (Tiếp tân)
            var nurses = await _authService.GetUsersByRolesAsync(new List<string> { "Nurse", "Receptionist" });
            return Ok(nurses);
        }

        [HttpGet("users/patients")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPatients()
        {
            var patients = await _authService.GetUsersByRolesAsync(new List<string> { "Patient" });
            return Ok(patients);
        }

        [HttpPut("users/{id}/lock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LockUser(int id)
        {
            try
            {
                var success = await _authService.LockUserAsync(id);
                if (!success) return NotFound(new { Message = "Không tìm thấy người dùng." });
                return Ok(new { Message = "Khóa tài khoản thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("users/{id}/unlock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnlockUser(int id)
        {
            var success = await _authService.UnlockUserAsync(id);
            if (!success) return NotFound(new { Message = "Không tìm thấy người dùng." });
            return Ok(new { Message = "Mở khóa tài khoản thành công." });
        }
    }
}
