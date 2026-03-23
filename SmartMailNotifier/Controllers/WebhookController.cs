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

            var message = form["Body"].ToString().Trim().ToLower();
            var from = form["From"].ToString().Replace("whatsapp:", "");
            var sid = form["MessageSid"].ToString();

            Console.WriteLine("===== WhatsApp Message =====");
            Console.WriteLine($"From: {from}");
            Console.WriteLine($"Message: {message}");
            Console.WriteLine($"SID: {sid}");

            try
            {
                // ===============================
                // ✅ COMMAND 1: HELP
                // ===============================
                if (message == "help")
                {
                    return Ok(
                        "🤖 Available Commands:\n\n" +
                        "1. send email to xyz@gmail.com Hello\n" +
                        "2. todays overview\n" +
                        "3. show important emails\n" +
                        "4. total emails"
                    );
                }

                // ===============================
                // ✅ COMMAND 2: TODAY'S OVERVIEW
                // ===============================
                if (message.Contains("todays overview"))
                {
                    var overview = await _sendEmailService.GetTodaysOverview(from);
                    return Ok(overview);
                }

                // ===============================
                // ✅ COMMAND 3: IMPORTANT EMAILS
                // ===============================
                if (message.Contains("important emails"))
                {
                    var importantEmails = await _sendEmailService.GetImportantEmails(from);
                    return Ok(importantEmails);
                }

                // ===============================
                // ✅ COMMAND 4: TOTAL EMAILS
                // ===============================
                if (message.Contains("total emails"))
                {
                    var total = await _sendEmailService.GetTotalEmails(from);
                    return Ok($"📧 Total Emails Sent: {total}");
                }

                // ===============================
                // ✅ COMMAND 5: SEND EMAIL
                // ===============================
                string pattern = @"send\s+email\s+to\s+(\S+)\s*(.*)";
                var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    string receiverEmail = match.Groups[1].Value;
                    string emailBody = match.Groups[2].Value;

                    await _sendEmailService.SendEmail(
                        from,
                        receiverEmail,
                        emailBody,
                        emailBody
                    );

                    return Ok("✅ Email sent successfully!");
                }

                // ===============================
                // ❌ UNKNOWN COMMAND
                // ===============================
                return Ok(
                    "❌ Command not recognized.\n\n" +
                    "Type 'help' to see available commands."
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("Webhook error: " + ex.Message);
                return Ok("⚠️ Something went wrong.");
            }
        }
    }
}