using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartMailNotifier.Models
{
    public class Email
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public string MessageId { get; set; }   // Gmail message id

        public string GmailAddress { get; set; }   // which gmail received this mail
        public string Subject { get; set; }

        public string Sender { get; set; }

        public string Summary { get; set; }

        public string IsImportant { get; set; } = "No";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
