using System.ComponentModel.DataAnnotations;

namespace MudskipDB.Dto
{
    public class RegistrationDTO
    {
        [Required(ErrorMessage = "A felhasználónév kötelező.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "A teljes név kötelező.")]
        public string Fullname { get; set; }

        [Required(ErrorMessage = "Az email cím kötelező.")]
        [EmailAddress(ErrorMessage = "Érvénytelen email formátum.")]
        public string EmailAddress { get; set; }

        [Required(ErrorMessage = "A jelszó kötelező.")]
        public string PasswordHash { get; set; }
    }
}