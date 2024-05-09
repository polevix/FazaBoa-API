using System.Net;
using System.Net.Mail;
using FazaBoa_API.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(message))
        {
            throw new ArgumentException("Email, subject, and message are required.");
        }

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
    }
}
