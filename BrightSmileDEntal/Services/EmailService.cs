using System.Net;
using System.Net.Mail;

namespace BrightSmileDEntal.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // Send a simple HTML email. Throws if SMTP configuration is missing or send fails.
        public void SendEmail(string toEmail, string subject, string htmlBody)
        {
            var host = _config["Smtp:Host"];
            var portStr = _config["Smtp:Port"];
            var username = _config["Smtp:Username"];
            var password = _config["Smtp:Password"];
            var from = _config["Smtp:From"] ?? username;

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(portStr))
            {
                throw new InvalidOperationException("SMTP configuration is missing (Smtp:Host or Smtp:Port).");
            }

            if (!int.TryParse(portStr, out var port))
                port = 25;

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(username, password)
            };

            var mail = new MailMessage(from, toEmail, subject, htmlBody)
            {
                IsBodyHtml = true
            };

            client.Send(mail);
        }
    }
}
