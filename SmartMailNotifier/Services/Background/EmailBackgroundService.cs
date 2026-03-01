using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SmartMailNotifier.Data;
using SmartMailNotifier.Services;
using SmartMailNotifier.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartMailNotifier.Services.Background
{
    public class EmailBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public EmailBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("Background service running: " + DateTime.Now);

                try
                {
                    await FetchEmailsForAllUsers();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("BACKGROUND ERROR: " + ex.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
        }

        private async Task FetchEmailsForAllUsers()
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gmailService = scope.ServiceProvider.GetRequiredService<GmailService>();
            var aiService = scope.ServiceProvider.GetRequiredService<AiService>();
            var whatsAppService = scope.ServiceProvider.GetRequiredService<WhatsAppService>();

            var gmailAccounts = context.GmailRefreshTokens
                .Where(x => x.IsActive)
                .Include(x => x.User)
                .ToList();

            foreach (var gmail in gmailAccounts)
            {
                if (gmail.User == null) continue;
                if (string.IsNullOrEmpty(gmail.RefreshToken)) continue;

                var accessToken = await gmailService.GetAccessToken(gmail.RefreshToken);
                if (string.IsNullOrEmpty(accessToken)) continue;

                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                // 🔥 ONLY unread inbox emails
                var gmailResponse = await http.GetAsync(
                "https://gmail.googleapis.com/gmail/v1/users/me/messages?q=in:inbox is:unread&maxResults=10");

                if (!gmailResponse.IsSuccessStatusCode) continue;

                var emailsJson = await gmailResponse.Content.ReadAsStringAsync();
                var messageList = JsonSerializer.Deserialize<JsonElement>(emailsJson);

                if (!messageList.TryGetProperty("messages", out var messages)) continue;

                foreach (var msg in messages.EnumerateArray())
                {
                    var messageId = msg.GetProperty("id").GetString();
                    if (string.IsNullOrEmpty(messageId)) continue;

                    // skip duplicate in DB
                    if (context.Emails.Any(e => e.MessageId == messageId))
                        continue;

                    var fullMsg = await http.GetStringAsync(
                        $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{messageId}?format=full");

                    var emailData = JsonSerializer.Deserialize<JsonElement>(fullMsg);

                    if (!emailData.TryGetProperty("payload", out var payload))
                        continue;

                    string body = ExtractBody(payload);

                    if (!payload.TryGetProperty("headers", out var headers))
                        continue;

                    string subject = "No Subject";
                    string from = "Unknown Sender";

                    foreach (var h in headers.EnumerateArray())
                    {
                        var name = h.GetProperty("name").GetString();

                        if (name == "Subject")
                            subject = h.GetProperty("value").GetString() ?? "No Subject";

                        if (name == "From")
                            from = h.GetProperty("value").GetString() ?? "Unknown Sender";
                    }

                  

                    // 🔥 AI summary
                    string summary = aiService.GetSummary(subject, body);

                    bool isImportant =
                        subject.ToLower().Contains("job") ||
                        subject.ToLower().Contains("interview") ||
                        subject.ToLower().Contains("offer");

                    var email = new Email
                    {
                        MessageId = messageId,
                        Subject = subject,
                        Sender = from,
                        Summary = summary,
                        IsImportant = isImportant ? "Yes" : "No",
                        UserId = gmail.UserId,
                        GmailAddress = gmail.GmailAddress,
                        CreatedAt = DateTime.UtcNow
                    };

                    try
                    {
                        context.Emails.Add(email);
                        await context.SaveChangesAsync();

                        // mark as read in gmail
                        await MarkEmailAsRead(http, messageId);
                    }
                    catch (DbUpdateException)
                    {
                        Console.WriteLine("Duplicate skipped: " + messageId);
                        continue;
                    }

                    // 🔥 WhatsApp send
                    if (!string.IsNullOrEmpty(gmail.User.WhatsappNumber))
                    {
                        string phone = gmail.User.WhatsappNumber.Trim();

                        if (!phone.StartsWith("+"))
                            phone = "+91" + phone;

                        string msgText =
$@"📩 Important Email
From: {from}

🧠 Summary:
{summary}";

                        whatsAppService.SendWhatsApp(phone, msgText);
                        Console.WriteLine("WhatsApp sent to: " + phone);
                    }
                }
            }
        }

        // ================= BODY EXTRACTION =================

        private string ExtractBody(JsonElement payload)
        {
            if (payload.TryGetProperty("body", out var bodyElement))
            {
                if (bodyElement.TryGetProperty("data", out var dataElement))
                {
                    return DecodeBase64(dataElement.GetString());
                }
            }

            if (payload.TryGetProperty("parts", out var parts))
            {
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("mimeType", out var mimeType))
                    {
                        if (mimeType.GetString() == "text/plain")
                        {
                            if (part.GetProperty("body").TryGetProperty("data", out var data))
                            {
                                return DecodeBase64(data.GetString());
                            }
                        }
                    }
                }
            }

            return "";
        }

        private string DecodeBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64))
                return "";

            base64 = base64.Replace("-", "+").Replace("_", "/");
            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }

        private async Task MarkEmailAsRead(HttpClient http, string messageId)
        {
            var requestBody = new
            {
                removeLabelIds = new[] { "UNREAD" }
            };

            var json = JsonSerializer.Serialize(requestBody);

            var response = await http.PostAsync(
                $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{messageId}/modify",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            if (response.IsSuccessStatusCode)
                Console.WriteLine("Marked as read: " + messageId);
        }
    }
}