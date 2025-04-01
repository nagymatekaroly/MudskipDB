using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("[controller]")]
public class LevelStatsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LevelStatsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 🔍 1. Lekérdezi az összes pályastatisztikát
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

    // 🔧 2. Csak admin módosíthatja manuálisan a CompletionCount értéket
    [HttpPut("{levelId}")]
    public async Task<IActionResult> UpdateCompletionCount(int levelId, [FromBody] int newCount)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized("Not logged in.");

        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.Role != "Admin")
            return Forbid("Only admins can modify stats.");

        var stats = await _context.LevelStats.FirstOrDefaultAsync(ls => ls.LevelId == levelId);
        if (stats == null)
            return NotFound("No stats found for this level.");

        stats.CompletionCount = newCount;
        _context.LevelStats.Update(stats);
        await _context.SaveChangesAsync();

        return Ok("Completion count updated successfully.");
    }
}
