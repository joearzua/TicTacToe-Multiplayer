using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicTacToeAPI.Data;
using TicTacToeAPI.DTOs;

namespace TicTacToeAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaderboardController : ControllerBase
    {
        private readonly TicTacToeDbContext _context;

        public LeaderboardController(TicTacToeDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get top 10 players by ELO rating
        /// GET /api/leaderboard
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<LeaderboardEntry>>> GetLeaderboard()
        {
            // First, get the top players from database
            var topPlayers = await _context.Players
                .OrderByDescending(p => p.EloRating)
                .Take(10)
                .ToListAsync();

            // Then, add ranking in memory (can't do index in SQL)
            var leaderboard = topPlayers
                .Select((p, index) => new LeaderboardEntry
                {
                    Rank = index + 1,
                    Username = p.Username,
                    EloRating = p.EloRating,
                    GamesPlayed = p.GamesPlayed
                })
                .ToList();

            return Ok(leaderboard);
        }
    }
}
