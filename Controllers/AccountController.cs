using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using FazaBoa_API.Models;
using FazaBoa_API.Services;
using FluentValidation;
using System.Net;
using Serilog;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;
    private readonly IValidator<ResetPassword> _resetPasswordValidator;
    private readonly IValidator<Register> _registerValidator;

    public AccountController(UserManager<ApplicationUser> userManager, IConfiguration configuration, IEmailSender emailSender,
        IValidator<ResetPassword> resetPasswordValidator, IValidator<Register> registerValidator)
    {
        _userManager = userManager;
        _configuration = configuration;
        _emailSender = emailSender;
        _resetPasswordValidator = resetPasswordValidator;
        _registerValidator = registerValidator;
    }
    /// <summary>
    /// Registra um novo usuário.
    /// </summary>
    /// <param name="model">Modelo contendo os dados do registro</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterUser([FromBody] Register model)
    {
        ValidationResult validationResult = await _registerValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            return BadRequest(CreateResponse("Error", "Validation Failed", validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        if (await _userManager.FindByEmailAsync(model.Email) != null)
        {
            return Conflict(CreateResponse("Error", "User already exists", new List<string> { "O email fornecido já está registrado." }));
        }

        try
        {
            var user = new ApplicationUser
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Email,
                FullName = model.FullName,
                IsDependent = model.IsDependent,
                MasterUserId = model.IsDependent ? model.MasterUserId : null
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                Log.Error("Failed to create user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                return StatusCode(StatusCodes.Status500InternalServerError, CreateResponse("Error", "Falha ao criar usuário! Verifique os detalhes do usuário e tente novamente.", result.Errors.Select(e => e.Description).ToList()));
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = $"{_configuration["ClientAppUrl"]}/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

            Log.Information("Confirmation Link: {Link}", confirmationLink);

            return Ok(CreateResponse("Success", "Usuário criado com sucesso!"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception occurred while creating user");
            return StatusCode(StatusCodes.Status500InternalServerError, CreateResponse("Error", "Erro interno do servidor", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Envia um link de redefinição de senha para o email do usuário.
    /// </summary>
    /// <param name="model">Modelo contendo o email do usuário</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
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
    [AllowAnonymous]
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

    //Métodos
    private Response CreateResponse(string status, string message, List<string>? errors = null)
    {
        return new Response
        {
            Status = status,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}


