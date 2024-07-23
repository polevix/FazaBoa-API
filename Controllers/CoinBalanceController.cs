using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FazaBoa_API.Models;
using FazaBoa_API.Data;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoinBalanceController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CoinBalanceController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtém o saldo de moedas de um usuário em um grupo.
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="groupId">ID do grupo</param>
    /// <returns>Retorna o saldo de moedas ou uma mensagem de erro</returns>
    [HttpGet("{userId}/{groupId}")]
    public async Task<IActionResult> GetBalance(string userId, int groupId)
    {
        var balance = await _context.CoinBalances.FirstOrDefaultAsync(b => b.UserId == userId && b.GroupId == groupId);
        if (balance == null)
        {
            return NotFound(new { Message = "Saldo não encontrado" });
        }

        return Ok(balance);
    }

    /// <summary>
    /// Adiciona moedas ao saldo de um usuário.
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="amount">Quantidade de moedas a serem adicionadas</param>
    /// <returns>Retorna o saldo atualizado ou uma mensagem de erro</returns>
    [HttpPost("{userId}/{groupId}/add")]
    public async Task<IActionResult> AddBalance(string userId, int groupId, [FromBody] int amount)
    {
        if (amount < 0)
        {
            return BadRequest(new { Message = "A quantidade deve ser positiva" });
        }

        var balance = await _context.CoinBalances.FirstOrDefaultAsync(b => b.UserId == userId && b.GroupId == groupId);
        if (balance == null)
        {
            balance = new CoinBalance { UserId = userId, GroupId = groupId, Balance = 0 };
            _context.CoinBalances.Add(balance);
        }

        balance.Balance += amount;
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Saldo atualizado", Balance = balance.Balance });
    }

    /// <summary>
    /// Deduz moedas do saldo de um usuário.
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="groupId">ID do grupo</param>
    /// <param name="amount">Quantidade de moedas a serem deduzidas</param>
    /// <returns>Retorna o saldo atualizado ou uma mensagem de erro</returns>
    [HttpPost("{userId}/{groupId}/spend")]
    public async Task<IActionResult> SpendBalance(string userId, int groupId, [FromBody] int amount)
    {
        if (amount < 0)
        {
            return BadRequest(new { Message = "A quantidade deve ser positiva" });
        }

        var balance = await _context.CoinBalances.FirstOrDefaultAsync(b => b.UserId == userId && b.GroupId == groupId);
        if (balance == null)
        {
            return NotFound(new { Message = "Saldo não encontrado" });
        }

        if (balance.Balance < amount)
        {
            return BadRequest(new { Message = "Saldo insuficiente" });
        }

        balance.Balance -= amount;
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Saldo atualizado", Balance = balance.Balance });
    }
}
