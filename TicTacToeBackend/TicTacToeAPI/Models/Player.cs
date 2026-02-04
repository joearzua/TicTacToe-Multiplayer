using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicTacToeAPI.Models
{
    /// <summary>
    /// Player account - stores username, password hash, and ELO rating
    /// </summary>
    [Table("players")]
    public class Player
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("username")]
        public string Username { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; }

        [Column("elo_rating")]
        public int EloRating { get; set; } = 1000; // Starting ELO

        [Column("games_played")]
        public int GamesPlayed { get; set; } = 0;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
