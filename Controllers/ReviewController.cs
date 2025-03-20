using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudskipDB.Dto;
using MudskipDB.Models;

namespace SlimeDB.Controllers
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

        [HttpPost]
        public async Task<IActionResult> PostReview([FromBody] ReviewDTO reviewDto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized("You must be logged in to post a review.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized("User not found.");

            // Ellenőrzés: van-e már review-ja
            bool alreadyReviewed = await _context.Reviews.AnyAsync(r => r.UserId == user.Id);
            if (alreadyReviewed) return BadRequest("You have already submitted a review.");

            // Az Id-t EF Core generálja, CreatedAt-ot itt állítjuk be
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
                Message = "Review posted successfully!",
                ReviewId = newReview.Id,
                Username = user.Username,
                CreatedAt = newReview.CreatedAt
            });
        }


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
                    CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd") // Csak napra pontos dátum
                })
                .ToListAsync();

            return Ok(reviews);
        }



        // 🔹 Review törlése CSAK adminnak
        [HttpDelete("{reviewId}")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized("You must be logged in as admin to delete a review.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Role != "Admin") return Forbid("Only admins can delete reviews.");

            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return NotFound("Review not found.");

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok("Review deleted successfully.");
        }
    }
}
