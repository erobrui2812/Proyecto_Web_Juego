using hundir_la_flota.Models;
using Microsoft.EntityFrameworkCore;

public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<GameParticipant> GameParticipants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameParticipant>()
            .HasOne(gp => gp.Game)
            .WithMany(g => g.Participants)
            .HasForeignKey(gp => gp.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GameParticipant>()
            .HasOne(gp => gp.User)
            .WithMany()
            .HasForeignKey(gp => gp.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GameParticipant>()
            .HasIndex(gp => new { gp.GameId, gp.UserId })
            .IsUnique();


        modelBuilder.Entity<User>().HasData(new User
        {
            Id = -1,
            Nickname = "Bot",
            Email = "bot@hundirlaflota.com",
            PasswordHash = "hashdummy",
            AvatarUrl = null,
            CreatedAt = DateTime.UtcNow
        });

        modelBuilder.Entity<Game>()
            .HasKey(g => g.GameId);

        modelBuilder.Entity<Game>()
            .OwnsOne(g => g.Player1Board, board =>
            {
                board.Ignore(b => b.Grid);
                board.Ignore(b => b.GridForSerialization);
                board.OwnsMany(b => b.Ships, ship =>
                {
                    ship.ToTable("Player1_Ships");
                    ship.OwnsMany(s => s.Coordinates, coord =>
                    {
                        coord.HasKey(c => new { c.X, c.Y });
                    });
                });
            });

        modelBuilder.Entity<Game>()
            .OwnsOne(g => g.Player2Board, board =>
            {
                board.Ignore(b => b.Grid);
                board.Ignore(b => b.GridForSerialization); 
                board.OwnsMany(b => b.Ships, ship =>
                {
                    ship.ToTable("Player2_Ships");
                    ship.OwnsMany(s => s.Coordinates, coord =>
                    {
                        coord.HasKey(c => new { c.X, c.Y });
                    });
                });
            });

        modelBuilder.Entity<Game>()
            .OwnsMany(g => g.Actions, action =>
            {
                action.WithOwner().HasForeignKey("GameId");
                action.HasKey(a => new { a.Timestamp, a.PlayerId });
            });
    }
}
