using System.Net;
using System.Net.Mail;

namespace NKZAPI.Services.EmailServices
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, IWebHostEnvironment environment, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var host = _configuration["Email:Smtp:Host"];
            var username = _configuration["Email:Smtp:Username"];
            var password = _configuration["Email:Smtp:Password"];
            var from = _configuration["Email:Smtp:From"] ?? username;
            var fromName = _configuration["Email:Smtp:FromName"] ?? "NKZ Academy";
            var port = int.TryParse(_configuration["Email:Smtp:Port"], out var configuredPort) ? configuredPort : 587;
            var enableSsl = !bool.TryParse(_configuration["Email:Smtp:EnableSsl"], out var configuredSsl) || configuredSsl;

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(from))
            {
                _logger.LogWarning("Email SMTP is not configured. Email to {Email} with subject {Subject} was not sent.", to, subject);
                if (_environment.IsDevelopment())
                {
                    return;
                }

                throw new InvalidOperationException("Servico de email nao configurado.");
            }

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password),
            };

            using var message = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
        }
    }
}
