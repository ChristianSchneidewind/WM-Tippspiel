using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Models;

namespace TippSpiel.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Game> Games { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<MatchEvent> MatchEvents { get; set; }
        public DbSet<Tipp> Tipps { get; set; }

        // Added missing Groups DbSet so HomeController._db.Groups compiles
        public DbSet<Group> Groups { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Spezial-Konfiguration für MatchEvent
            // Wir sagen EF, dass beim Löschen eines Spielers nicht automatisch 
            // alle seine Tore/Assists gelöscht werden sollen (Restrict), 
            // um den Zirkelbezug zu vermeiden.

            modelBuilder.Entity<MatchEvent>()
                .HasOne(m => m.Player)
                .WithMany()
                .HasForeignKey(m => m.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MatchEvent>()
                .HasOne(m => m.AssistPlayer)
                .WithMany()
                .HasForeignKey(m => m.AssistPlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Falls dein Game-Model jetzt HomeTeam und AwayTeam als Objekte hat:
            modelBuilder.Entity<Game>()
                .HasOne(g => g.HomeTeam)
                .WithMany()
                .HasForeignKey(g => g.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Game>()
                .HasOne(g => g.AwayTeam)
                .WithMany()
                .HasForeignKey(g => g.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}