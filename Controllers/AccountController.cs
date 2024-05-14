using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using FazaBoa_API.Models;
using FazaBoa_API.Services;
using FluentValidation;
using System.Net;
using Serilog;
using FluentValidation.Results;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;
    private readonly IValidator<ResetPassword> _resetPasswordValidator;

    public AccountController(UserManager<ApplicationUser> userManager, IConfiguration configuration, IEmailSender emailSender,
        IValidator<ResetPassword> resetPasswordValidator)
    {
        _userManager = userManager;
        _configuration = configuration;
        _emailSender = emailSender;
        _resetPasswordValidator = resetPasswordValidator;
    }

    /// <summary>
    /// Envia um link de redefinição de senha para o email do usuário.
    /// </summary>
    /// <param name="model">Modelo contendo o email do usuário</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPassword model)
    {
        if (string.IsNullOrEmpty(model.Email))
        {
            return BadRequest(new { Message = "Email is required" });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return BadRequest(new { Message = "User with this email does not exist" });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = $"{_configuration["ClientAppUrl"]}/reset-password?token={WebUtility.UrlEncode(token)}&email={WebUtility.UrlEncode(user.Email)}";

        var emailMessage = _emailSender.GenerateForgotPasswordMessage(resetUrl);
        await _emailSender.SendEmailAsync(user.Email, "Password Reset", emailMessage);

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
        // Validar o modelo usando FluentValidation
        ValidationResult validationResult = await _resetPasswordValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { Message = "Validation failed", Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList() });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return BadRequest(new { Message = "Invalid email" });
        }

        var decodedToken = WebUtility.UrlDecode(model.Token);
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);
        if (!result.Succeeded)
        {
            Log.Error("Error resetting password for user {Email}: {Errors}", model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new { Message = "Error resetting password", Errors = result.Errors });
        }

        return Ok(new { Message = "Password reset successfully" });
    }
}
