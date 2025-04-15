using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudskipDB.Dto;
using MudskipDB.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace MudskipDB.Controllers
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
            // 📌 Modell érvényesítés (pl. email formátum)
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 📌 Üres kérés ellenőrzése
            if (registerDto == null)
            {
                return BadRequest("A felhasználói adat hiányzik.");
            }

            // 📌 Email cím egyediségének ellenőrzése
            if (await _context.Users.AnyAsync(u => u.EmailAddress == registerDto.EmailAddress))
            {
                return BadRequest("Ez az email cím már használatban van.");
            }

            // 📌 Felhasználónév egyediségének ellenőrzése
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                return BadRequest("Ez a felhasználónév már foglalt.");
            }

            // 📌 Jelszó hashelése
            var hashedPassword = HashPassword(registerDto.PasswordHash);

            // 📌 Új felhasználó létrehozása
            var newUser = new User
            {
                Username = registerDto.Username,
                Fullname = registerDto.Fullname,
                EmailAddress = registerDto.EmailAddress,
                PasswordHash = hashedPassword,
                CreatedAt = DateTime.UtcNow
            };

            // 📌 Felhasználó mentése adatbázisba
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok("A regisztráció sikeres volt.");
        }

        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { message = "Nem vagy bejelentkezve." });

            return Ok(new { Username = username });
        }

        // 🔹 Bejelentkezés (Session beállítása)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            // 📌 Felhasználó keresése felhasználónév vagy email alapján
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == loginDto.Username || u.EmailAddress == loginDto.Username);

            // 📌 Hitelesítési adatok ellenőrzése
            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized("Hibás felhasználónév vagy jelszó.");
            }

            // 📌 Session beállítása a bejelentkezett felhasználónak
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);

            return Ok(new { Message = "Sikeres bejelentkezés", UserId = user.Id });
        }

        // 🔹 Profil frissítése
        [HttpPut("update")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDTO updateDto)
        {
            // 📌 Modell validáció (email formátum, ha megadva)
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 📌 Sessionből user azonosító lekérése
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized("A módosításhoz be kell jelentkezni.");
            }

            // 📌 Felhasználó lekérése adatbázisból
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("A felhasználó nem található.");

            // ✉️ Email cím módosítása (ha be van jelölve)
            if (updateDto.ModifyEmail)
            {
                if (string.IsNullOrWhiteSpace(updateDto.NewEmail))
                    return BadRequest("Az új email cím megadása kötelező.");

                if (!new EmailAddressAttribute().IsValid(updateDto.NewEmail))
                    return BadRequest("Az email cím formátuma érvénytelen.");

                bool emailTaken = await _context.Users
                    .AnyAsync(u => u.EmailAddress == updateDto.NewEmail && u.Id != user.Id);

                if (emailTaken)
                    return BadRequest("Ez az email cím már foglalt.");

                user.EmailAddress = updateDto.NewEmail;
            }

            // 🔒 Jelszó módosítása (ha be van jelölve)
            if (updateDto.ModifyPassword)
            {
                if (string.IsNullOrWhiteSpace(updateDto.NewPassword))
                    return BadRequest("Az új jelszó megadása kötelező.");

                user.PasswordHash = HashPassword(updateDto.NewPassword);
            }

            // 💾 Módosítások mentése
            await _context.SaveChangesAsync();
            return Ok("A profil sikeresen frissítve lett.");
        }

        // 🔹 Kijelentkezés (Session törlése)
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok("Sikeres kijelentkezés.");
        }

        // 🔹 Segédfüggvény a jelszó hasheléséhez
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        // 🔹 Segédfüggvény a jelszó ellenőrzéséhez
        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            var enteredHash = HashPassword(enteredPassword);
            return enteredHash == storedHash;
        }

        // 🔹 Felhasználó törlése (csak adminnak engedélyezett)
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // 🔐 Ellenőrzés, hogy admin-e a bejelentkezett user
            var adminId = HttpContext.Session.GetInt32("UserId");
            var adminUser = await _context.Users.FindAsync(adminId);

            if (adminUser == null || adminUser.Role != "Admin")
                return Unauthorized("Csak admin törölhet felhasználót.");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("A felhasználó nem található.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok("A felhasználó sikeresen törölve lett.");
        }
    }
}