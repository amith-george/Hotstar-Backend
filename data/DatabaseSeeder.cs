using HotstarApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotstarApi.Data;

public static class DatabaseSeeder
{
    public static void Seed(ApplicationDbContext db)
    {
        // ── 0. Force Upgrade User to Premium (Quick Fix) ──────────────────────
        var existingUser = db.Users.FirstOrDefault(u => u.Email == "amithgeorge130@gmail.com");
        if (existingUser != null && existingUser.SubscriptionId != 3)
        {
            existingUser.SubscriptionId = 3;
            db.SaveChanges();
        }

        // Only seed if the database is essentially empty (e.g., no genres)
        if (db.Genres.Any()) return;

        // ── 1. Create Users & Profiles ──────────────────────────────────────────
        var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123");

        var user1 = new User
        {
            Email = "amithgeorge130@gmail.com",
            PasswordHash = defaultPasswordHash,
            Role = "User",
            SubscriptionId = 3, // Premium
            CreatedAt = DateTime.UtcNow
        };

        var admin = new User
        {
            Email = "amithgeorgescms@gmail.com",
            PasswordHash = defaultPasswordHash,
            Role = "Admin",
            SubscriptionId = 3, // Premium
            CreatedAt = DateTime.UtcNow
        };

        // Note: The Admin seeded in OnModelCreating has Id=1. So we check if our users exist.
        if (!db.Users.Any(u => u.Email == "amithgeorge130@gmail.com"))
        {
            db.Users.Add(user1);
            db.Users.Add(admin);
            db.SaveChanges(); // Save to get the User Ids

            db.Profiles.AddRange(
                new Profile { UserId = user1.Id, Name = "Amith", IsKidsProfile = false, AvatarUrl = "https://ui-avatars.com/api/?name=Amith&background=0D8ABC&color=fff&size=128" },
                new Profile { UserId = admin.Id, Name = "Admin", IsKidsProfile = false, AvatarUrl = "https://ui-avatars.com/api/?name=Admin&background=B91C1C&color=fff&size=128" }
            );
            db.SaveChanges();
        }

        // ── 2. Create Genres ────────────────────────────────────────────────────
        var action   = new Genre { Name = "Action" };
        var drama    = new Genre { Name = "Drama" };
        var sciFi    = new Genre { Name = "Sci-Fi" };
        var thriller = new Genre { Name = "Thriller" };
        var comedy   = new Genre { Name = "Comedy" };
        var sports   = new Genre { Name = "Sports" };

        db.Genres.AddRange(action, drama, sciFi, thriller, comedy, sports);
        db.SaveChanges(); // Get Genre Ids

        // ── 3. Create Content (Movies & TV Shows) ───────────────────────────────
        
        var moviesAndShows = new List<Content>
        {
            new Content
            {
                Title = "Avengers: Endgame",
                Description = "After the devastating events of Infinity War, the universe is in ruins. With the help of remaining allies, the Avengers assemble once more in order to reverse Thanos' actions.",
                PosterUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/w500/or06FN3Dka5tukK1e9sl16pB3iy.jpg",
                BannerUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/original/7RyHsO4yDXtBv1zUU3mTpHeQ0d5.jpg",
                ContentType = ContentType.Movie,
                ReleaseYear = 2019,
                IsPremium = true,
                Genres = new List<Genre> { action, sciFi }
            },
            new Content
            {
                Title = "The Mandalorian",
                Description = "After the fall of the Galactic Empire, lawlessness has spread throughout the galaxy. A lone gunfighter makes his way through the outer reaches, earning his keep as a bounty hunter.",
                PosterUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/w500/eU1i6eHXlzMOlEq0ku1Rzq7Y4wA.jpg",
                BannerUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/original/8rj1P2PWEKzNlsK9SjHn0i5JbI2.jpg",
                ContentType = ContentType.TVShow,
                ReleaseYear = 2019,
                IsPremium = true,
                Genres = new List<Genre> { action, sciFi }
            },
            new Content
            {
                Title = "Inception",
                Description = "Cobb, a skilled thief who commits corporate espionage by infiltrating the subconscious of his targets is offered a chance to regain his old life as payment for a task considered to be impossible.",
                PosterUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/w500/9gk7adHYeDvHkCSEqAvQNLV5Uge.jpg",
                BannerUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/original/8ZTVqvKDQ8emSGUEMjsS4yHAwrp.jpg",
                ContentType = ContentType.Movie,
                ReleaseYear = 2010,
                IsPremium = false,
                Genres = new List<Genre> { action, sciFi, thriller }
            },
            new Content
            {
                Title = "Breaking Bad",
                Description = "When Walter White, a New Mexico chemistry teacher, is diagnosed with Stage III cancer and given a prognosis of only two years left to live. He becomes filled with a sense of fearlessness and an unrelenting desire to secure his family's financial future.",
                PosterUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/w500/ggFHVNu6YYI5L9pCfOacjizwpB.jpg",
                BannerUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/original/tsRy63Mu5cu8etL1X7ZLyf7UP1M.jpg",
                ContentType = ContentType.TVShow,
                ReleaseYear = 2008,
                IsPremium = true,
                Genres = new List<Genre> { drama, thriller }
            },
            new Content
            {
                Title = "The Dark Knight",
                Description = "Batman raises the stakes in his war on crime. With the help of Lt. Jim Gordon and District Attorney Harvey Dent, Batman sets out to dismantle the remaining criminal organizations that plague the streets.",
                PosterUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/w500/qJ2tW6WMUDux911r6m7haRef0WH.jpg",
                BannerUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/original/dqK9Hag1054tghRQSqLSfrkvQnA.jpg",
                ContentType = ContentType.Movie,
                ReleaseYear = 2008,
                IsPremium = true,
                Genres = new List<Genre> { action, drama, thriller }
            },
            new Content
            {
                Title = "Stranger Things",
                Description = "When a young boy vanishes, a small town uncovers a mystery involving secret experiments, terrifying supernatural forces, and one strange little girl.",
                PosterUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/w500/49WJfeN0moxb9IPfGn8NNq1D0L.jpg",
                BannerUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/original/56v2KjBlU4XaM9tzLNdoD4q7nMV.jpg",
                ContentType = ContentType.TVShow,
                ReleaseYear = 2016,
                IsPremium = false,
                Genres = new List<Genre> { sciFi, drama }
            },
            new Content
            {
                Title = "Interstellar",
                Description = "The adventures of a group of explorers who make use of a newly discovered wormhole to surpass the limitations on human space travel and conquer the vast distances involved in an interstellar voyage.",
                PosterUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/w500/gEU2QlsUUcsOls4c4E9eZ21M2XQ.jpg",
                BannerUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/original/rAiYTfKGqDCRIIqo664sY9XZIvQ.jpg",
                ContentType = ContentType.Movie,
                ReleaseYear = 2014,
                IsPremium = true,
                Genres = new List<Genre> { sciFi, drama }
            },
            new Content
            {
                Title = "The Office",
                Description = "The everyday lives of office employees in the Scranton, Pennsylvania branch of the fictional Dunder Mifflin Paper Company.",
                PosterUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/w500/qWnJzyZhyy74gjpSjIXWmuk0ifX.jpg",
                BannerUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/original/fMuwKIB9D29aIeW30Y5hN0uR0tW.jpg",
                ContentType = ContentType.TVShow,
                ReleaseYear = 2005,
                IsPremium = false,
                Genres = new List<Genre> { comedy }
            },
            new Content
            {
                Title = "ICC Men's T20 World Cup Final",
                Description = "Watch the thrilling finale of the ICC Men's T20 World Cup, where the world's best cricketing nations battle for ultimate glory.",
                PosterUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/w500/3oN8XhN2g0OZbV7n3hW4K9pT5kY.jpg", 
                BannerUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/original/jK8pM1X5Y7mR4bW2H3uF4pL9cE7.jpg",
                ContentType = ContentType.TVShow,
                ReleaseYear = 2024,
                IsPremium = true,
                Genres = new List<Genre> { sports }
            },
            new Content
            {
                Title = "Formula 1: Drive to Survive",
                Description = "Drivers, managers and team owners live life in the fast lane — both on and off the track — during one cutthroat season of Formula 1 racing.",
                PosterUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/w500/38wB8yD238rD51o0L4I7j8Rz2yQ.jpg",
                BannerUrl = "https://wsrv.nl/?url=image.tmdb.org/t/p/original/x8Y3T6J8B9v2d5L1N4oK7pQ9yV6.jpg",
                ContentType = ContentType.TVShow,
                ReleaseYear = 2019,
                IsPremium = true,
                Genres = new List<Genre> { sports, drama }
            }
        };

        db.Contents.AddRange(moviesAndShows);
        db.SaveChanges(); // Get Content Ids

        // ── 4. Create Videos (YouTube Trailer Embeds) ───────────────────────────
        
        // Dictionary mapping Titles to YouTube IDs
        var trailerMap = new Dictionary<string, string>
        {
            { "Avengers: Endgame", "TcMBFSGVi1c" },
            { "The Mandalorian", "aOC8E8z_ifw" },
            { "Inception", "YoHD9XEInc0" },
            { "Breaking Bad", "HhesaQXLuRY" },
            { "The Dark Knight", "EXeTwQWrcwY" },
            { "Stranger Things", "b9EkMc79ZSU" },
            { "Interstellar", "zSWdZVtXT7E" },
            { "The Office", "gO8N3L_aERg" }
        };

        foreach (var content in moviesAndShows)
        {
            var ytId = trailerMap.GetValueOrDefault(content.Title, "dQw4w9WgXcQ"); // Fallback to Rickroll :)
            
            db.Videos.Add(new Video
            {
                ContentId = content.Id,
                Title = $"{content.Title} - Official Trailer",
                VideoUrl = $"https://www.youtube.com/embed/{ytId}",
                SeasonNumber = content.ContentType == ContentType.TVShow ? 1 : null,
                EpisodeNumber = content.ContentType == ContentType.TVShow ? 1 : null,
                DurationInSeconds = 120
            });

            // If it's a TV show, let's add a few more mock episodes
            if (content.ContentType == ContentType.TVShow)
            {
                for (int ep = 2; ep <= 5; ep++)
                {
                    db.Videos.Add(new Video
                    {
                        ContentId = content.Id,
                        Title = $"{content.Title} - Episode {ep}",
                        VideoUrl = $"https://www.youtube.com/embed/{ytId}", // Reusing trailer link for mock
                        SeasonNumber = 1,
                        EpisodeNumber = ep,
                        DurationInSeconds = 2700
                    });
                }
            }
        }

        db.SaveChanges();
    }
}
