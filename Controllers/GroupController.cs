using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using FazaBoa_API.Models;
using FazaBoa_API.Data;
using FazaBoa_API.Dtos;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using FazaBoa_API.Services;
using Serilog;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<GroupCreationDto> _groupValidator;
    private readonly PhotoService _photoService;
    private readonly UserManager<ApplicationUser> _userManager;

    public GroupController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IValidator<GroupCreationDto> groupValidator,
     PhotoService photoService)
    {
        _userManager = userManager;
        _context = context;
        _groupValidator = groupValidator;
        _photoService = photoService;
    }

    /// <summary>
    /// Cria um novo grupo.
    /// </summary>
    /// <param name="group">Modelo contendo os dados do grupo</param>
    /// <returns>Retorna o grupo criado ou uma mensagem de erro</returns>
    [HttpPost("create-group")]
    [Authorize]
    public async Task<IActionResult> CreateGroup([FromForm] GroupCreationDto groupDto)
    {
        // Validação do modelo
        var validationResult = await _groupValidator.ValidateAsync(groupDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { Message = "Dados do grupo inválidos", Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList() });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new { Message = "Usuário não autorizado" });
        }

        // Busca o usuário no banco de dados
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { Message = "Usuário não encontrado" });
        }

        // Configura o grupo com os dados do DTO
        var group = new Group
        {
            Name = groupDto.Name,
            Description = groupDto.Description,
            HasUniqueRewards = groupDto.HasUniqueRewards,
            CreatedById = userId,
            PhotoUrl = groupDto.Photo != null ? await _photoService.UploadPhotoAsync(groupDto.Photo, userId) : "/default-photo.png"
        };

        // Adiciona o usuário como membro
        group.Members.Add(user);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Log.Error(ex, "Erro ao criar o grupo para o usuário {UserId}", userId);
            return StatusCode(500, new { Message = "Erro ao criar o grupo" });
        }

        return Ok(new
        {
            Message = "Grupo criado com sucesso",
            Group = new
            {
                group.Id,
                group.Name,
                group.Description,
                group.PhotoUrl,
                CreatedBy = new { user.Id, user.FullName }
            }
        });

    }

    /// <summary>
    /// Obtém os detalhes de um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <returns>Retorna os detalhes do grupo ou uma mensagem de erro</returns>
    [HttpGet("{groupId}")]
    public async Task<IActionResult> GetGroupDetails(Guid groupId)
    {
        var group = await _context.Groups
            .Include(g => g.Members)
            .Include(g => g.Challenges)
            .Include(g => g.Rewards)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            return NotFound(new { Message = "Grupo não encontrado" });
        }

        return Ok(new
        {
            group.Id,
            group.Name,
            group.PhotoUrl,
            Members = group.Members.Select(m => new { m.Id, m.FullName, m.Email }),
            Challenges = group.Challenges.Select(c => new { c.Id, c.Name, c.Description }),
            Rewards = group.Rewards.Select(r => new { r.Id, r.Description, r.RequiredCoins })
        });
    }

    /// <summary>
    /// Obtém todos os grupos criados por um usuário.
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Retorna uma lista de grupos criados pelo usuário</returns>
    [HttpGet("created-by/{userId}")]
    public async Task<IActionResult> GetGroupsCreatedByUser(string userId)
    {
        var groups = await _context.Groups
            .Where(g => g.CreatedById == userId)
            .ToListAsync();

        return Ok(groups);
    }

    /// <summary>
    /// Adiciona um membro a um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("{groupId}/add-member")]
    public async Task<IActionResult> AddMember(Guid groupId, [FromBody] string userId)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
            return NotFound(new { Message = "Grupo não encontrado" });

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { Message = "Usuário não encontrado" });

        if (!group.Members.Contains(user))
        {
            group.Members.Add(user);
            await _context.SaveChangesAsync();
        }

        return Ok(new { Message = "Membro adicionado com sucesso" });
    }

    /// <summary>
    /// Remove um membro de um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("{groupId}/remove-member")]
    public async Task<IActionResult> RemoveMember(Guid groupId, [FromBody] string userId)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
            return NotFound(new { Message = "Grupo não encontrado" });

        var user = group.Members.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return NotFound(new { Message = "Membro não encontrado no grupo" });

        group.Members.Remove(user);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Membro removido com sucesso" });
    }

    /// <summary>
    /// Marca um membro como dependente.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("{groupId}/mark-dependent")]
    public async Task<IActionResult> MarkAsDependent(Guid groupId, [FromBody] string userId)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
            return NotFound(new { Message = "Grupo não encontrado" });

        var user = group.Members.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return NotFound(new { Message = "Membro não encontrado no grupo" });

        var masterUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(masterUserId))
            return Unauthorized(new { Message = "Usuário mestre não encontrado" });

        var applicationUser = await _context.Users.FindAsync(userId);
        if (applicationUser == null)
            return NotFound(new { Message = "Usuário não encontrado" });

        applicationUser.IsDependent = true;
        applicationUser.MasterUserId = masterUserId;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Membro marcado como dependente com sucesso" });
    }

    /// <summary>
    /// Adiciona um dependente a um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="dependentEmail">Email do dependente</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("{groupId}/add-dependent")]
    public async Task<IActionResult> AddDependent(Guid groupId, [FromBody] string dependentEmail)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
            return NotFound(new { Message = "Grupo não encontrado" });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var masterUser = await _context.Users.FindAsync(userId);
        if (masterUser == null || group.CreatedById != userId)
            return Unauthorized(new { Message = "Usuário não autorizado a adicionar dependentes" });

        var dependentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dependentEmail);
        if (dependentUser == null)
            return NotFound(new { Message = "Usuário não encontrado" });

        if (!group.Members.Contains(dependentUser))
            group.Members.Add(dependentUser);

        dependentUser.IsDependent = true;
        dependentUser.MasterUserId = userId;

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Dependente adicionado com sucesso" });
    }

    /// <summary>
    /// Obtém todos os dependentes de um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <returns>Retorna uma lista de dependentes ou uma mensagem de erro</returns>
    [HttpGet("{groupId}/dependents")]
    public async Task<IActionResult> GetDependents(Guid groupId)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
            return NotFound(new { Message = "Grupo não encontrado" });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (group.CreatedById != userId)
            return Unauthorized(new { Message = "Usuário não autorizado a visualizar dependentes" });

        var dependents = group.Members
            .Where(m => m.IsDependent)
            .Select(d => new { d.Id, d.FullName, d.Email })
            .ToList();

        return Ok(dependents);
    }

    /// <summary>
    /// Envia um convite para um usuário entrar no grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="email">Email do usuário a ser convidado</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("{groupId}/invite")]
    public async Task<IActionResult> InviteMember(Guid groupId, [FromBody] string email)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
        {
            return NotFound(new { Message = "Grupo não encontrado" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (group.CreatedById != userId)
        {
            return Unauthorized(new { Message = "Usuário não autorizado a convidar membros" });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return NotFound(new { Message = "Usuário com o email fornecido não encontrado" });
        }

        if (group.Members.Any(m => m.Id == user.Id))
        {
            return BadRequest(new { Message = "Usuário já é membro do grupo" });
        }

        group.Members.Add(user);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Usuário convidado com sucesso" });
    }

    /// <summary>
    /// Aceita um convite para entrar no grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("{groupId}/accept-invite")]
    public async Task<IActionResult> AcceptInvite(Guid groupId, [FromBody] string userId)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        var user = await _context.Users.FindAsync(userId);

        if (group == null || user == null)
        {
            return NotFound(new { Message = "Grupo ou usuário não encontrado" });
        }

        if (group.Members.Any(m => m.Id == user.Id))
        {
            return BadRequest(new { Message = "Usuário já é membro do grupo" });
        }

        group.Members.Add(user);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Convite aceito" });
    }

    /// <summary>
    /// Atualiza os dados de um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="updatedGroup">Modelo contendo os dados atualizados do grupo</param>
    /// <returns>Retorna o grupo atualizado ou uma mensagem de erro</returns>
    [HttpPut("{groupId}")]
    public async Task<IActionResult> UpdateGroup(Guid groupId, [FromBody] Group updatedGroup)
    {
        var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
        {
            return NotFound(new { Message = "Grupo não encontrado" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (group.CreatedById != userId)
        {
            return Unauthorized(new { Message = "Usuário não autorizado a editar este grupo" });
        }

        var existingGroup = await _context.Groups
            .FirstOrDefaultAsync(g => g.CreatedById == userId && g.Name == updatedGroup.Name && g.Id != groupId);

        if (existingGroup != null)
        {
            return BadRequest(new { Message = "Um grupo com o mesmo nome já existe" });
        }

        group.Name = updatedGroup.Name ?? group.Name;
        group.PhotoUrl = updatedGroup.PhotoUrl ?? group.PhotoUrl;

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Grupo atualizado com sucesso", Group = group });
    }

    /// <summary>
    /// Remove um dependente de um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpDelete("{groupId}/remove-dependent/{userId}")]
    public async Task<IActionResult> RemoveDependent(Guid groupId, string userId)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
            return NotFound(new { Message = "Grupo não encontrado" });

        var masterUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (group.CreatedById != masterUserId)
            return Unauthorized(new { Message = "Usuário não autorizado a remover dependentes" });

        var dependentUser = group.Members.FirstOrDefault(u => u.Id == userId && u.IsDependent);
        if (dependentUser == null)
            return NotFound(new { Message = "Dependente não encontrado no grupo" });

        group.Members.Remove(dependentUser);
        dependentUser.IsDependent = false;
        dependentUser.MasterUserId = null;

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Dependente removido com sucesso" });
    }

    /// <summary>
    /// Exclui um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpDelete("{groupId}")]
    public async Task<IActionResult> DeleteGroup(Guid groupId)
    {
        var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
        {
            return NotFound(new { Message = "Grupo não encontrado" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (group.CreatedById != userId)
        {
            return Unauthorized(new { Message = "Usuário não autorizado a excluir este grupo" });
        }

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Grupo excluído com sucesso" });
    }
}
