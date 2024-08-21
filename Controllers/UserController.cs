using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using FazaBoa_API.Models;
using FazaBoa_API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using FluentValidation.Results;
using Serilog;
using System.Security.Cryptography;
using FazaBoa_API.Services;

namespace FazaBoa_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IValidator<Register> _registerValidator;
        private readonly IValidator<IFormFile> _uploadPhotoValidator;
        private readonly PhotoService _photoService;
        private readonly ILogger<UserController> _logger;

        public UserController(UserManager<ApplicationUser> userManager, IConfiguration configuration, ApplicationDbContext context,
            IValidator<IFormFile> uploadPhotoValidator, PhotoService photoService, ILogger<UserController> logger, IValidator<Register> registerValidator)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
            _uploadPhotoValidator = uploadPhotoValidator;
            _photoService = photoService;
            _logger = logger;
            _registerValidator = registerValidator;
        }

        /// <summary>
        /// Realiza o login do usuário.
        /// </summary>
        /// <param name="model">Modelo contendo os dados do login</param>
        /// <returns>Retorna um token JWT ou uma mensagem de erro</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] Login model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Email))
                {
                    return BadRequest(new { Message = "Email é obrigatório" });
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    var authClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    };

                    // Gerar JWT
                    var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? throw new InvalidOperationException("Chave JWT não está definida nas variáveis de ambiente");
                    var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

                    var token = new JwtSecurityToken(
                        issuer: _configuration["Jwt:Issuer"],
                        audience: _configuration["Jwt:Audience"],
                        expires: DateTime.UtcNow.AddHours(3),
                        claims: authClaims,
                        signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                    );

                    // Gerar Refresh Token
                    var refreshToken = GenerateRefreshToken();
                    user.RefreshToken = refreshToken;
                    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                    await _userManager.UpdateAsync(user);

                    return Ok(new
                    {
                        token = new JwtSecurityTokenHandler().WriteToken(token),
                        expiration = token.ValidTo,
                        refreshToken = refreshToken
                    });
                }
                return Unauthorized(new { Message = "Credenciais inválidas" });
            }
            catch (Exception ex)
            {
                // Log the exception
                Log.Error(ex, "Erro ocorreu durante o login");

                // Return a generic error message
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Ocorreu um erro ao processar sua solicitação" });
            }
        }

        /// <summary>
        /// Realiza o logout do usuário.
        /// </summary>
        /// <returns>Retorna uma mensagem de sucesso</returns>
        [HttpPost("logout")]
        [Authorize] // Este endpoint exige autenticação
        public async Task<IActionResult> Logout()
        {
            // Obtém o ID do usuário autenticado
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "Usuário não encontrado" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { Message = "Usuário inválido" });
            }

            // Limpa o refresh token do usuário para prevenir novas autenticações usando o mesmo
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.UtcNow; // Define a data de expiração do refresh token para o momento atual

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "Falha ao deslogar o usuário." });
            }

            return Ok(new { Message = "Deslogado com sucesso" });
        }

        /// <summary>
        /// Realiza a atualização do token.
        /// </summary>
        /// <returns>Retorna o token atualizado</returns>
        [HttpPost("refresh-token")]
        [Authorize]
        public async Task<IActionResult> RefreshToken([FromBody] TokenApi model)
        {
            if (model == null || string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.RefreshToken))
            {
                return BadRequest("Solicitação inválida do cliente");
            }

            try
            {
                var principal = GetPrincipalFromExpiredToken(model.Token);
                var username = principal.Identity?.Name;

                var user = await _userManager.FindByNameAsync(username);
                if (user == null || user.RefreshToken != model.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    return BadRequest("Solicitação inválida do cliente");
                }

                var newJwtToken = GenerateJwtToken(principal.Claims);
                var newRefreshToken = GenerateRefreshToken();

                user.RefreshToken = newRefreshToken;
                await _userManager.UpdateAsync(user);

                return new ObjectResult(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(newJwtToken),
                    refreshToken = newRefreshToken
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ocorreu durante a atualização do token");
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Realiza o login de um dependente.
        /// </summary>
        /// <param name="model">Modelo contendo os dados do login do dependente</param>
        /// <returns>Retorna um token JWT ou uma mensagem de erro</returns>        
        [HttpPost("dependent-login")]
        [Authorize]
        public async Task<IActionResult> DependentLogin([FromBody] DependentLogin model)
        {
            var masterUser = await _userManager.FindByEmailAsync(model.MasterUserEmail);
            if (masterUser == null || !await _userManager.CheckPasswordAsync(masterUser, model.MasterUserPassword))
            {
                return Unauthorized(new { Message = "Credenciais inválidas do usuário mestre" });
            }

            var dependentUser = await _userManager.FindByEmailAsync(model.DependentEmail);
            if (dependentUser == null || !dependentUser.IsDependent || dependentUser.MasterUserId != masterUser.Id)
            {
                return Unauthorized(new { Message = "Credenciais inválidas do dependente" });
            }

            var authClaims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, dependentUser.UserName),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }

        /// <summary>
        /// Faz upload da foto do perfil do usuário.
        /// </summary>
        /// <param name="photo">Foto a ser carregada</param>
        /// <returns>Retorna a URL da foto carregada ou uma mensagem de erro</returns>
        [HttpPost("upload-photo")]
        [Authorize]
        public async Task<IActionResult> UploadProfilePhoto([FromForm] IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                return BadRequest(new { Message = "Nenhum arquivo carregado" });
            }

            if (!photo.ContentType.StartsWith("image/"))
            {
                return BadRequest(new { Message = "Tipo de arquivo inválido. Apenas arquivos de imagem são permitidos" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "Usuário não autorizado" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "Usuário não encontrado" });
            }

            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "profile-photos");
            Directory.CreateDirectory(directoryPath);

            // Deletar a foto antiga, se existir
            if (!string.IsNullOrEmpty(user.ProfilePhotoUrl))
            {
                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePhotoUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            var fileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(directoryPath, fileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                user.ProfilePhotoUrl = $"/profile-photos/{fileName}";
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    System.IO.File.Delete(filePath);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Falha ao atualizar a foto do perfil do usuário." });
                }

                return Ok(new { Message = "Foto do perfil carregada com sucesso", PhotoUrl = user.ProfilePhotoUrl });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao carregar a foto do perfil para o usuário {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro ao carregar a foto do perfil." });
            }
        }


        /// <summary>
        /// Exclui a foto de perfil do usuário.
        /// </summary>
        /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
        [HttpDelete("delete-photo")]
        [Authorize]
        public async Task<IActionResult> DeleteProfilePhoto()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "Usuário não autorizado" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "Usuário não encontrado" });
            }

            if (string.IsNullOrEmpty(user.ProfilePhotoUrl) || user.ProfilePhotoUrl == "/profile-photos/default.png")
            {
                return BadRequest(new { Message = "Nenhuma foto de perfil para excluir ou já está usando a foto padrão" });
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePhotoUrl.TrimStart('/'));

            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                    user.ProfilePhotoUrl = "/profile-photos/default.png"; // Define a foto padrão
                    await _userManager.UpdateAsync(user);

                    return Ok(new { Message = "Foto do perfil excluída com sucesso e revertida para a padrão", PhotoUrl = user.ProfilePhotoUrl });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao excluir a foto do perfil para o usuário {UserId}", userId);
                    return StatusCode(500, new { Message = "Erro ao excluir a foto do perfil" });
                }
            }
            else
            {
                return NotFound(new { Message = "Foto de perfil não encontrada no servidor" });
            }
        }


        /// <summary>
        /// Obtém os detalhes do perfil de um usuário.
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <returns>Retorna os detalhes do perfil ou uma mensagem de erro</returns>
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "Usuário não autorizado" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { Message = "Usuário não encontrado" });
                }

                var createdChallenges = await _context.Challenges
                    .Where(c => c.CreatedById == userId)
                    .ToListAsync();

                var completedChallenges = await _context.CompletedChallenges
                    .Include(cc => cc.Challenge)
                    .Where(cc => cc.UserId == userId && cc.IsValidated)
                    .ToListAsync();

                var redeemedRewards = await _context.RewardTransactions
                    .Include(rt => rt.Reward)
                    .Where(rt => rt.UserId == userId)
                    .ToListAsync();

                return Ok(new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.ProfilePhotoUrl,
                    CreatedChallenges = createdChallenges.Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        c.CoinValue,
                        c.StartDate,
                        c.EndDate,
                        c.IsDaily
                    }),
                    CompletedChallenges = completedChallenges.Select(cc => new
                    {
                        cc.Challenge.Id,
                        cc.Challenge.Name,
                        cc.Challenge.Description,
                        cc.Challenge.CoinValue,
                        cc.CompletedDate
                    }),
                    RedeemedRewards = redeemedRewards.Select(rt => new
                    {
                        rt.Reward.Id,
                        rt.Reward.Description,
                        rt.Reward.RequiredCoins,
                        rt.Timestamp
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao obter o perfil do usuário.");
                return StatusCode(500, new { Message = "Ocorreu um erro ao obter o perfil do usuário" });
            }
        }

        // Métodos
        private JwtSecurityToken GenerateJwtToken(IEnumerable<Claim> claims)
        {
            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? throw new InvalidOperationException("A chave JWT não está definida nas variáveis de ambiente");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: creds
            );

            return token;
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token), "O token não pode ser nulo ou vazio");

            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? _configuration["Jwt:Key"];
            Log.Information("Chave JWT carregada: {JwtKey}", jwtKey);  // Log para verificar a chave JWT
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("A chave JWT não está definida na configuração");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateLifetime = false // Aqui estamos ignorando a validade do token
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Token inválido");

            return principal;
        }

    }
}
