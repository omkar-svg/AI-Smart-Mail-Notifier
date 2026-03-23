using Microsoft.EntityFrameworkCore;
using SmartMailNotifier.Data;
using SmartMailNotifier.DTOs;
using SmartMailNotifier.Models;
using SmartMailNotifier.Repository.Interfaces;
using SmartMailNotifier.Services.Interfaces;
using SmartMailNotifier.Helpers;

namespace SmartMailNotifier.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtHelper _jwtHelperInstance;

        public AuthService(AppDbContext context, JwtHelper jwtHelper)
        {
            _context = context;
            _jwtHelperInstance = jwtHelper;
        }

        // =========================
        // ✅ REGISTER
        // =========================
        public async Task<bool> RegisterAsync(RegisterDto dto)
        {
            var email = dto.Email.Trim().ToLower();

            // check if user exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser != null)
                return false;

            var user = new User
            {
                Email = email,
                Password = dto.Password // ⚠️ plain for now (you can hash later)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return true;
        }

        // =========================
        // ✅ LOGIN (FIXED)
        // =========================
        public async Task<string?> LoginAsync(LoginDto dto)
        {
            try
            {
                var email = dto.Email.Trim().ToLower();
                var password = dto.Password.Trim();

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                // 🔥 FIX 1: user null check
                if (user == null)
                {
                    Console.WriteLine("User not found");
                    return null;
                }

                // 🔥 FIX 2: password check
                if (user.Password != password)
                {
                    Console.WriteLine("Password mismatch");
                    return null;
                }

                // 🔥 FIX 3: generate JWT
                var token = _jwtHelperInstance.GenerateToken(user);

                return token;
            }
            catch (Exception ex)
            {
                Console.WriteLine("LOGIN ERROR: " + ex.Message);
                throw; // shows in Render logs
            }
        }
    }
}