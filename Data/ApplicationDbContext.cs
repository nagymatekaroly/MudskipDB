using Microsoft.EntityFrameworkCore;
using MudskipDB.Models;

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Level> Levels { get; set; }
    public DbSet<Highscore> Highscores { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<LevelStats> LevelStats { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Highscore>()
            .HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Highscore>()
            .HasOne(h => h.Level)
            .WithMany()
            .HasForeignKey(h => h.LevelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
       
        modelBuilder.Entity<LevelStats>()
            .HasOne(ls => ls.Level)
            .WithMany()
            .HasForeignKey(ls => ls.LevelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
