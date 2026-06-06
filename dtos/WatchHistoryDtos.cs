using System.ComponentModel.DataAnnotations;

namespace HotstarApi.Dtos.WatchHistories;

// ─── Write: the payload pulsed every ~10 seconds from the player ─────────────

public class WatchHistoryLogDto
{
    [Required]
    public int ProfileId { get; set; }

    [Required]
    public int VideoId { get; set; }

    /// <summary>Playback position in seconds at the time of the pulse.</summary>
    [Required]
    [Range(0, int.MaxValue)]
    public int StoppedAtTimestamp { get; set; }
}

// ─── Read: "Continue Watching" carousel item ─────────────────────────────────

public class WatchHistoryDto
{
    public int      Id                  { get; set; }
    public int      VideoId             { get; set; }
    public string   VideoTitle          { get; set; } = string.Empty;
    public int      StoppedAtTimestamp  { get; set; }
    public DateTime LastWatchedAt       { get; set; }

    // Parent Content metadata so the frontend can render the carousel card
    public int      ContentId           { get; set; }
    public string   ContentTitle        { get; set; } = string.Empty;
    public string?  PosterUrl           { get; set; }
    public bool     IsPremium           { get; set; }
    public string   ContentType         { get; set; } = string.Empty;   // "Movie" | "TVShow"

    // For TV show episodes — useful for displaying "S2 E5" label
    public int?     SeasonNumber        { get; set; }
    public int?     EpisodeNumber       { get; set; }

    // Total duration lets the frontend render the progress bar
    public int      DurationInSeconds   { get; set; }
}
