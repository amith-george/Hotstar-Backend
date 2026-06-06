using HotstarApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotstarApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // ─── Domain 1: Identity & Subscriptions ────────────────────────────────────
    public DbSet<User>         Users         { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Profile>      Profiles      { get; set; }

    // ─── Domain 2: Core Content & Catalog ──────────────────────────────────────
    public DbSet<Content> Contents { get; set; }
    public DbSet<Genre>   Genres   { get; set; }
    public DbSet<Video>   Videos   { get; set; }

    // ─── Domain 3: User Engagement ─────────────────────────────────────────────
    public DbSet<WatchHistory> WatchHistories { get; set; }
    public DbSet<Watchlist>    Watchlists     { get; set; }

    // ─── Domain 4: Payments ─────────────────────────────────────────────────────
    public DbSet<Transaction> Transactions { get; set; }

    // ─── Domain 5: Search, User Management, & OTP Integration ──────────────────
    public DbSet<OtpToken>     OtpTokens     { get; set; }
    public DbSet<UserSession>  UserSessions  { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ════════════════════════════════════════════════════════════════════
        // DOMAIN 1
        // ════════════════════════════════════════════════════════════════════

        // ── Subscription ──────────────────────────────────────────────────────
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(50);
            entity.Property(s => s.MaxResolution).IsRequired().HasMaxLength(10);
            entity.Property(s => s.MonthlyPrice).HasColumnType("decimal(10,2)");

            // Seed the three subscription tiers
            entity.HasData(
                new Subscription { Id = 1, Name = "Free",    MonthlyPrice = 0m,   MaxResolution = "480p",  HasAds = true  },
                new Subscription { Id = 2, Name = "Basic",   MonthlyPrice = 99m,  MaxResolution = "720p",  HasAds = true  },
                new Subscription { Id = 3, Name = "Premium", MonthlyPrice = 299m, MaxResolution = "1080p", HasAds = false }
            );
        });

        // ── User ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);

            entity.HasOne(u => u.Subscription)
                  .WithMany(s => s.Users)
                  .HasForeignKey(u => u.SubscriptionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Profile ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.AvatarUrl).HasMaxLength(500);

            entity.HasOne(p => p.User)
                  .WithMany(u => u.Profiles)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ════════════════════════════════════════════════════════════════════
        // DOMAIN 2
        // ════════════════════════════════════════════════════════════════════

        // ── Content ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Content>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Title).IsRequired().HasMaxLength(300);
            entity.Property(c => c.Description).HasMaxLength(2000);
            entity.Property(c => c.PosterUrl).HasMaxLength(500);
            entity.Property(c => c.BannerUrl).HasMaxLength(500);
            entity.Property(c => c.ContentType)
                  .HasConversion<string>()    // stored as "Movie" / "TVShow"
                  .HasMaxLength(10);
        });

        // ── Genre ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(g => g.Name).IsUnique();
        });

        // ── Content ↔ Genre — explicit many-to-many join table ────────────────
        modelBuilder.Entity<Content>()
            .HasMany(c => c.Genres)
            .WithMany(g => g.Contents)
            .UsingEntity(j => j.ToTable("ContentGenres"));

        // ── Video ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Video>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Title).IsRequired().HasMaxLength(300);
            entity.Property(v => v.VideoUrl).IsRequired().HasMaxLength(500);

            // Cascade delete: removing a Content title removes all its episodes
            entity.HasOne(v => v.Content)
                  .WithMany(c => c.Videos)
                  .HasForeignKey(v => v.ContentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ════════════════════════════════════════════════════════════════════
        // DOMAIN 3
        // ════════════════════════════════════════════════════════════════════

        // ── WatchHistory ──────────────────────────────────────────────────────
        modelBuilder.Entity<WatchHistory>(entity =>
        {
            entity.HasKey(wh => wh.Id);

            // Profile → WatchHistory: cascade delete (profile gone → history gone)
            entity.HasOne(wh => wh.Profile)
                  .WithMany()
                  .HasForeignKey(wh => wh.ProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Video → WatchHistory: restrict delete so history is preserved even if
            // a video is removed from the catalog (soft business rule)
            entity.HasOne(wh => wh.Video)
                  .WithMany()
                  .HasForeignKey(wh => wh.VideoId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Watchlist ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Watchlist>(entity =>
        {
            entity.HasKey(wl => wl.Id);

            // Composite unique index: one user cannot save the same title twice
            entity.HasIndex(wl => new { wl.ProfileId, wl.ContentId }).IsUnique();

            // Profile → Watchlist: cascade delete
            entity.HasOne(wl => wl.Profile)
                  .WithMany()
                  .HasForeignKey(wl => wl.ProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Content → Watchlist: cascade delete (title removed → saved entries removed)
            entity.HasOne(wl => wl.Content)
                  .WithMany()
                  .HasForeignKey(wl => wl.ContentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ══════════════════════════════════════════════════════════════════
        // DOMAIN 4
        // ══════════════════════════════════════════════════════════════════

        // ── Transaction ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);

            // RazorpayOrderId must be globally unique (one order = one transaction)
            entity.HasIndex(t => t.RazorpayOrderId).IsUnique();

            entity.Property(t => t.RazorpayOrderId).IsRequired().HasMaxLength(100);
            entity.Property(t => t.RazorpayPaymentId).HasMaxLength(100);
            entity.Property(t => t.RazorpaySignature).HasMaxLength(512);
            entity.Property(t => t.Currency).IsRequired().HasMaxLength(3);
            entity.Property(t => t.Status).IsRequired().HasMaxLength(20);

            // User → Transaction: Restrict delete so financial records are NEVER orphaned
            entity.HasOne(t => t.User)
                  .WithMany()
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ══════════════════════════════════════════════════════════════════
        // DOMAIN 5
        // ══════════════════════════════════════════════════════════════════

        // ── OtpToken ────────────────────────────────────────────────────────────
        modelBuilder.Entity<OtpToken>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Email).IsRequired().HasMaxLength(256);
            entity.Property(o => o.OtpCode).IsRequired().HasMaxLength(6);
            entity.Property(o => o.Purpose).IsRequired().HasMaxLength(50);
        });

        // ── UserSession ─────────────────────────────────────────────────────────
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.DeviceIdentifier).IsRequired().HasMaxLength(256);
            entity.Property(s => s.IpAddress).HasMaxLength(50);

            // Cascade delete: when user is deleted, their active sessions are removed.
            entity.HasOne(s => s.User)
                  .WithMany()
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

