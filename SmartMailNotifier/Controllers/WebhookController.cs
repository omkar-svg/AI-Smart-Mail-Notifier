using Microsoft.AspNetCore.Mvc;
using SmartMailNotifier.Services;
using System.Text.RegularExpressions;

namespace SmartMailNotifier.Controllers
{
    [ApiController]
    [Route("api/webhook")]
    public class WebhookController : ControllerBase
    {
        private readonly SendEmailService _sendEmailService;

        public WebhookController(SendEmailService sendEmailService)
        {
            _sendEmailService = sendEmailService;
        }

        [HttpPost("whatsapp")]
        public async Task<IActionResult> ReceiveWhatsApp()
        {
            var form = await Request.ReadFormAsync();

            var message = form["Body"].ToString();
            var from = form["From"].ToString().Replace("whatsapp:", "");
            var sid = form["MessageSid"].ToString();

            Console.WriteLine("===== WhatsApp Message =====");
            Console.WriteLine($"From: {from}");
            Console.WriteLine($"Message: {message}");
            Console.WriteLine($"SID: {sid}");

            try
            {
                string pattern = @"send\s+email\s+to\s+(\S+)\s*(.*)";
                var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);

                if (!match.Success)
                {
                    Console.WriteLine("Invalid command format");
                    return Ok("Command not recognized.");
                }

                string receiverEmail = match.Groups[1].Value;
                string emailBody = match.Groups[2].Value;

                await _sendEmailService.SendEmail(
                    from,
                    receiverEmail,
                    "WhatsApp Email",
                    emailBody
                );

                return Ok("Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Webhook error: " + ex.Message);
                return Ok("Something went wrong.");
            }
        }
    }
}