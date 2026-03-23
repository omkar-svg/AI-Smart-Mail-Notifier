using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using static Google.Apis.Gmail.v1.UsersResource;

public class WhatsAppService
{
    private readonly IConfiguration _config;

    public WhatsAppService(IConfiguration config)
    {
        _config = config;
    }

    public void SendWhatsApp(string to, string message)
    {
        var sid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID")
           ?? _config["Twilio:AccountSid"];

        var token = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN")
                    ?? _config["Twilio:AuthToken"];

        var from = Environment.GetEnvironmentVariable("TWILIO_FROM_NUMBER")
                   ?? _config["Twilio:FromNumber"];

        TwilioClient.Init(sid, token);

        MessageResource.Create(
            from: new PhoneNumber($"whatsapp:{from}"),
            to: new PhoneNumber($"whatsapp:{to}"),
            body: message
        );
    }
}