using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace SmartMailNotifier.Services
{
    public class GmailService
    {
        public readonly IConfiguration _config;
        public readonly HttpClient _httpClient;
        public GmailService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }
        public async Task<string?> GetAccessToken(string refreshToken)
        {
            using var http = new HttpClient();

            var tokenRequest = new Dictionary<string, string>
            {
                { "client_id", _config["Gmail:ClientId"] },
                { "client_secret", _config["Gmail:ClientSecret"] },
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" }
            };

            var response = await http.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(tokenRequest));

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            if (data.TryGetProperty("access_token", out var token))
                return token.GetString();

            return null;
        }

        public async Task SendEmailAsync(
    string accessToken,
    string to,
    string subject,
    string body)
        {
            var raw =
                $"To: {to}\r\n" +
                $"Subject: {subject}\r\n" +
                "Content-Type: text/plain; charset=utf-8\r\n\r\n" +
                body;

            var encoded = Base64UrlEncode(raw);

            var message = new { raw = encoded };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://gmail.googleapis.com/gmail/v1/users/me/messages/send");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            request.Content = new StringContent(
                JsonSerializer.Serialize(message),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Email failed: {error}");
            }
        }
        // Add this method to the GmailService class to fix CS0103
        private static string Base64UrlEncode(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var base64 = Convert.ToBase64String(bytes);
            // Convert base64 to base64url by replacing + with -, / with _, and removing trailing =
            return base64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }
}
