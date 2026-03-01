using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartMailNotifier.Models
{
    public class GmailRefreshToken
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        [Required]
        public string RefreshToken { get; set; }
        [Required]
        public string GmailAddress { get; set; }

        public bool IsActive { get; set; } = true;

       
        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}