using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace MudskipDB.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }
        public string Fullname { get; set; }

        [Required, EmailAddress]
        public string EmailAddress { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Role { get; set; } = "User"; // vagy "Admin"
    }
}