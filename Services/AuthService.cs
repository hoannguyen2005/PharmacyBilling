using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PharmacyBillingService.Data;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Helpers;
using PharmacyBillingService.Models;

namespace PharmacyBillingService.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<UserDto?> GetProfileAsync(int userId);
        Task<List<UserDto>> GetAllUsersAsync();
        Task<List<UserDto>> GetUsersByRolesAsync(List<string> roles);
        Task<bool> LockUserAsync(int userId);
        Task<bool> UnlockUserAsync(int userId);
    }

    public class AuthService : IAuthService
    {
        private readonly PharmacyDbContext _context;
        private readonly JwtHelper _jwtHelper;

        public AuthService(PharmacyDbContext context, JwtHelper jwtHelper)
        {
            _context = context;
            _jwtHelper = jwtHelper;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null) return null;

            // BR18: Chỉ tài khoản Active mới được đăng nhập
            if (user.Status != "Active")
            {
                throw new InvalidOperationException("Tài khoản đã bị khóa.");
            }

            if (!PasswordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return null;
            }

            var token = _jwtHelper.GenerateToken(user);
            return new LoginResponseDto
            {
                Token = token,
                User = MapToUserDto(user)
            };
        }

        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            var exists = await _context.Users.AnyAsync(u => u.Email == registerDto.Email);
            if (exists)
            {
                throw new InvalidOperationException("Email đã tồn tại trong hệ thống.");
            }

            var user = new User
            {
                FullName = registerDto.FullName,
                Email = registerDto.Email,
                PasswordHash = PasswordHasher.HashPassword(registerDto.Password),
                Role = registerDto.Role,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return MapToUserDto(user);
        }

        public async Task<UserDto?> GetProfileAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;
            return MapToUserDto(user);
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            return users.Select(MapToUserDto).ToList();
        }

        public async Task<List<UserDto>> GetUsersByRolesAsync(List<string> roles)
        {
            var users = await _context.Users
                .Where(u => roles.Contains(u.Role))
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            return users.Select(MapToUserDto).ToList();
        }

        public async Task<bool> LockUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (user.Role == "Admin")
            {
                throw new InvalidOperationException("Không thể khóa tài khoản Admin.");
            }

            user.Status = "Locked";
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlockUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.Status = "Active";
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}
