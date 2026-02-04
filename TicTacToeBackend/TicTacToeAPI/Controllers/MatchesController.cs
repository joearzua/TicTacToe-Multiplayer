using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicTacToeAPI.Data;
using TicTacToeAPI.DTOs;
using TicTacToeAPI.Models;

namespace TicTacToeAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MatchesController : ControllerBase
    {
        private readonly TicTacToeDbContext _context;

        public MatchesController(TicTacToeDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Save match result and update ELO ratings
        /// POST /api/matches
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> SaveMatch([FromBody] MatchRequest request)
        {
            // Validate players exist
            var player1 = await _context.Players.FindAsync(request.Player1Id);
            var player2 = await _context.Players.FindAsync(request.Player2Id);

            if (player1 == null || player2 == null)
            {
                return BadRequest(new { message = "Invalid player IDs" });
            }

            // Create match record
            var match = new Match
            {
                Player1Id = request.Player1Id,
                Player2Id = request.Player2Id,
                WinnerId = request.WinnerId,
                PlayedAt = DateTime.UtcNow
            };

            _context.Matches.Add(match);

            // Update ELO ratings
            UpdateEloRatings(player1, player2, request.WinnerId);

            // Increment games played
            player1.GamesPlayed++;
            player2.GamesPlayed++;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Match saved successfully",
                player1NewRating = player1.EloRating,
                player2NewRating = player2.EloRating
            });
        }

        /// <summary>
        /// Calculate and update ELO ratings based on match result
        /// </summary>
        private void UpdateEloRatings(Player player1, Player player2, int? winnerId)
        {
            const int K = 32; // K-factor (rating change sensitivity)

            // Expected scores
            double expectedPlayer1 = 1.0 / (1.0 + Math.Pow(10, (player2.EloRating - player1.EloRating) / 400.0));
            double expectedPlayer2 = 1.0 / (1.0 + Math.Pow(10, (player1.EloRating - player2.EloRating) / 400.0));

            // Actual scores
            double actualPlayer1;
            double actualPlayer2;

            if (winnerId == null)
            {
                // Draw
                actualPlayer1 = 0.5;
                actualPlayer2 = 0.5;
            }
            else if (winnerId == player1.Id)
            {
                // Player 1 won
                actualPlayer1 = 1.0;
                actualPlayer2 = 0.0;
            }
            else
            {
                // Player 2 won
                actualPlayer1 = 0.0;
                actualPlayer2 = 1.0;
            }

            // Update ratings
            player1.EloRating += (int)(K * (actualPlayer1 - expectedPlayer1));
            player2.EloRating += (int)(K * (actualPlayer2 - expectedPlayer2));

            // Ensure ratings don't go below 0
            if (player1.EloRating < 0) player1.EloRating = 0;
            if (player2.EloRating < 0) player2.EloRating = 0;
        }
    }
}
