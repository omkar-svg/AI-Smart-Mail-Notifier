using System.Collections.Specialized;

namespace SmartMailNotifier.DTOs
{
    public class EmailDto
    {
        public string? Subject { get; set; }
        public string? Sender { get; set; }
        public string? Summary { get; set; }
    }
}