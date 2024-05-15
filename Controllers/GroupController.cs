using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FazaBoa_API.Models;
using FazaBoa_API.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public GroupController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Cria um novo grupo.
    /// </summary>
    /// <param name="group">Modelo contendo os dados do grupo</param>
    /// <returns>Retorna o grupo criado ou uma mensagem de erro</returns>
    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] Group group)
    {
        if (string.IsNullOrEmpty(group.Name))
        {
            return BadRequest(new { Message = "Invalid group data" });
        }

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();
        return Ok(group);
    }

    /// <summary>
    /// Obtém os detalhes de um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <returns>Retorna os detalhes do grupo ou uma mensagem de erro</returns>
    [HttpGet("{groupId}")]
    public async Task<IActionResult> GetGroupDetails(int groupId)
    {
        var group = await _context.Groups
            .Include(g => g.Members)
            .Include(g => g.Challenges)
            .Include(g => g.Rewards)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            return NotFound(new { Message = "Group not found" });
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
    public async Task<IActionResult> AddMember(int groupId, [FromBody] string userId)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
            return NotFound(new { Message = "Group not found" });

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { Message = "User not found" });

        if (!group.Members.Contains(user))
        {
            group.Members.Add(user);
            await _context.SaveChangesAsync();
        }

        return Ok(new { Message = "Member added successfully" });
    }

    /// <summary>
    /// Remove um membro de um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("{groupId}/remove-member")]
    public async Task<IActionResult> RemoveMember(int groupId, [FromBody] string userId)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
            return NotFound(new { Message = "Group not found" });

        var user = group.Members.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return NotFound(new { Message = "Member not found in group" });

        group.Members.Remove(user);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Member removed successfully" });
    }

    /// <summary>
    /// Marca um membro como dependente.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("{groupId}/mark-dependent")]
    public async Task<IActionResult> MarkAsDependent(int groupId, [FromBody] string userId)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
            return NotFound(new { Message = "Group not found" });

        var user = group.Members.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return NotFound(new { Message = "Member not found in group" });

        var masterUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(masterUserId))
            return Unauthorized(new { Message = "Master user not found" });

        var applicationUser = await _context.Users.FindAsync(userId);
        if (applicationUser == null)
            return NotFound(new { Message = "User not found" });

        applicationUser.IsDependent = true;
        applicationUser.MasterUserId = masterUserId;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Member marked as dependent successfully" });
    }

    /// <summary>
    /// Adiciona um dependente a um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="dependentEmail">Email do dependente</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("{groupId}/add-dependent")]
    public async Task<IActionResult> AddDependent(int groupId, [FromBody] string dependentEmail)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
            return NotFound(new { Message = "Group not found" });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var masterUser = await _context.Users.FindAsync(userId);
        if (masterUser == null || group.CreatedById != userId)
            return Unauthorized(new { Message = "User not authorized to add dependents" });

        var dependentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dependentEmail);
        if (dependentUser == null)
            return NotFound(new { Message = "User not found" });

        if (!group.Members.Contains(dependentUser))
            group.Members.Add(dependentUser);

        dependentUser.IsDependent = true;
        dependentUser.MasterUserId = userId;

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Dependent added successfully" });
    }

    /// <summary>
    /// Obtém todos os dependentes de um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <returns>Retorna uma lista de dependentes ou uma mensagem de erro</returns>
    [HttpGet("{groupId}/dependents")]
    public async Task<IActionResult> GetDependents(int groupId)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
            return NotFound(new { Message = "Group not found" });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (group.CreatedById != userId)
            return Unauthorized(new { Message = "User not authorized to view dependents" });

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
    public async Task<IActionResult> InviteMember(int groupId, [FromBody] string email)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
        {
            return NotFound(new { Message = "Group not found" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (group.CreatedById != userId)
        {
            return Unauthorized(new { Message = "User not authorized to invite members" });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return NotFound(new { Message = "User with the provided email not found" });
        }

        if (group.Members.Any(m => m.Id == user.Id))
        {
            return BadRequest(new { Message = "User is already a member of the group" });
        }

        group.Members.Add(user);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "User invited successfully" });
    }

    /// <summary>
    /// Aceita um convite para entrar no grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("{groupId}/accept-invite")]
    public async Task<IActionResult> AcceptInvite(int groupId, [FromBody] string userId)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        var user = await _context.Users.FindAsync(userId);

        if (group == null || user == null)
        {
            return NotFound(new { Message = "Group or User not found" });
        }

        if (group.Members.Any(m => m.Id == user.Id))
        {
            return BadRequest(new { Message = "User is already a member of the group" });
        }

        group.Members.Add(user);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Invite accepted" });
    }

    /// <summary>
    /// Atualiza os dados de um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="updatedGroup">Modelo contendo os dados atualizados do grupo</param>
    /// <returns>Retorna o grupo atualizado ou uma mensagem de erro</returns>
    [HttpPut("{groupId}")]
    public async Task<IActionResult> UpdateGroup(int groupId, [FromBody] Group updatedGroup)
    {
        var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
        {
            return NotFound(new { Message = "Group not found" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (group.CreatedById != userId)
        {
            return Unauthorized(new { Message = "User not authorized to edit this group" });
        }

        var existingGroup = await _context.Groups
            .FirstOrDefaultAsync(g => g.CreatedById == userId && g.Name == updatedGroup.Name && g.Id != groupId);

        if (existingGroup != null)
        {
            return BadRequest(new { Message = "A group with the same name already exists" });
        }

        group.Name = updatedGroup.Name ?? group.Name;
        group.PhotoUrl = updatedGroup.PhotoUrl ?? group.PhotoUrl;

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Group updated successfully", Group = group });
    }

    /// <summary>
    /// Remove um dependente de um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpDelete("{groupId}/remove-dependent/{userId}")]
    public async Task<IActionResult> RemoveDependent(int groupId, string userId)
    {
        var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
            return NotFound(new { Message = "Group not found" });

        var masterUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (group.CreatedById != masterUserId)
            return Unauthorized(new { Message = "User not authorized to remove dependents" });

        var dependentUser = group.Members.FirstOrDefault(u => u.Id == userId && u.IsDependent);
        if (dependentUser == null)
            return NotFound(new { Message = "Dependent not found in group" });

        group.Members.Remove(dependentUser);
        dependentUser.IsDependent = false;
        dependentUser.MasterUserId = null;

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Dependent removed successfully" });
    }

    /// <summary>
    /// Exclui um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpDelete("{groupId}")]
    public async Task<IActionResult> DeleteGroup(int groupId)
    {
        var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
        {
            return NotFound(new { Message = "Group not found" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (group.CreatedById != userId)
        {
            return Unauthorized(new { Message = "User not authorized to delete this group" });
        }

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Group deleted successfully" });
    }
}
