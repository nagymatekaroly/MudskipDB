using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudskipDB.Dto;
using MudskipDB.Models;

namespace MudskipDB.Controllers
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
            // 📌 Ellenőrzés: létezik-e ilyen szint
            var level = await _context.Levels.FirstOrDefaultAsync(l => l.Id == dotLevel);
            if (level == null)
            {
                return NotFound("A megadott DOT szint nem létezik.");
            }

            // 📌 Highscore-ok lekérése az adott szinthez
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
                return NotFound("Ehhez a DOT szinthez még nincs highscore bejegyzés.");
            }

            return Ok(highscores);
        }

        // 🔎 GET: Saját highscore-ok lekérése (bejelentkezett userhez)
        [HttpGet("my-highscores")]
        public async Task<IActionResult> GetMyHighscores([FromQuery] int? userId = null)
        {
            // 🔧 SESSION fallback (ha nincs userId átadva, akkor próbál sessionből)
            userId ??= HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized("Nincs megadva userId, és nem vagy bejelentkezve.");

            var userHighscores = await _context.Highscores
                .Where(h => h.UserId == userId)
                .Include(h => h.Level)
                .ToListAsync();

            if (!userHighscores.Any())
            {
                return NotFound("A felhasználóhoz még nem tartozik highscore.");
            }

            var bestScoresPerLevel = userHighscores
                .GroupBy(h => h.LevelId)
                .Select(g => g.OrderByDescending(h => h.HighscoreValue).First())
                .ToList();

            var result = bestScoresPerLevel
                .Select(h => new
                {
                    LevelName = h.Level.Name,
                    Highscore = h.HighscoreValue
                })
                .OrderBy(h => h.LevelName)
                .ToList();

            return Ok(result);
        }

        // 🔹 Highscore hozzáadása vagy frissítése
        [HttpPost]
        public async Task<IActionResult> AddHighscore([FromBody] HighscorePostDto input)
        {
            // ✅ Sessionből felhasználó azonosító lekérése
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized("Highscore beküldéshez be kell jelentkezni.");

            // ✅ Felhasználó és pálya lekérése
            var user = await _context.Users.FindAsync(userId);
            var level = await _context.Levels.FirstOrDefaultAsync(l => l.Name == input.LevelName);

            if (user == null || level == null)
                return BadRequest("Hibás felhasználó vagy pályanév.");

            // ✅ Highscore kezelés: meglévő rekord módosítása vagy új beszúrása
            var existingHighscore = await _context.Highscores
                .FirstOrDefaultAsync(h => h.UserId == user.Id && h.LevelId == level.Id);

            if (existingHighscore != null)
            {
                if (existingHighscore.HighscoreValue >= input.HighscoreValue)
                {
                    // ✅ A meglévő highscore jobb vagy egyenlő – csak LevelStats növelés
                    await IncrementLevelStats(level.Id);
                    return Ok(existingHighscore);
                }

                // ✅ Új, jobb highscore – frissítés
                existingHighscore.HighscoreValue = input.HighscoreValue;
                _context.Highscores.Update(existingHighscore);
            }
            else
            {
                // ✅ Még nincs highscore ehhez a pályához – új rekord
                _context.Highscores.Add(new Highscore
                {
                    UserId = user.Id,
                    LevelId = level.Id,
                    HighscoreValue = input.HighscoreValue
                });
            }

            // ✅ Minden esetben növeljük a pálya statisztikáját
            await IncrementLevelStats(level.Id);

            await _context.SaveChangesAsync();
            return Ok("Highscore sikeresen mentve.");
        }

        // 🔄 LevelStats növelése, ha végigmentek egy pályán
        private async Task IncrementLevelStats(int levelId)
        {
            var stats = await _context.LevelStats.FirstOrDefaultAsync(ls => ls.LevelId == levelId);

            if (stats == null)
            {
                // 📌 Ha még nincs stat, létrehozunk egy új bejegyzést
                _context.LevelStats.Add(new LevelStats
                {
                    LevelId = levelId,
                    CompletionCount = 1
                });
            }
            else
            {
                // 📌 Már van ilyen – növeljük a számlálót
                stats.CompletionCount += 1;
                _context.LevelStats.Update(stats);
            }

            await _context.SaveChangesAsync(); // ← Fontos: menteni kell külön is!
        }

        // 🔥 Highscore törlése (csak admin jogosultsággal)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHighscore(int id)
        {
            // 🔐 Bejelentkezett felhasználó lekérése a session alapján
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized("Csak admin törölhet highscore-t.");

            // 🔍 Felhasználó lekérése az adatbázisból
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Role != "Admin")
            {
                return Forbid("Csak admin jogosultsággal lehet törölni highscore-t.");
            }

            // 🔍 Highscore bejegyzés megkeresése
            var highscore = await _context.Highscores.FindAsync(id);
            if (highscore == null)
            {
                return NotFound("A highscore nem található.");
            }

            // 🗑️ Törlés végrehajtása
            _context.Highscores.Remove(highscore);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("by-level")]
        public async Task<IActionResult> GetHighscoreForLevel([FromQuery] string levelName)
        {
            // ✅ Sessionből felhasználó azonosító lekérése
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized("Highscore lekéréshez be kell jelentkezni.");

            // ✅ Pálya lekérése név alapján
            var level = await _context.Levels.FirstOrDefaultAsync(l => l.Name == levelName);
            if (level == null)
                return BadRequest("Hibás pályanév.");

            // ✅ Highscore rekord lekérése az adott user + pályára
            var highscore = await _context.Highscores
                .Include(h => h.User)
                .FirstOrDefaultAsync(h => h.UserId == userId && h.LevelId == level.Id);

            if (highscore == null)
                return Ok(null); // Még nincs rekord

            // ✅ DTO válasz
            return Ok(new HighscoreCheckDto
            {
                Username = highscore.User.Username,
                HighscoreValue = highscore.HighscoreValue,
                LevelName = levelName
            });
        
        }

    }
}
