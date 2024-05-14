using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FazaBoa_API.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(message))
            {
                _logger.LogError("Email, subject, or message is null or empty.");
                throw new ArgumentException("Email, subject, and message are required.");
            }

            try
            {
                var smtpClient = new SmtpClient
                {
                    Host = _configuration["Smtp:Host"],
                    Port = int.Parse(_configuration["Smtp:Port"]),
                    EnableSsl = bool.Parse(_configuration["Smtp:EnableSsl"]),
                    Credentials = new NetworkCredential(_configuration["Smtp:Username"], _configuration["Smtp:Password"])
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration["Smtp:Username"]),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {email}", email);
                throw;
            }
        }

        public string GenerateForgotPasswordMessage(string resetUrl)
        {
            return $@"
                <html>
                <body>
                    <h2>Redefinição de Senha</h2>
                    <p>Você solicitou a redefinição de sua senha. Clique no link abaixo para redefinir sua senha:</p>
                    <a href='{resetUrl}'>Redefinir Senha</a>
                    <p>Se você não solicitou a redefinição de senha, ignore este e-mail.</p>
                </body>
                </html>";
        }
    }
}
