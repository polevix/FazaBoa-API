using System.Threading.Tasks;

namespace FazaBoa_API.Services
{
    public interface IEmailSender
    {
        /// <summary>
        /// Sends an email asynchronously.
        /// </summary>
        /// <param name="email">Recipient email address.</param>
        /// <param name="subject">Subject of the email.</param>
        /// <param name="message">Message body of the email.</param>
        Task SendEmailAsync(string email, string subject, string message);
    }
}
