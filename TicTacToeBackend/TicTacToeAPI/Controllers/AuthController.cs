using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicTacToeAPI.Data;
using TicTacToeAPI.DTOs;
using TicTacToeAPI.Models;
using BCrypt.Net;

namespace TicTacToeAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly TicTacToeDbContext _context;

        public AuthController(TicTacToeDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Register new player account
        /// POST /api/auth/register
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
        {
            // Check if username already exists
            var existingPlayer = await _context.Players
                .FirstOrDefaultAsync(p => p.Username == request.Username);

            if (existingPlayer != null)
            {
                return BadRequest(new { message = "Username already taken" });
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create new player
            var player = new Player
            {
                Username = request.Username,
                PasswordHash = passwordHash,
                EloRating = 1000,
                GamesPlayed = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            // Return player info
            return Ok(new LoginResponse
            {
                PlayerId = player.Id,
                Username = player.Username,
                EloRating = player.EloRating
            });
        }

        /// <summary>
        /// Login existing player
        /// POST /api/auth/login
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            // Find player by username
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Username == request.Username);

            if (player == null)
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }

            // Verify password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, player.PasswordHash);

            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }

            // Return player info
            return Ok(new LoginResponse
            {
                PlayerId = player.Id,
                Username = player.Username,
                EloRating = player.EloRating
            });
        }
    }
}
