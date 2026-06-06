namespace HotstarApi.Models;

public enum ContentType
{
    Movie,
    TVShow
}

public class Content
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Relative path: /uploads/posters/&lt;filename&gt;</summary>
    public string? PosterUrl { get; set; }

    /// <summary>Relative path: /uploads/banners/&lt;filename&gt;</summary>
    public string? BannerUrl { get; set; }

    public ContentType ContentType { get; set; }

    public int ReleaseYear { get; set; }

    public bool IsPremium { get; set; }

    // Navigation — many-to-many
    public ICollection<Genre> Genres { get; set; } = new List<Genre>();

    // Navigation — one-to-many Videos
    public ICollection<Video> Videos { get; set; } = new List<Video>();

    // Navigation — one-to-many Reviews
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    // Navigation — one-to-many Watchlists
    public ICollection<Watchlist> Watchlists { get; set; } = new List<Watchlist>();
}
