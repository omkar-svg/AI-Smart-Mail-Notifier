using System.ComponentModel.DataAnnotations;

namespace SmartMailNotifier.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string WhatsappNumber { get; set; }

        public ICollection<Email> Emails { get; set; }

        public ICollection<GmailRefreshToken> GmailRefreshTokens { get; set; }
    }
}
