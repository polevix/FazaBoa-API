using FazaBoa_API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FazaBoa_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CoinTransactionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CoinTransactionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetTransactionsByUser(string userId)
        {
            var transactions = await _context.CoinTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
            return Ok(transactions);
        }

        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetTransactionsByGroup(Guid groupId)
        {
            var transactions = await _context.CoinTransactions
                .Where(t => t.GroupId == groupId)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
            return Ok(transactions);
        }

        [HttpGet("user/{userId}/group/{groupId}")]
        public async Task<IActionResult> GetTransactionsByUserAndGroup(string userId, Guid groupId)
        {
            var transactions = await _context.CoinTransactions
                .Where(t => t.UserId == userId && t.GroupId == groupId)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
            return Ok(transactions);
        }
    }

}