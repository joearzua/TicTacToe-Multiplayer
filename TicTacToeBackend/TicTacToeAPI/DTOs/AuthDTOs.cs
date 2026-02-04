using System.ComponentModel.DataAnnotations;

namespace TicTacToeAPI.DTOs
{
    /// <summary>
    /// Request for player registration
    /// </summary>
    public class RegisterRequest
    {
        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }
    }

    /// <summary>
    /// Request for player login
    /// </summary>
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }

    /// <summary>
    /// Response after successful login
    /// </summary>
    public class LoginResponse
    {
        public int PlayerId { get; set; }
        public string Username { get; set; }
        public int EloRating { get; set; }
    }
}
