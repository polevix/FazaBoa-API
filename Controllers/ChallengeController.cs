using Microsoft.AspNetCore.Mvc;
using FazaBoa_API.Models;
using FazaBoa_API.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChallengeController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ChallengeController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Cria um novo desafio.
    /// </summary>
    /// <param name="challenge">Modelo contendo os dados do desafio</param>
    /// <returns>Retorna o desafio criado ou uma mensagem de erro</returns>
    [HttpPost]
    public async Task<IActionResult> CreateChallenge([FromBody] Challenge challenge)
    {
        if (!ModelState.IsValid || string.IsNullOrEmpty(challenge.Name) || challenge.GroupId <= 0)
        {
            return BadRequest(new { Message = "Dados do desafio inválidos" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized(new { Message = "Usuário não autorizado" });
        }

        challenge.CreatedById = userId;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Challenges.Add(challenge);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { Message = "Erro ao criar desafio" });
        }

        return Ok(challenge);
    }


    /// <summary>
    /// Obtém os detalhes de um desafio.
    /// </summary>
    /// <param name="id">ID do desafio</param>
    /// <returns>Retorna os detalhes do desafio ou uma mensagem de erro</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetChallengeDetails(int id)
    {
        var challenge = await _context.Challenges
            .Include(c => c.AssignedUsers)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (challenge == null)
        {
            return NotFound(new { Message = "Desafio não encontrado" });
        }

        return Ok(new
        {
            challenge.Id,
            challenge.Name,
            challenge.Description,
            challenge.CoinValue,
            challenge.StartDate,
            challenge.EndDate,
            challenge.IsDaily,
            AssignedUsers = challenge.AssignedUsers.Select(u => new { u.Id, u.FullName, u.Email })
        });
    }

    /// <summary>
    /// Obtém todos os desafios criados por um usuário.
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Retorna uma lista de desafios criados pelo usuário</returns>
    [HttpGet("created-by/{userId}")]
    public async Task<IActionResult> GetChallengesCreatedByUser(string userId)
    {
        var challenges = await _context.Challenges
            .Where(c => c.CreatedById == userId)
            .ToListAsync();
        return Ok(challenges.Select(c => new
        {
            c.Id,
            c.Name,
            c.Description,
            c.CoinValue,
            c.StartDate,
            c.EndDate,
            c.IsDaily
        }));
    }

    /// <summary>
    /// Obtém todos os desafios atribuídos a um usuário.
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Retorna uma lista de desafios atribuídos ao usuário</returns>
    [HttpGet("assigned-to/{userId}")]
    public async Task<IActionResult> GetChallengesAssignedToUser(string userId)
    {
        var challenges = await _context.Challenges
            .Where(c => c.AssignedUsers.Any(u => u.Id == userId))
            .ToListAsync();
        return Ok(challenges.Select(c => new
        {
            c.Id,
            c.Name,
            c.Description,
            c.CoinValue,
            c.StartDate,
            c.EndDate,
            c.IsDaily
        }));
    }

    /// <summary>
    /// Atribui usuários a um desafio.
    /// </summary>
    /// <param name="id">ID do desafio</param>
    /// <param name="userIds">Lista de IDs de usuários</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPut("{id}/assign")]
    public async Task<IActionResult> AssignChallengeToUsers(int id, [FromBody] List<string> userIds)
    {
        var challenge = await _context.Challenges.Include(c => c.AssignedUsers).FirstOrDefaultAsync(c => c.Id == id);
        if (challenge == null)
        {
            return NotFound(new { Message = "Desafio não encontrado" });
        }

        var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
        foreach (var user in users)
        {
            if (!challenge.AssignedUsers.Contains(user))
            {
                challenge.AssignedUsers.Add(user);
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Usuários atribuídos ao desafio com sucesso" });

    }

    /// <summary>
    /// Atualiza um desafio.
    /// </summary>
    /// <param name="id">ID do desafio</param>
    /// <param name="updatedChallenge">Modelo contendo os dados atualizados do desafio</param>
    /// <returns>Retorna o desafio atualizado ou uma mensagem de erro</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateChallenge(int id, [FromBody] Challenge updatedChallenge)
    {
        var challenge = await _context.Challenges.FindAsync(id);
        if (challenge == null)
        {
            return NotFound(new { Message = "Desafio não encontrado" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (challenge.CreatedById != userId)
        {
            return Unauthorized(new { Message = "Usuário não autorizado a atualizar este desafio" });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            challenge.Name = updatedChallenge.Name;
            challenge.Description = updatedChallenge.Description;
            challenge.CoinValue = updatedChallenge.CoinValue;
            challenge.IsDaily = updatedChallenge.IsDaily;
            challenge.StartDate = updatedChallenge.StartDate;
            challenge.EndDate = updatedChallenge.EndDate;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { Message = "Erro ao atualizar desafio" });
        }

        return Ok(challenge);
    }


    /// <summary>
    /// Exclui um desafio.
    /// </summary>
    /// <param name="id">ID do desafio</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteChallenge(int id)
    {
        var challenge = await _context.Challenges.FindAsync(id);
        if (challenge == null)
        {
            return NotFound(new { Message = "Desafio não encontrado" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (challenge.CreatedById != userId)
        {
            return Unauthorized(new { Message = "Usuário não autorizado a excluir este desafio" });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Challenges.Remove(challenge);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { Message = "Erro ao excluir desafio" });
        }

        return Ok(new { Message = "Desafio excluído com sucesso" });
    }


    /// <summary>
    /// Marca um desafio como concluído.
    /// </summary>
    /// <param name="id">ID do desafio</param>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> MarkChallengeAsCompleted(int id, [FromBody] string userId)
    {
        var challenge = await _context.Challenges.Include(c => c.AssignedUsers).FirstOrDefaultAsync(c => c.Id == id);
        if (challenge == null)
        {
            return NotFound(new { Message = "Desafio não encontrado" });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null || (!challenge.AssignedUsers.Contains(user) && user.MasterUserId != challenge.CreatedById))
        {
            return Unauthorized(new { Message = "Usuário não autorizado a completar este desafio" });
        }

        if (_context.CompletedChallenges.Any(cc => cc.ChallengeId == id && cc.UserId == userId))
        {
            return BadRequest(new { Message = "Desafio já completado pelo usuário" });
        }

        var completedChallenge = new CompletedChallenge
        {
            ChallengeId = id,
            UserId = userId,
            CompletedDate = DateTime.UtcNow,
            IsValidated = false
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.CompletedChallenges.Add(completedChallenge);
            await AddCoinsToUserBalance(userId, challenge.GroupId, challenge.CoinValue);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { Message = "Erro ao marcar desafio como completado" });
        }

        return Ok(new { Message = "Desafio marcado como completado com sucesso" });
    }


    /// <summary>
    /// Valida a conclusão de um desafio.
    /// </summary>
    /// <param name="id">ID do desafio</param>
    /// <param name="model">Modelo contendo o ID do usuário e se o desafio foi concluído</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpPost("{id}/validate")]
    public async Task<IActionResult> ValidateChallengeCompletion(int id, [FromBody] ValidateChallenge model)
    {
        var challenge = await _context.Challenges.FirstOrDefaultAsync(c => c.Id == id);
        if (challenge == null)
        {
            return NotFound(new { Message = "Desafio não encontrado" });
        }

        var completedChallenge = await _context.CompletedChallenges
            .FirstOrDefaultAsync(cc => cc.ChallengeId == id && cc.UserId == model.UserId);

        if (completedChallenge == null)
        {
            return NotFound(new { Message = "Nenhum desafio completado encontrado para este usuário" });
        }

        var creatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (challenge.CreatedById != creatorId)
        {
            return Unauthorized(new { Message = "Usuário não autorizado a validar este desafio" });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            completedChallenge.IsValidated = model.IsCompleted;

            if (model.IsCompleted)
            {
                await AddCoinsToUserBalance(model.UserId, challenge.GroupId, challenge.CoinValue);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { Message = "Desafio validado com sucesso", CoinsAwarded = challenge.CoinValue });
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { Message = "Erro ao validar o desafio" });
        }
    }


    /// <summary>
    /// Adiciona moedas ao saldo do usuário.
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="coinsToAdd">Número de moedas a serem adicionadas</param>
    private async Task AddCoinsToUserBalance(string userId, int groupId, int coinsToAdd)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var balance = await _context.CoinBalances.FirstOrDefaultAsync(b => b.UserId == userId && b.GroupId == groupId);
            if (balance == null)
            {
                balance = new CoinBalance { UserId = userId, GroupId = groupId, Balance = 0 };
                _context.CoinBalances.Add(balance);
            }
            balance.Balance += coinsToAdd;

            _context.CoinTransactions.Add(new CoinTransaction
            {
                UserId = userId,
                GroupId = groupId,
                Amount = coinsToAdd,
                Description = "Desafio Concluído",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
