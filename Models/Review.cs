namespace MudskipDB.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = null;
        public DateTime CreatedAt { get; set; } = DateTime.Now;


        
        public User User { get; set; }


        
    }
}
