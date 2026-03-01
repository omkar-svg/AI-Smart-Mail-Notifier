using SmartMailNotifier.DTOs;
using SmartMailNotifier.Helpers;
using SmartMailNotifier.Models;
using SmartMailNotifier.Repository.Interfaces;
using SmartMailNotifier.Services.Interfaces;

namespace SmartMailNotifier.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        private readonly JwtHelper _jwtHelper;

        public AuthService(IUserRepository userRepository, JwtHelper jwtHelper)
        {
            _userRepository = userRepository;
            _jwtHelper = jwtHelper;
        }

        public async Task<string?> LoginAsync(LoginDto loginDto)
        {
            Console.WriteLine("Email entered: " + loginDto.Email);
            Console.WriteLine("Password entered: " + loginDto.Password);
            var user = await _userRepository.GetUserByEmailAsync(loginDto.Email);

            if (user == null || user.Password != loginDto.Password)
            {
                return null; // ✅ RETURN NULL NOT STRING
            }

            var token = _jwtHelper.GenerateToken(user);
            return token;
        }

        public async Task<bool> RegisterAsync(RegisterDto registerDto)
        {
            // basic register flow - ensure email not taken
            var existing = await _userRepository.GetUserByEmailAsync(registerDto.Email);
            if (existing != null) return false;

            var user = new Models.User
            {
                Name = registerDto.Name,
                Email = registerDto.Email,
                Password = (registerDto.Password),
                WhatsappNumber = registerDto.WhatsappNumber
            };

            await _userRepository.AddUserAsync(user);
            return true;
        }
    }
}
