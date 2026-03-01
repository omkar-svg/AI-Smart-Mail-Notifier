using System.Text.RegularExpressions;
using System.Linq;

namespace SmartMailNotifier.Services
{
    public class AiService
    {
        public string GetSummary(string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(body))
                return "New email received.";

            body = CleanHtml(body);
            body = RemoveReplyChain(body);

            if (body.Length > 3000)
                body = body.Substring(0, 3000);

            var sentences = Regex
                .Split(body, @"(?<=[\.!\?])\s+")
                .Where(s => s.Length > 20)
                .ToList();

            if (!sentences.Any())
                return subject ?? "New email received.";

            var importantWords = new[]
            {
                "interview","meeting","deadline","offer",
                "exam","payment","invoice","scheduled",
                "tomorrow","today","zoom","teams",
                "google","date","time","joining","credited","debited","internship"
            };

            var ranked = sentences
                .Select(s => new
                {
                    Sentence = s.Trim(),
                    Score = importantWords.Count(w => s.ToLower().Contains(w))
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Sentence.Length)
                .First();

            string summary = ranked.Sentence;

            // Extract date/time if not already included
            string dateInfo = ExtractDateTime(body);

            if (!string.IsNullOrEmpty(dateInfo) && !summary.ToLower().Contains(dateInfo.ToLower()))
                summary += " " + dateInfo;

            if (summary.Length > 180)
                summary = summary.Substring(0, 180) + "...";

            return summary;
        }

        private string CleanHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return Regex.Replace(input, "<.*?>", string.Empty);
        }

        private string RemoveReplyChain(string body)
        {
            var split = Regex.Split(body, @"On .* wrote:|From:|-----Original Message-----", RegexOptions.IgnoreCase);
            return split[0];
        }

        private string ExtractDateTime(string body)
        {
            if (string.IsNullOrEmpty(body)) return "";

            string lower = body.ToLower();

            if (lower.Contains("tomorrow")) return "tomorrow";
            if (lower.Contains("today")) return "today";

            var timeMatch = Regex.Match(body, @"\b\d{1,2}:\d{2}\s?(AM|PM|am|pm)\b");
            if (timeMatch.Success)
                return "at " + timeMatch.Value;

            var dateMatch = Regex.Match(body, @"\b\d{1,2}\s?(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s?\d{0,4}\b", RegexOptions.IgnoreCase);
            if (dateMatch.Success)
                return "on " + dateMatch.Value;

            return "";
        }
    }
}