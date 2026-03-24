using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMailNotifier.Data;
using SmartMailNotifier.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace SmartMailNotifier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GmailController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public GmailController(IConfiguration config, AppDbContext context)
        {
            _config = config;
            _context = context;
        }

        // ================= GET USER FROM JWT =================
        private int? GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                              ?? User.FindFirst(JwtRegisteredClaimNames.Sub)
                              ?? User.FindFirst("sub");

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var id))
                return id;

            return null;
        }

        // ================= CONNECT GMAIL =================
        // 🔐 MUST REQUIRE LOGIN
        [Authorize]
        [HttpGet("connect")]
        public IActionResult Connect()
        {
            var userId = GetAuthenticatedUserId();

            if (userId == null)
                return Unauthorized("Invalid token");

            var clientId = Environment.GetEnvironmentVariable("GMAIL_CLIENT_ID");
            var redirectUri = Environment.GetEnvironmentVariable("GMAIL_REDIRECT_URI");

            var url = "https://accounts.google.com/o/oauth2/v2/auth" +
                      "?client_id=" + clientId +
                      "&redirect_uri=" + redirectUri +
                      "&response_type=code" +
                      "&scope=" +
                      "https://www.googleapis.com/auth/gmail.readonly%20" +
                      "https://www.googleapis.com/auth/gmail.send%20" +
                      "https://www.googleapis.com/auth/userinfo.email%20" +
                      "openid" +
                      "&access_type=offline" +
                      "&prompt=consent" +
                      "&state=" + userId;

            return Redirect(url);
        }

        // ================= CALLBACK =================
        // 🌐 PUBLIC (Google will call this)
        [AllowAnonymous]
        [HttpGet("callback")]
        public async Task<IActionResult> Callback(string code, string state)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest("Authorization code not received.");

            if (!int.TryParse(state, out int userId))
                return Unauthorized("Invalid state.");

            var clientId = Environment.GetEnvironmentVariable("GMAIL_CLIENT_ID")
                 ?? _config["Gmail:ClientId"];

            var clientSecret = Environment.GetEnvironmentVariable("GMAIL_CLIENT_SECRET")
                               ?? _config["Gmail:ClientSecret"];

            var redirectUri = Environment.GetEnvironmentVariable("GMAIL_REDIRECT_URI")
                              ?? _config["Gmail:RedirectUri"];

            var tokenRequest = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", clientId ?? "" },
                { "client_secret", clientSecret ?? "" },
                { "redirect_uri", redirectUri ?? "" },
                { "grant_type", "authorization_code" }
            };

            using var httpClient = new HttpClient();

            var tokenResponse = await httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(tokenRequest));

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();

            if (!tokenResponse.IsSuccessStatusCode)
                return BadRequest(tokenJson);

            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);

            string accessToken = tokenData.GetProperty("access_token").GetString() ?? "";
            string idToken = tokenData.GetProperty("id_token").GetString() ?? "";

            string refreshToken = "";
            if (tokenData.TryGetProperty("refresh_token", out var refreshTokenElement))
                refreshToken = refreshTokenElement.GetString() ?? "";

            // ================= GET USER EMAIL =================
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
            string gmailAddress = payload.Email ?? "";

            // ================= SAVE REFRESH TOKEN =================
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var existing = _context.GmailRefreshTokens
                    .FirstOrDefault(x => x.UserId == userId && x.GmailAddress == gmailAddress);

                if (existing == null)
                {
                    _context.GmailRefreshTokens.Add(new GmailRefreshToken
                    {
                        UserId = userId,
                        GmailAddress = gmailAddress,
                        RefreshToken = refreshToken,
                        IsActive = true
                    });
                }
                else
                {
                    existing.RefreshToken = refreshToken;
                    existing.IsActive = true;
                }

                await _context.SaveChangesAsync();
            }

            // ================= FETCH EMAILS =================
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var gmailResponse = await httpClient.GetAsync(
                "https://gmail.googleapis.com/gmail/v1/users/me/messages?maxResults=5");

            if (!gmailResponse.IsSuccessStatusCode)
                return BadRequest(await gmailResponse.Content.ReadAsStringAsync());

            var emailsJson = await gmailResponse.Content.ReadAsStringAsync();
            var messageList = JsonSerializer.Deserialize<JsonElement>(emailsJson);

            if (!messageList.TryGetProperty("messages", out var messages))
                return Ok($"Gmail {gmailAddress} connected but no emails");

            foreach (var msg in messages.EnumerateArray())
            {
                var messageId = msg.GetProperty("id").GetString();
                if (string.IsNullOrEmpty(messageId))
                    continue;

                if (_context.Emails.Any(e => e.MessageId == messageId))
                    continue;

                var fullMsg = await httpClient.GetStringAsync(
                    $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{messageId}");

                var emailData = JsonSerializer.Deserialize<JsonElement>(fullMsg);
                var payloadData = emailData.GetProperty("payload");
                var headers = payloadData.GetProperty("headers");

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

                string isImportant =
                    subject.ToLower().Contains("job") ||
                    subject.ToLower().Contains("interview") ||
                    subject.ToLower().Contains("offer")
                    ? "Yes" : "No";

                _context.Emails.Add(new Email
                {
                    MessageId = messageId,
                    Subject = subject,
                    Sender = from,
                    Summary = $"Mail from {from} about {subject}",
                    IsImportant = isImportant,
                    UserId = userId,
                    GmailAddress = gmailAddress
                });
            }

            await _context.SaveChangesAsync();

            return Ok($"Gmail {gmailAddress} connected successfully");
        }
    }
}