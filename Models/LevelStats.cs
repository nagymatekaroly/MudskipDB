namespace MudskipDB.Models
{
    public class LevelStats
    {
        public int Id { get; set; }

        public int LevelId { get; set; }
        public Level Level { get; set; }

        public int CompletionCount { get; set; } = 0;
    }
}
