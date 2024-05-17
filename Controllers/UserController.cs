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

        public UserController(UserManager<ApplicationUser> userManager, IConfiguration configuration, ApplicationDbContext context, IValidator<Register> registerValidator)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
            _registerValidator = registerValidator;
        }

        /// <summary>
        /// Registra um novo usuário.
        /// </summary>
        /// <param name="model">Modelo contendo os dados do registro</param>
        /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] Register model)
        {
            ValidationResult validationResult = await _registerValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                return BadRequest(CreateResponse("Error", "Validation Failed", validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
            }

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                return Conflict(CreateResponse("Error", "User already exists", new List<string> { "The provided email is already registered." }));
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
                    return StatusCode(StatusCodes.Status500InternalServerError, CreateResponse("Error", "User creation failed! Please check user details and try again.", result.Errors.Select(e => e.Description).ToList()));
                }

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = $"{_configuration["ClientAppUrl"]}/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

                Log.Information("Confirmation Link: {Link}", confirmationLink);

                return Ok(CreateResponse("Success", "User created successfully!"));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred while creating user");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateResponse("Error", "Internal Server Error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Realiza o login do usuário.
        /// </summary>
        /// <param name="model">Modelo contendo os dados do login</param>
        /// <returns>Retorna um token JWT ou uma mensagem de erro</returns>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Login model)
        {
            if (string.IsNullOrEmpty(model.Email))
            {
                return BadRequest(new { Message = "Email is required" });
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

                // Generate JWT
                var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? throw new InvalidOperationException("JWT Key is not set in environment variables");
                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    expires: DateTime.UtcNow.AddHours(3),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                // Generate Refresh Token
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
            return Unauthorized(new { Message = "Invalid credentials" });
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
                return Unauthorized(new { Message = "User not found" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { Message = "Invalid user" });
            }

            // Limpa o refresh token do usuário para prevenir novas autenticações usando o mesmo
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.UtcNow; // Define a data de expiração do refresh token para o momento atual

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "Failed to logout user." });
            }

            return Ok(new { Message = "Logged out successfully" });
        }

        /// <summary>
        /// Realiza a atualização to token.
        /// </summary>
        /// <returns>Retorna o token com o token atualizado</returns>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] TokenApi model)
        {
            if (model == null || string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.RefreshToken))
                return BadRequest("Invalid client request");

            var principal = GetPrincipalFromExpiredToken(model.Token);
            var username = principal.Identity?.Name;

            var user = await _userManager.FindByNameAsync(username);
            if (user == null || user.RefreshToken != model.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return BadRequest("Invalid client request");

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


        /// <summary>
        /// Realiza o login de um dependente.
        /// </summary>
        /// <param name="model">Modelo contendo os dados do login do dependente</param>
        /// <returns>Retorna um token JWT ou uma mensagem de erro</returns>
        [AllowAnonymous]
        [HttpPost("dependent-login")]
        public async Task<IActionResult> DependentLogin([FromBody] DependentLogin model)
        {
            var masterUser = await _userManager.FindByEmailAsync(model.MasterUserEmail);
            if (masterUser == null || !await _userManager.CheckPasswordAsync(masterUser, model.MasterUserPassword))
            {
                return Unauthorized(new { Message = "Invalid master user credentials" });
            }

            var dependentUser = await _userManager.FindByEmailAsync(model.DependentEmail);
            if (dependentUser == null || !dependentUser.IsDependent || dependentUser.MasterUserId != masterUser.Id)
            {
                return Unauthorized(new { Message = "Invalid dependent credentials" });
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
        public async Task<IActionResult> UploadProfilePhoto([FromForm] IFormFile photo)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not authorized" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "profile-photos");
            Directory.CreateDirectory(directoryPath);
            var filePath = Path.Combine(directoryPath, $"{userId}_{photo.FileName}");

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            user.ProfilePhotoUrl = $"/profile-photos/{userId}_{photo.FileName}";
            await _userManager.UpdateAsync(user);

            return Ok(new { Message = "Profile photo uploaded successfully", PhotoUrl = user.ProfilePhotoUrl });
        }

        /// <summary>
        /// Obtém os detalhes do perfil de um usuário.
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <returns>Retorna os detalhes do perfil ou uma mensagem de erro</returns>
        [HttpGet("profile/{userId}")]

        // Métodos
        public async Task<IActionResult> GetUserProfile(string userId)
        {
            var requestingUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var requestingUser = await _userManager.FindByIdAsync(requestingUserId);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            if (requestingUserId != userId && (!user.IsDependent || user.MasterUserId != requestingUserId))
            {
                return Unauthorized(new { Message = "User not authorized to view this profile" });
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

        private Response CreateResponse(string status, string message, List<string> errors = null)

        {
            return new Response
            {
                Status = status,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }

        private JwtSecurityToken GenerateJwtToken(IEnumerable<Claim> claims)
        {
            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? throw new InvalidOperationException("JWT Key is not set in environment variables");
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
                throw new ArgumentNullException(nameof(token), "Token cannot be null or empty");

            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? _configuration["Jwt:Key"];
            Log.Information("JWT Key loaded: {JwtKey}", jwtKey);  // Log para verificar a chave JWT
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("JWT Key is not set in configuration");

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
                throw new SecurityTokenException("Invalid token");

            return principal;
        }



    }
}
