using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudskipDB.Dto;
using MudskipDB.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SlimeDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HighscoreController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HighscoreController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔎 GET: api/DotHighscore/{dotLevel}
        [HttpGet("{dotLevel}")]
        public async Task<IActionResult> GetHighscoresByDot(int dotLevel)
        {
            // Ellenőrzés: van-e ilyen szint
            var level = await _context.Levels.FirstOrDefaultAsync(l => l.Id == dotLevel);
            if (level == null)
            {
                return NotFound("The selected DOT level does not exist.");
            }

            // Highscore lekérés az adott szintre
            var highscores = await _context.Highscores
                .Where(h => h.LevelId == dotLevel)
                .Include(h => h.User)
                .OrderByDescending(h => h.HighscoreValue)
                .Select(h => new HighscoreResponseDTO
                {
                    Username = h.User.Username,
                    HighscoreValue = h.HighscoreValue
                })
                .ToListAsync();

            if (!highscores.Any())
            {
                return NotFound("No highscores found for this DOT level.");
            }

            return Ok(highscores);
        }



        [HttpGet("my-highscores")]
        public async Task<IActionResult> GetMyHighscores()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized("You must be logged in to view your highscores.");

            // Csak a saját highscore-ok lekérése
            var userHighscores = await _context.Highscores
                .Where(h => h.UserId == userId)
                .Include(h => h.Level)
                .ToListAsync();

            if (!userHighscores.Any())
            {
                return NotFound("No highscores found for this user.");
            }

            // Pályánként a legjobb highscore kiválasztása
            var bestScoresPerLevel = userHighscores
                .GroupBy(h => h.LevelId)
                .Select(g => g.OrderByDescending(h => h.HighscoreValue).First())
                .ToList();

            // Összeállított válasz DTO, pálya nevével
            var result = bestScoresPerLevel
                .Select(h => new
                {
                    LevelName = h.Level.Name,
                    Highscore = h.HighscoreValue
                })
                .OrderBy(h => h.LevelName) // vagy egyéni sorrend, ha kell
                .ToList();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddHighscore([FromBody] Highscore highscore)
        {
            // 🔎 UserId lekérése session-ből
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized("You must be logged in to post a highscore.");

            // 🔎 User ellenőrzés
            var user = await _context.Users.FindAsync(userId);
            var level = await _context.Levels.FindAsync(highscore.LevelId);

            if (user == null || level == null)
            {
                return BadRequest("Invalid user or level.");
            }

            // 🔎 Megnézzük, van-e már rekord erre a userre és pályára
            var existingHighscore = await _context.Highscores
                .FirstOrDefaultAsync(h => h.UserId == user.Id && h.LevelId == highscore.LevelId);

            if (existingHighscore != null)
            {
                // Ha az új pontszám nem nagyobb, nem frissítünk
                if (existingHighscore.HighscoreValue >= highscore.HighscoreValue)
                {
                    return Ok(existingHighscore);
                }

                // Frissítjük a rekordot
                existingHighscore.HighscoreValue = highscore.HighscoreValue;
                _context.Highscores.Update(existingHighscore);
            }
            else
            {
                // Új rekord beszúrása
                _context.Highscores.Add(new Highscore
                {
                    UserId = user.Id,
                    LevelId = highscore.LevelId,
                    HighscoreValue = highscore.HighscoreValue
                });
            }

            await _context.SaveChangesAsync();

            return Ok("Highscore saved successfully.");
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHighscore(int id)
        {
            // 🔐 Bejelentkezett felhasználó lekérése session-ből
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized("You must be logged in as admin.");

            // 🔎 Felhasználó lekérése az adatbázisból
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Role != "Admin")
            {
                return Forbid("Only admin users can delete highscores.");
            }

            // 🔎 Highscore megkeresése
            var highscore = await _context.Highscores.FindAsync(id);
            if (highscore == null)
            {
                return NotFound("Highscore not found.");
            }

            // 🔥 Törlés
            _context.Highscores.Remove(highscore);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
