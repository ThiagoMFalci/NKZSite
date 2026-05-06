using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

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
            var host = GetConfig("Email:Smtp:Host", "Smtp:Host", "SmtpHost");
            var username = GetConfig("Email:Smtp:Username", "Email:Smtp:User", "Smtp:User", "SmtpUser");
            var password = GetConfig("Email:Smtp:Password", "Smtp:Password", "SmtpPassword");
            var from = GetConfig("Email:Smtp:From", "Smtp:From", "SmtpFrom") ?? username;
            var fromName = GetConfig("Email:Smtp:FromName", "Email:Smtp:DisplayName", "Smtp:DisplayName", "SmtpDisplayName") ?? "NKZ Academy";
            var port = GetIntConfig(587, "Email:Smtp:Port", "Smtp:Port", "SmtpPort");
            var enableSsl = GetBoolConfig(true, "Email:Smtp:EnableSsl", "Smtp:EnableSsl", "SmtpEnableSsl");
            var timeoutSeconds = Math.Clamp(GetIntConfig(20, "Email:Smtp:TimeoutSeconds", "Smtp:TimeoutSeconds", "SmtpTimeoutSeconds"), 5, 120);

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(from))
            {
                _logger.LogWarning("Email SMTP is not configured. Email to {Email} with subject {Subject} was not sent.", to, subject);
                if (_environment.IsDevelopment())
                {
                    return;
                }

                throw new InvalidOperationException("Servico de email nao configurado.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, from));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new BodyBuilder
            {
                TextBody = body
            }.ToMessageBody();

            using var client = new SmtpClient
            {
                Timeout = timeoutSeconds * 1000
            };

            var socketOptions = GetSecureSocketOptions(port, enableSsl);

            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                await client.ConnectAsync(host, port, socketOptions, timeout.Token);
                await client.AuthenticateAsync(username, password, timeout.Token);
                await client.SendAsync(message, timeout.Token);
                await client.DisconnectAsync(true, timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Falha ao enviar email SMTP para {Email}. Host: {Host}; Port: {Port}; Ssl: {EnableSsl}.", to, host, port, enableSsl);
                throw new InvalidOperationException("Nao foi possivel enviar email via SMTP. Verifique host, porta, usuario, senha, remetente e SSL.");
            }
        }

        private string? GetConfig(params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = _configuration[key];
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }

            return null;
        }

        private int GetIntConfig(int defaultValue, params string[] keys)
        {
            return int.TryParse(GetConfig(keys), out var value) ? value : defaultValue;
        }

        private bool GetBoolConfig(bool defaultValue, params string[] keys)
        {
            return bool.TryParse(GetConfig(keys), out var value) ? value : defaultValue;
        }

        private static SecureSocketOptions GetSecureSocketOptions(int port, bool enableSsl)
        {
            if (!enableSsl)
            {
                return SecureSocketOptions.None;
            }

            return port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        }
    }
}
