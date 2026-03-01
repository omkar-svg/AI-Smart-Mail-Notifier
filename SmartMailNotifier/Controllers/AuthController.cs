using Microsoft.AspNetCore.Mvc;
using SmartMailNotifier.DTOs;
using SmartMailNotifier.Services.Interfaces;
using SmartMailNotifier.Services;

namespace SmartMailNotifier.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
            private readonly WhatsAppService whatsAppService;
        public AuthController(IAuthService authService, WhatsAppService whatsAppService )
        {

            _authService = authService;
            this.whatsAppService = whatsAppService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto);
            if (result)
            {
                return Ok("Registration successful.");
            }
            return BadRequest("Registration failed.");

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var token = await _authService.LoginAsync(loginDto);

            if (token == null)
                return Unauthorized("Invalid email or password");

            return Ok(new
            {
                token = token
            });
        }
    }
}