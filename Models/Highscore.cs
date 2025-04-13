namespace MudskipDB.Models
{
    public class Highscore
    {
        public int Id { get; set; }

        // Felhasználó, aki a pontot szerezte
        public int UserId { get; set; }

        // A pálya, amelyen a pontot szerezte
        public int LevelId { get; set; }

        // A felhasználó által elért pontszám
        public int HighscoreValue { get; set; }

        // Kapcsolat a User entitással
        public User User { get; set; }

        // Kapcsolat a Level entitással
        public Level Level { get; set; }
    }
}
