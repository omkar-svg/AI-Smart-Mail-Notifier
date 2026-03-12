using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SmartMailNotifier.Services
{
    public class AiService
    {
        private readonly IConfiguration _config;

        public AiService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<string> GetSummary(string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(body))
                return "New email received.";

            body = CleanHtml(body);
            body = RemoveReplyChain(body);

            if (body.Length > 4000)
                body = body.Substring(0, 4000);

            var apiKey = _config["Groq:ApiKey"];

            try
            {
                using var http = new HttpClient();

                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                var prompt = $"""
You are an assistant that summarizes emails.

Rules:
- Write one clear sentence.
- Do NOT repeat the subject.
- Focus on the key action or event.
- If a date/time exists include it.

Subject:
{subject}

Email Body:
{body}

Summary:
""";

                var request = new
                {
                    model = "llama-3.1-8b-instant",   // safer free model
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.3
                };

                var json = JsonSerializer.Serialize(request);

                var response = await http.PostAsync(
                    "https://api.groq.com/openai/v1/chat/completions",
                    new StringContent(json, Encoding.UTF8, "application/json")
                );

                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Groq API error: " + result);
                    return subject ?? "New email received.";
                }

                var doc = JsonDocument.Parse(result);

                var summary = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrWhiteSpace(summary))
                    return subject ?? "New email received.";

                if (summary.Length > 180)
                    summary = summary.Substring(0, 180) + "...";

                return summary;
            }
            catch (Exception ex)
            {
                Console.WriteLine("AI summarization error: " + ex.Message);
                return subject ?? "New email received.";
            }
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
    


     public async Task<string> GenerateEmailBody(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
                return "Please provide details for the email.";

            var apiKey = _config["Groq:ApiKey"];

            try
            {
                using var http = new HttpClient();

                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                var prompt = $"""
You are an assistant that writes professional email messages.

Task:
Convert the following request into a short professional email body.

Rules:
- Write 4–6 lines.
- Be polite and professional.
- Do not include subject.
- Do not include email address.

Request:
{sentence}

Email Body:
""";

                var request = new
                {
                    model = "llama-3.1-8b-instant",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.4
                };

                var json = JsonSerializer.Serialize(request);

                var response = await http.PostAsync(
                    "https://api.groq.com/openai/v1/chat/completions",
                    new StringContent(json, Encoding.UTF8, "application/json")
                );

                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Groq API error: " + result);
                    return sentence;
                }

                var doc = JsonDocument.Parse(result);

                var emailBody = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return emailBody ?? sentence;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email generation error: " + ex.Message);
                return sentence;
            }
        }
      }

    }