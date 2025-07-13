using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace FleaMarket.FrontEnd.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly SmtpOptions _options;

        public SmtpEmailService(IOptions<SmtpOptions> options)
        {
            _options = options.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(to))
            {
                return;
            }

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                Credentials = new NetworkCredential(_options.User, _options.Password),
                EnableSsl = true
            };
            using var message = new MailMessage(_options.From, to, subject, body);
            await client.SendMailAsync(message);
        }
    }
}
