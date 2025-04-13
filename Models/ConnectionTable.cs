namespace MudskipDB.Models
{
    public class ConnectionTable
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int? HighscoreId { get; set; }
        public int? ReviewId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public User User { get; set; }
        public Highscore Highscore { get; set; }
        public Review Review { get; set; }
    }
}
