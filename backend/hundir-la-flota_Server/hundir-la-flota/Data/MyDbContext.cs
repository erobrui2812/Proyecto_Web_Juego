﻿using hundir_la_flota.Models;
using Microsoft.EntityFrameworkCore;

public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<Game> Games { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Friend)
            .WithMany()
            .HasForeignKey(f => f.FriendId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Friendship>()
            .HasIndex(f => new { f.UserId, f.FriendId })
            .IsUnique();


        modelBuilder.Entity<Game>()
            .HasKey(g => g.GameId);

        modelBuilder.Entity<Game>()
            .OwnsOne(g => g.Player1Board, board =>
            {
                board.Ignore(b => b.Grid);
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
