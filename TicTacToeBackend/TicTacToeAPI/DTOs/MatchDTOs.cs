using System.ComponentModel.DataAnnotations;

namespace TicTacToeAPI.DTOs
{
    /// <summary>
    /// Request to save match result
    /// </summary>
    public class MatchRequest
    {
        [Required]
        public int Player1Id { get; set; }

        [Required]
        public int Player2Id { get; set; }

        public int? WinnerId { get; set; } // Null for draw
    }

    /// <summary>
    /// Entry in leaderboard
    /// </summary>
    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string Username { get; set; }
        public int EloRating { get; set; }
        public int GamesPlayed { get; set; }
    }
}
