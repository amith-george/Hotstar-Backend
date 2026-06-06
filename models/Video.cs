namespace HotstarApi.Models;

public class Video
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Relative path: /uploads/videos/&lt;filename&gt;</summary>
    public string VideoUrl { get; set; } = string.Empty;

    public int DurationInSeconds { get; set; }

    /// <summary>Null for Movies; set for TV show episodes.</summary>
    public int? SeasonNumber { get; set; }

    /// <summary>Null for Movies; set for TV show episodes.</summary>
    public int? EpisodeNumber { get; set; }

    // FK
    public int ContentId { get; set; }
    public Content Content { get; set; } = null!;

    // Navigation
    public ICollection<WatchHistory> WatchHistories { get; set; } = new List<WatchHistory>();
}
