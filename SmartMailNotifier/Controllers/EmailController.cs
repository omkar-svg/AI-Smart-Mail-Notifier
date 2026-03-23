using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMailNotifier.DTOs;
using SmartMailNotifier.Models;
using SmartMailNotifier.Services;
using SmartMailNotifier.Services.Interfaces;
using System.Security.Claims;

namespace SmartMailNotifier.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/email/[controller]")]
    public class EmailController : ControllerBase
    {

        public readonly IEmailService _emailService;
        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("addEmail")]
        public async Task<IActionResult> AddEmail(EmailDto emailDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized("Invalid or missing token");
            }

            int userId = int.Parse(userIdClaim);

            await _emailService.AddEmail(emailDto, userId);

            return Ok("Email added successfully");
        }


        [HttpGet("getEmails")]
        public async Task<IActionResult> GetEmails()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var emails = await _emailService.GetEmailsByUserId(userId);
            return Ok(emails);

        }
        [HttpGet("getEmail")]
        public async Task<IActionResult> GetEmail(int emailId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var email = await _emailService.GetEmailById(emailId, userId);
            if (email == null)
                return NotFound("Email not found or access denied");
            return Ok(email);
        }

        [HttpPut("emailactivation")]
        public async Task<IActionResult> ActivationEmail(string email)
        {
            try
            {
                await _emailService.ActivateEmail(email);
                return Ok("Email activation status toggled successfully");
            }
            catch (Exception ex)
            {
                return BadRequest("Error toggling email activation: " + ex.Message);
            }

        }
    }
}