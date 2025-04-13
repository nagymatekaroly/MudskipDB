using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MudskipDB.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LevelStatsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LevelStatsController(ApplicationDbContext context)
        {
            _context = context;
        }


        [HttpGet]
        public async Task<IActionResult> GetAllStats()
        {
            var stats = await _context.LevelStats
                .Include(ls => ls.Level)
                .Select(ls => new
                {
                    LevelName = ls.Level.Name,
                    CompletionCount = ls.CompletionCount
                })
                .ToListAsync();

            return Ok(stats);
        }


        [HttpPut("{levelId}")]
        public async Task<IActionResult> UpdateCompletionCount(int levelId, [FromBody] int newCount)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized("A módosításhoz be kell jelentkezni.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Role != "Admin")
                return Forbid("Csak admin jogosultságú felhasználók módosíthatják a statisztikákat.");

            var stats = await _context.LevelStats.FirstOrDefaultAsync(ls => ls.LevelId == levelId);
            if (stats == null)
                return NotFound("Nem található statisztika ehhez a pályához.");

            stats.CompletionCount = newCount;
            _context.LevelStats.Update(stats);
            await _context.SaveChangesAsync();

            return Ok("A teljesítésszámláló sikeresen frissítve.");
        }

        [HttpDelete("{levelId}")]
        public async Task<IActionResult> DeleteLevelStats(int levelId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized("Csak bejelentkezett admin törölhet statisztikát.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Role != "Admin")
                return Forbid("Csak admin törölhet statisztikát.");

            var stats = await _context.LevelStats.FirstOrDefaultAsync(ls => ls.LevelId == levelId);
            if (stats == null)
                return NotFound("Ehhez a pályához nem található statisztika.");

            _context.LevelStats.Remove(stats);
            await _context.SaveChangesAsync();

            return Ok("A statisztika sikeresen törölve lett.");
        }
    }
}