using Microsoft.IdentityModel.Tokens;
using SmartMailNotifier.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartMailNotifier.Helpers
{
    public class JwtHelper
    {
        private readonly IConfiguration _config;

        public JwtHelper(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(User user)
        {
            // ✅ FIX: Read ENV first, fallback to appsettings
            var key = Environment.GetEnvironmentVariable("JWT_KEY")
                      ?? _config["Jwt:Key"];

            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                         ?? _config["Jwt:Issuer"];

            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                           ?? _config["Jwt:Audience"];

            if (string.IsNullOrEmpty(key))
                throw new Exception("JWT_KEY is missing!");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            // ✅ FIX: Avoid null crash
            if (!string.IsNullOrEmpty(user.Name))
            {
                claims.Add(new Claim("username", user.Name));
            }

            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key)
            );

            var creds = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}