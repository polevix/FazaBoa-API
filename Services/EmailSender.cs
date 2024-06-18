using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

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

        public string GenerateForgotPasswordMessage(string resetUrl, HttpContext httpContext)
        {
            var logoUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/images/logo.png";

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        line-height: 1.6;
                        color: #333;
                    }}
                    .container {{
                        max-width: 600px;
                        margin: 0 auto;
                        padding: 20px;
                        border: 1px solid #e0e0e0;
                        border-radius: 8px;
                        box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
                    }}
                    .header {{
                        text-align: center;
                        margin-bottom: 20px;
                    }}
                    .header img {{
                        max-height: 50px;
                    }}
                    .content {{
                        margin-bottom: 20px;
                    }}
                    .button {{
                        display: inline-block;
                        padding: 10px 20px;
                        margin: 20px 0;
                        font-size: 16px;
                        color: #fff;
                        background-color: #007BFF;
                        text-decoration: none;
                        border-radius: 4px;
                    }}
                    .footer {{
                        text-align: center;
                        font-size: 12px;
                        color: #777;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <img src='{logoUrl}' alt='FazaBoa Logo'>
                    </div>
                    <div class='content'>
                        <p>Olá,</p>
                        <p>Você solicitou a redefinição da sua senha. Para redefinir sua senha, clique no botão abaixo:</p>
                        <p style='text-align: center;'>
                            <a href='{resetUrl}' class='button'>Redefinir Senha</a>
                        </p>
                        <p>Se você não solicitou essa alteração, por favor, ignore este e-mail. Sua senha permanecerá a mesma.</p>
                        <p>Atenciosamente,<br>Equipe Faz a Boa</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2024 FazaBoa. Todos os direitos reservados.</p>
                        <p>Este é um e-mail automático, por favor, não responda.</p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
}
