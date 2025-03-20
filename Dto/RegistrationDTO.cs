namespace MudskipDB.Dto
{
    public class RegistrationDTO
    {
        public string Username { get; set; }
        public string Fullname { get; set; }
        public string EmailAddress { get; set; }
        public string PasswordHash { get; set; }
    }
}