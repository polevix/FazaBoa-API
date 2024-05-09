using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using FazaBoa_API.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;

    public AccountController(UserManager<ApplicationUser> userManager, IConfiguration configuration, IEmailSender emailSender)
    {
        _userManager = userManager;
        _configuration = configuration;
        _emailSender = emailSender;
    }

    /// <summary>
    /// Envia um link de redefinição de senha para o email do usuário.
    /// </summary>
    /// <param name="model">Modelo contendo o email do usuário</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPassword model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return BadRequest(new { Message = "User with this email does not exist" });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = $"{_configuration["ClientAppUrl"]}/reset-password?token={WebUtility.UrlEncode(token)}&email={WebUtility.UrlEncode(user.Email)}";

        await _emailSender.SendEmailAsync(user.Email, "Password Reset", $"Click the link to reset your password: <a href=\"{resetUrl}\">Reset Password</a>");

        return Ok(new { Message = "Password reset link sent to email" });
    }

    /// <summary>
    /// Reseta a senha do usuário com base no token fornecido.
    /// </summary>
    /// <param name="model">Modelo contendo o email do usuário, token e nova senha</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPassword model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return BadRequest(new { Message = "Invalid email" });
        }

        var decodedToken = WebUtility.UrlDecode(model.Token);
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { Message = "Error resetting password", Errors = result.Errors });
        }

        return Ok(new { Message = "Password reset successfully" });
    }
}
