using Google.Apis.Auth;
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

        // ================= CONNECT GMAIL =================
        [HttpGet("connect")]
        public IActionResult Connect(string token)
        {
            if (string.IsNullOrEmpty(token))
                return Unauthorized("Token missing");

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized("Invalid token");

            int userId = int.Parse(userIdClaim.Value);

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

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
            string gmailAddress = payload.Email ?? "";

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

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var gmailResponse = await httpClient.GetAsync(
                "https://gmail.googleapis.com/gmail/v1/users/me/messages?maxResults=5");

            if (!gmailResponse.IsSuccessStatusCode)
                return BadRequest(await gmailResponse.Content.ReadAsStringAsync());

            return Ok($"Gmail {gmailAddress} connected successfully");
        }
    }
}