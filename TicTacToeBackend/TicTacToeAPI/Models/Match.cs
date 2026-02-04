using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicTacToeAPI.Models
{
    /// <summary>
    /// Match record - tracks who played and who won
    /// </summary>
    [Table("matches")]
    public class Match
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("player1_id")]
        public int Player1Id { get; set; }

        [Required]
        [Column("player2_id")]
        public int Player2Id { get; set; }

        [Column("winner_id")]
        public int? WinnerId { get; set; } // Nullable for draws

        [Column("played_at")]
        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("Player1Id")]
        public Player Player1 { get; set; }

        [ForeignKey("Player2Id")]
        public Player Player2 { get; set; }

        [ForeignKey("WinnerId")]
        public Player Winner { get; set; }
    }
}
