using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudskipDB.Dto;
using MudskipDB.Models;
using System.Security.Cryptography;
using System.Text;

namespace SlimeDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 Regisztráció
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistrationDTO registerDto)
        {
            if (registerDto == null)
            {
                return BadRequest("User data is null.");
            }

            if (await _context.Users.AnyAsync(u => u.EmailAddress == registerDto.EmailAddress))
            {
                return BadRequest("Email already in use.");
            }

            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                return BadRequest("Username already taken.");
            }

            var hashedPassword = HashPassword(registerDto.PasswordHash);

            var newUser = new User
            {
                Username = registerDto.Username,
                Fullname = registerDto.Fullname,
                EmailAddress = registerDto.EmailAddress,
                PasswordHash = hashedPassword,
                CreatedAt = DateTime.UtcNow 
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully.");
        }

        // 🔹 Bejelentkezés (Session beállítás)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == loginDto.Username || u.EmailAddress == loginDto.Username);

            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid username/email or password.");
            }

            // 🔹 Felhasználói session beállítása
            HttpContext.Session.SetInt32("UserId", user.Id);

            return Ok(new { Message = "Login successful", UserId = user.Id });
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDTO updateDto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized("You must be logged in to update your profile.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            // Email cím módosítás
            if (!string.IsNullOrWhiteSpace(updateDto.NewEmail))
            {
                // Nézzük meg, hogy nem foglalt-e már ez az email
                if (await _context.Users.AnyAsync(u => u.EmailAddress == updateDto.NewEmail && u.Id != user.Id))
                    return BadRequest("This email address is already taken.");

                user.EmailAddress = updateDto.NewEmail;
            }

            // Jelszó módosítás
            if (!string.IsNullOrWhiteSpace(updateDto.NewPassword))
            {
                user.PasswordHash = HashPassword(updateDto.NewPassword); // Hashed jelszó mentés (pl. bcrypt)
            }

            await _context.SaveChangesAsync();
            return Ok("User email and/or password updated successfully.");
        }


        // 🔹 Kijelentkezés (Session törlés)
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok("User logged out successfully.");
        }

        // 🔹 Segédfüggvények a jelszó kezeléséhez
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            var enteredHash = HashPassword(enteredPassword);
            return enteredHash == storedHash;
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // 🔐 Ellenőrzés, hogy admin-e a bejelentkezett user
            var adminId = HttpContext.Session.GetInt32("UserId");
            var adminUser = await _context.Users.FindAsync(adminId);

            if (adminUser == null || adminUser.Role != "Admin")
                return Unauthorized("Only admin can delete users.");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("User not found.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok("User deleted successfully.");
        }
    }
}
