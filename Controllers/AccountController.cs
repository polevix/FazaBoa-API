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
            return BadRequest(new { Message = "O email é obrigatório" });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return BadRequest(new { Message = "Usuário com este email não existe" });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = $"{_configuration["ClientAppUrl"]}/reset-password?token={WebUtility.UrlEncode(token)}&email={WebUtility.UrlEncode(user.Email)}";

        if (user.Email != null)
        {
            await _emailSender.SendEmailAsync(user.Email, "Redefinição de Senha", _emailSender.GenerateForgotPasswordMessage(resetUrl, HttpContext));
        }

        return Ok(new { Message = "Link de redefinição de senha enviado para o email" });
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
            return BadRequest(new { Message = "Falha na validação", Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList() });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return BadRequest(new { Message = "Email inválido" });
        }

        var decodedToken = WebUtility.UrlDecode(model.Token);
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);
        if (!result.Succeeded)
        {
            Log.Error("Erro ao redefinir a senha para o usuário {Email}: {Errors}", model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new { Message = "Erro ao redefinir a senha", Errors = result.Errors });
        }

        return Ok(new { Message = "Senha redefinida com sucesso" });
    }
}
