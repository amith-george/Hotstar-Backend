using HotstarApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotstarApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // ─── Domain 1: Identity & Subscriptions ────────────────────────────────
    public DbSet<User>         Users         { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Profile>      Profiles      { get; set; }

    // ─── Domain 2: Core Content & Catalog (DbSets declared now; configured next iteration) ──
    // public DbSet<Content>      Contents      { get; set; }
    // public DbSet<Genre>        Genres        { get; set; }
    // public DbSet<Video>        Videos        { get; set; }

    // ─── Domain 3: User Engagement ─────────────────────────────────────────
    // public DbSet<WatchHistory> WatchHistories { get; set; }
    // public DbSet<Watchlist>    Watchlists     { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Subscription ──────────────────────────────────────────────────
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(50);
            entity.Property(s => s.MaxResolution).IsRequired().HasMaxLength(10);
            entity.Property(s => s.MonthlyPrice).HasColumnType("decimal(10,2)");

            // Seed the three subscription tiers so new users can be assigned Free immediately.
            entity.HasData(
                new Subscription { Id = 1, Name = "Free",    MonthlyPrice = 0m,    MaxResolution = "480p",  HasAds = true  },
                new Subscription { Id = 2, Name = "Basic",   MonthlyPrice = 99m,   MaxResolution = "720p",  HasAds = true  },
                new Subscription { Id = 3, Name = "Premium", MonthlyPrice = 299m,  MaxResolution = "1080p", HasAds = false }
            );
        });

        // ── User ──────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);

            // User → Subscription (many-to-one, nullable FK)
            entity.HasOne(u => u.Subscription)
                  .WithMany(s => s.Users)
                  .HasForeignKey(u => u.SubscriptionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Profile ───────────────────────────────────────────────────────
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.AvatarUrl).HasMaxLength(500);

            // Profile → User (many-to-one, cascade delete)
            entity.HasOne(p => p.User)
                  .WithMany(u => u.Profiles)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Domain 2 — Genre ↔ Content many-to-many (configured in next iteration) ──
        // modelBuilder.Entity<Content>()
        //     .HasMany(c => c.Genres)
        //     .WithMany(g => g.Contents)
        //     .UsingEntity(j => j.ToTable("ContentGenres"));
    }
}
