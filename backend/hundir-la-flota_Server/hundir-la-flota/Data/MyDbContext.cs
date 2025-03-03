using hundir_la_flota.Models;
using Microsoft.EntityFrameworkCore;


public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<GameParticipant> GameParticipants { get; set; }
    public DbSet<PlayerStats> PlayerStats { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseMySql(
                new MySqlServerVersion(new Version(8, 0, 27)),
                builder => builder.MigrationsAssembly("hundir-la-flota")
            );
        }
    }
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

        modelBuilder.Entity<PlayerStats>()
            .HasKey(ps => ps.UserId);


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
                    ship.HasKey(s => s.Id);
                    ship.Property(s => s.Id).ValueGeneratedOnAdd();
                    ship.OwnsMany(s => s.Coordinates, coord =>
                    {
                        coord.ToTable("Player1_Ships_Coordinates");
                        coord.WithOwner().HasForeignKey("ShipId");
                        coord.HasKey("ShipId", "X", "Y");
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
                    ship.HasKey(s => s.Id);
                    ship.Property(s => s.Id).ValueGeneratedOnAdd();
                    ship.OwnsMany(s => s.Coordinates, coord =>
                    {
                        coord.ToTable("Player2_Ships_Coordinates");
                        coord.WithOwner().HasForeignKey("ShipId");
                        coord.HasKey("ShipId", "X", "Y");
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
