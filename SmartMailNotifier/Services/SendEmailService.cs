using Microsoft.EntityFrameworkCore;
using SmartMailNotifier.Data;
using SmartMailNotifier.DTOs;
using SmartMailNotifier.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartMailNotifier.Services
{
    public class SendEmailService
    {
        private readonly GmailService _gmailService;
        private readonly AppDbContext _context;
        private readonly AiService _aiService;

        public SendEmailService(
            AppDbContext dbContext,
            GmailService gmailService,
            AiService aiService)
        {
            _context = dbContext;
            _gmailService = gmailService;
            _aiService = aiService;
        }

        // =========================================================
        // ✅ SEND EMAIL + SAVE TO DB
        // =========================================================
        public async Task SendEmail(string sender, string receiver, string subject, string body)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.WhatsappNumber == sender);

                if (user == null)
                {
                    Console.WriteLine("User not found");
                    return;
                }

                var refreshTokenEntity = await _context.GmailRefreshTokens
                    .FirstOrDefaultAsync(t => t.UserId == user.Id && t.IsActive);

                if (refreshTokenEntity == null)
                {
                    Console.WriteLine("No refresh token found");
                    return;
                }

                // ✅ Get access token
                var accessToken = await _gmailService
                    .GetAccessToken(refreshTokenEntity.RefreshToken);

                // ✅ AI body generation
                EmailResponse response1 = new EmailResponse();
                response1 =  await _aiService.GenerateEmailBody(body,sender);

                // ✅ Detect importance (simple logic)
                bool isImportant = body.ToLower().Contains("urgent") ||
                                   body.ToLower().Contains("important");

                // ✅ Create MIME message
                var emailMessage =
                    $"From: {user.Email}\r\n" +
                    $"To: {receiver}\r\n" +
                    $"Subject: {response1.Subject}\r\n" +
                    $"Content-Type: text/plain; charset=utf-8\r\n\r\n" +
                    $"{response1.Body}";

                var base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(emailMessage))
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");

                var gmailRequest = new { raw = base64Message };
                var json = JsonSerializer.Serialize(gmailRequest);

                using var httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(
                    "https://gmail.googleapis.com/gmail/v1/users/me/messages/send",
                    content
                );

                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Gmail send failed:");
                    Console.WriteLine(responseText);
                    return;
                }

                Console.WriteLine("✅ Email sent successfully!");

                // ✅ SAVE EMAIL TO DB (VERY IMPORTANT)
                var emailEntity = new Email
                {
                    UserId = user.Id,
                    GmailAddress = user.Email,
                    Subject = subject,
                    Sender = receiver,
                    Summary = body,
                    IsImportant = isImportant ? "Yes" : "No",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Emails.Add(emailEntity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ SendEmail error: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        // =========================================================
        // ✅ TODAY'S OVERVIEW
        // =========================================================
        public async Task<string> GetTodaysOverview(string sender)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sender))
                    return "❌ Invalid sender.";

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.WhatsappNumber == sender);

                if (user == null)
                    return "❌ User not found";

                var today = DateTime.UtcNow.Date;

                var todaysEmailsQuery = _context.Emails
                    .Where(e => e.UserId == user.Id && e.CreatedAt.Date == today);

                int totalEmails = await todaysEmailsQuery.CountAsync();

                int importantEmails = await todaysEmailsQuery
                    .CountAsync(e => (e.IsImportant ?? "no").ToLower() == "yes");

                int normalEmails = totalEmails - importantEmails;

                var latestEmails = await todaysEmailsQuery
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(3)
                    .ToListAsync();

                string response = $"📊 Today's Overview\n\n" +
                                  $"📧 Total Emails: {totalEmails}\n" +
                                  $"⭐ Important: {importantEmails}\n" +
                                  $"📩 Normal: {normalEmails}\n\n";

                if (latestEmails.Any())
                {
                    response += "🕒 Latest Emails:\n";

                    foreach (var email in latestEmails)
                    {
                        string subject = string.IsNullOrEmpty(email.Subject) ? "No Subject" : email.Subject;
                        string importance = email.IsImportant ?? "No";

                        response += $"• {subject} ({importance})\n";
                    }
                }
                else
                {
                    response += "No emails received today.";
                }

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ GetTodaysOverview error: " + ex.Message);
                return "⚠️ Failed to fetch overview.";
            }
        }

        // =========================================================
        // ✅ IMPORTANT EMAILS
        // =========================================================
        public async Task<string> GetImportantEmails(string sender)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sender))
                    return "❌ Invalid sender.";

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.WhatsappNumber == sender);

                if (user == null)
                    return "❌ User not found";

                var importantEmails = await _context.Emails
                    .Where(e => e.UserId == user.Id &&
                                (e.IsImportant ?? "no").ToLower() == "yes")
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                if (!importantEmails.Any())
                    return "⭐ No important emails found.";

                string response = "⭐ Important Emails:\n\n";

                foreach (var email in importantEmails)
                {
                    string subject = string.IsNullOrEmpty(email.Subject) ? "No Subject" : email.Subject;
                    string senderEmail = string.IsNullOrEmpty(email.Sender) ? "Unknown" : email.Sender;

                    response += $"• {subject}\n";
                    response += $"  From: {senderEmail}\n\n";
                }

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ GetImportantEmails error: " + ex.Message);
                return "⚠️ Error fetching important emails.";
            }
        }

        // =========================================================
        // ✅ TOTAL EMAILS
        // =========================================================
        public async Task<string> GetTotalEmails(string sender)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sender))
                    return "❌ Invalid sender.";

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.WhatsappNumber == sender);

                if (user == null)
                    return "❌ User not found";

                int totalEmails = await _context.Emails
                    .CountAsync(e => e.UserId == user.Id);

                return $"📧 Total Emails: {totalEmails}";
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ GetTotalEmails error: " + ex.Message);
                return "⚠️ Error fetching total emails.";
            }
        }
    }
}