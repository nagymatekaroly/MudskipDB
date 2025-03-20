using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudskipDB.Models;

namespace SlimeDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LevelController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LevelController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Level
        // Az összes pálya lekérése
        [HttpGet]
        public async Task<IActionResult> GetLevels()
        {
            var levels = await _context.Levels.ToListAsync();

            if (levels == null || !levels.Any())
            {
                return NotFound("No levels found.");
            }

            return Ok(levels);
        }

        // GET: api/Level/5
        // Egy adott pálya lekérése ID alapján
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLevel(int id)
        {
            var level = await _context.Levels.FirstOrDefaultAsync(l => l.Id == id);

            if (level == null)
            {
                return NotFound($"Level with ID {id} not found.");
            }

            return Ok(level);
        }

        // POST: api/Level
        // Új pálya létrehozása
        [HttpPost]
        public async Task<IActionResult> CreateLevel([FromBody] Level level)
        {
            if (level == null)
            {
                return BadRequest("Level data is null.");
            }

            // Validáció (például nézd meg, hogy a név nem üres)
            if (string.IsNullOrWhiteSpace(level.Name))
            {
                return BadRequest("Level name is required.");
            }

            _context.Levels.Add(level);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLevel), new { id = level.Id }, level);
        }

        // PUT: api/Level/5
        // Adott pálya frissítése
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLevel(int id, [FromBody] Level level)
        {
            if (level == null || id != level.Id)
            {
                return BadRequest("Level data is invalid.");
            }

            var existingLevel = await _context.Levels.FirstOrDefaultAsync(l => l.Id == id);

            if (existingLevel == null)
            {
                return NotFound($"Level with ID {id} not found.");
            }

            existingLevel.Name = level.Name;

            _context.Levels.Update(existingLevel);
            await _context.SaveChangesAsync();

            return NoContent();  // Válasz, ami jelzi, hogy sikeres volt a frissítés
        }

        // DELETE: api/Level/5
        // Adott pálya törlése
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLevel(int id)
        {
            var level = await _context.Levels.FirstOrDefaultAsync(l => l.Id == id);

            if (level == null)
            {
                return NotFound($"Level with ID {id} not found.");
            }

            _context.Levels.Remove(level);
            await _context.SaveChangesAsync();

            return NoContent();  // Válasz, ami jelzi, hogy a törlés sikeres volt
        }
    }
}
