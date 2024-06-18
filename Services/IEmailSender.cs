using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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

        /// <summary>
        /// Generates the forgot password message.
        /// </summary>
        /// <param name="resetUrl">The reset URL.</param>
        /// <param name="httpContext">The HttpContext for generating the logo URL.</param>
        /// <returns>The HTML message string.</returns>
        string GenerateForgotPasswordMessage(string resetUrl, HttpContext httpContext);
    }
}
