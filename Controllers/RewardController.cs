using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FazaBoa_API.Models;
using FazaBoa_API.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RewardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RewardController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Cria uma nova recompensa.
    /// </summary>
    /// <param name="reward">Modelo contendo os dados da recompensa</param>
    /// <returns>Retorna a recompensa criada ou uma mensagem de erro</returns>
    [HttpPost]
    public async Task<IActionResult> CreateReward([FromBody] Reward reward)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var group = await _context.Groups.FindAsync(reward.GroupId);

        if (group == null || group.CreatedById != userId)
        {
            return Unauthorized(new { Message = "Usuário não autorizado a criar recompensas para este grupo" });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Rewards.Add(reward);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { Message = "Erro ao criar recompensa" });
        }

        return Ok(reward);
    }

    /// <summary>
    /// Obtém todas as recompensas de um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <returns>Retorna uma lista de recompensas ou uma mensagem de erro</returns>
    [HttpGet("group/{groupId}")]
    public async Task<IActionResult> GetRewardsByGroup(int groupId)
    {
        var rewards = await _context.Rewards.Where(r => r.GroupId == groupId).ToListAsync();
        return Ok(rewards.Select(r => new
        {
            r.Id,
            r.Description,
            r.RequiredCoins
        }));
    }

    /// <summary>
    /// Exclui uma recompensa.
    /// </summary>
    /// <param name="id">ID da recompensa</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReward(int id)
    {
        var reward = await _context.Rewards.FindAsync(id);
        if (reward == null)
        {
            return NotFound(new { Message = "Recompensa não encontrada" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var group = await _context.Groups.FindAsync(reward.GroupId);

        if (group == null || group.CreatedById != userId)
        {
            return Unauthorized(new { Message = "Usuário não autorizado a excluir recompensas para este grupo" });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Rewards.Remove(reward);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { Message = "Erro ao excluir recompensa" });
        }

        return Ok(new { Message = "Recompensa excluída com sucesso" });
    }

    /// <summary>
    /// Resgata uma recompensa.
    /// </summary>
    /// <param name="rewardId">ID da recompensa</param>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Retorna o saldo atualizado ou uma mensagem de erro</returns>
    [HttpPost("{rewardId}/redeem")]
    public async Task<IActionResult> RedeemReward(int rewardId, [FromBody] string userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var reward = await _context.Rewards.FindAsync(rewardId);
            var user = await _context.Users.Include(u => u.Groups).FirstOrDefaultAsync(u => u.Id == userId);
            var balance = await _context.CoinBalances.FirstOrDefaultAsync(b => b.UserId == userId && b.GroupId == reward.GroupId);

            if (reward == null || user == null || balance == null || !user.Groups.Any(g => g.Id == reward.GroupId))
                return NotFound(new { Message = "Recompensa, usuário ou saldo não encontrados, ou usuário não faz parte do grupo" });

            if (balance.Balance < reward.RequiredCoins)
                return BadRequest(new { Message = "Moedas insuficientes" });

            balance.Balance -= reward.RequiredCoins;
            _context.RewardTransactions.Add(new RewardTransaction
            {
                UserId = userId,
                RewardId = rewardId,
                Timestamp = DateTime.UtcNow
            });

            _context.CoinTransactions.Add(new CoinTransaction
            {
                UserId = userId,
                GroupId = reward.GroupId,
                Amount = -reward.RequiredCoins,
                Description = $"Recompensa resgatada: {reward.Description}",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { Message = "Recompensa resgatada com sucesso", NewBalance = balance.Balance });
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obtém todas as recompensas resgatadas por um usuário em um grupo.
    /// </summary>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Retorna uma lista de recompensas resgatadas ou uma mensagem de erro</returns>
    [HttpGet("group/{groupId}/redeemed-by/{userId}")]
    public async Task<IActionResult> GetRewardsRedeemedByUserInGroup(int groupId, string userId)
    {
        var rewards = await _context.RewardTransactions
            .Include(rt => rt.Reward)
            .Where(rt => rt.Reward.GroupId == groupId && rt.UserId == userId)
            .ToListAsync();

        if (rewards == null || !rewards.Any())
        {
            return NotFound(new { Message = "Nenhuma recompensa resgatada pelo usuário neste grupo" });
        }

        return Ok(rewards.Select(rt => new
        {
            rt.Reward.Id,
            rt.Reward.Description,
            rt.Reward.RequiredCoins,
            rt.Timestamp
        }));
    }
}
