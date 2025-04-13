using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudskipDB.Dto;


namespace MudskipDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 Új értékelés beküldése
        [HttpPost]
        public async Task<IActionResult> PostReview([FromBody] ReviewDTO reviewDto)
        {
            // 📌 Felhasználó azonosítása session alapján
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized("Értékelés küldéséhez be kell jelentkezni.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized("A felhasználó nem található.");

            // 📌 Ellenőrzés: van-e már értékelése a felhasználónak
            bool alreadyReviewed = await _context.Reviews.AnyAsync(r => r.UserId == user.Id);
            if (alreadyReviewed) return BadRequest("Már küldtél be értékelést.");

            // 📌 Az Id-t az EF Core generálja, a CreatedAt-ot itt állítjuk be
            var newReview = new Review
            {
                UserId = user.Id,
                Comment = reviewDto.Comment,
                Rating = reviewDto.Rating,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(newReview);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Az értékelés sikeresen el lett mentve!",
                ReviewId = newReview.Id,
                Username = user.Username,
                CreatedAt = newReview.CreatedAt
            });
        }

        // 🔹 Összes értékelés lekérése
        [HttpGet("all")]
        public async Task<IActionResult> GetAllReviews()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    Username = r.User.Username,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd") // 📌 Csak napra pontos dátum
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // 🔹 Értékelés törlése – CSAK admin számára elérhető
        [HttpDelete("{reviewId}")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            // 📌 Bejelentkezett felhasználó ellenőrzése (admin jog)
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized("Csak bejelentkezett admin törölhet értékelést.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Role != "Admin") return Forbid("Csak admin törölhet értékelést.");

            // 📌 Törlendő értékelés keresése
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return NotFound("Az értékelés nem található.");

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok("Az értékelés sikeresen törölve lett.");
        }
    }
}