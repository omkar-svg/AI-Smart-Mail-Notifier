using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMailNotifier.Data;
using System.Security.Claims;
using SmartMailNotifier.DTOs;

namespace SmartMailNotifier.Controllers
{
   [Authorize]
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public IActionResult GetDashboard()
        {
            // var userId = 1; // temporary hardcoded user ID for testing
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // total today emails
            var today = DateTime.UtcNow.Date;

            var emailsToday = _context.Emails
                .Where(e => e.UserId == userId && e.CreatedAt.Date == today)
                .Count();

            var important = _context.Emails
                .Where(e => e.UserId == userId && e.IsImportant == "Yes")
                .Count();

            var recentImportant = _context.Emails
                .Where(e => e.UserId == userId && e.IsImportant == "Yes")
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .Select(e => new
                {
                    e.Id,
                    e.Subject,
                    e.Summary,
                    e.Sender
                })
                .ToList();

            return Ok(new
            {
                emailsToday,
                important,
                whatsappAlerts = important, // temporary
                recentImportant
            });
        }
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            int userid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (userid == 0)
            {
                return BadRequest("Invalid user ID");
            }

            string? name  = _context.Users.Where(u => u.Id == userid).Select(u => u.Name).FirstOrDefault();
            string? email = _context.Users.Where(u => u.Id == userid).Select(u => u.Email).FirstOrDefault();
            string? whatsapp = _context.Users.Where(u => u.Id == userid).Select(u => u.WhatsappNumber).FirstOrDefault();

            List<GmailDto> ConnectedGemails = _context.GmailRefreshTokens
                .Where(g => g.UserId == userid )
                .Select(g => new GmailDto
                {
                    GmailAddress = g.GmailAddress,
                    IsActive = g.IsActive ? "Yes" : "No"
                })
                .ToList();
            return Ok(new
            {
                name,
                email,
                whatsapp,
                ConnectedGemails
            });
        }
    }
}