﻿using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> AddHighscore([FromBody] HighscorePostDto input)
        {
            // ✅ Sessionből userId
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized("You must be logged in to post a highscore.");

            // ✅ User + pálya lekérés
            var user = await _context.Users.FindAsync(userId);
            var level = await _context.Levels.FirstOrDefaultAsync(l => l.Name == input.LevelName);

            if (user == null || level == null)
                return BadRequest("Invalid user or level.");

            // ✅ Highscore kezelése
            var existingHighscore = await _context.Highscores
                .FirstOrDefaultAsync(h => h.UserId == user.Id && h.LevelId == level.Id);

            if (existingHighscore != null)
            {
                if (existingHighscore.HighscoreValue >= input.HighscoreValue)
                {
                    // ✅ Highscore nem javult, de LevelStats-ot akkor is növeljük
                    await IncrementLevelStats(level.Id);
                    return Ok(existingHighscore);
                }

                existingHighscore.HighscoreValue = input.HighscoreValue;
                _context.Highscores.Update(existingHighscore);
            }
            else
            {
                _context.Highscores.Add(new Highscore
                {
                    UserId = user.Id,
                    LevelId = level.Id,
                    HighscoreValue = input.HighscoreValue
                });
            }

            // ✅ LevelStats frissítése mindig megtörténik
            await IncrementLevelStats(level.Id);

            await _context.SaveChangesAsync();
            return Ok("Highscore saved successfully.");
        }

        private async Task IncrementLevelStats(int levelId)
        {
            var stats = await _context.LevelStats.FirstOrDefaultAsync(ls => ls.LevelId == levelId);

            if (stats == null)
            {
                _context.LevelStats.Add(new LevelStats
                {
                    LevelId = levelId,
                    CompletionCount = 1
                });
            }
            else
            {
                stats.CompletionCount += 1;
                _context.LevelStats.Update(stats);
            }
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
