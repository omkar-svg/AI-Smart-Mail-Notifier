using System.Text.Json;

namespace SmartMailNotifier.Services
{
    public class GmailService
    {
        public readonly IConfiguration _config;

        public GmailService(IConfiguration config)
        {
            _config = config;
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

    }
}
