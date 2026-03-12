using SmartMailNotifier.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartMailNotifier.Services
{
    public class SendEmailService
    {
        readonly GmailService _gmailService;
        readonly AppDbContext _context;
        readonly AiService _aiService;  

        public SendEmailService(AppDbContext dbContext, GmailService gmailService,AiService aiService)
        {
            _context = dbContext;
            _gmailService = gmailService;
            _aiService = aiService;
        }

        public async Task SendEmail(string sender, string receiver, string subject, string body)
        {
            try
            {
                Console.WriteLine($"Searching user with WhatsApp: {sender}");

                var user = _context.Users
                    .FirstOrDefault(u => u.WhatsappNumber == sender);

                if (user == null)
                {
                    Console.WriteLine("User not found");
                    return;
                }

                var refreshTokenEntity = _context.GmailRefreshTokens
                    .FirstOrDefault(t => t.UserId == user.Id && t.IsActive);

                if (refreshTokenEntity == null)
                {
                    Console.WriteLine("No active refresh token found");
                    return;
                }

                var accessToken = await _gmailService
                    .GetAccessToken(refreshTokenEntity.RefreshToken);

                Console.WriteLine("Access token obtained");

                using var httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                // Proper MIME email format
                body = _aiService.GenerateEmailBody(body).Result;
                var emailMessage =
                    $"From: {user.Email}\r\n" +
                    $"To: {receiver}\r\n" +
                    $"Subject: {subject}\r\n" +
                    $"Content-Type: text/plain; charset=utf-8\r\n\r\n" +
                    $"{body}";

                // Convert to Base64 URL safe format
                var bytes = Encoding.UTF8.GetBytes(emailMessage);
                var base64Message = Convert.ToBase64String(bytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");

                var gmailRequest = new
                {
                    raw = base64Message
                };

                var json = JsonSerializer.Serialize(gmailRequest);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(
                    "https://gmail.googleapis.com/gmail/v1/users/me/messages/send",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Gmail send failed: " + error);
                    return;
                }

                Console.WriteLine("Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendEmail error: " + ex.Message);
            }
        }
    }
}