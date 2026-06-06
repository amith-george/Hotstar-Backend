namespace HotstarApi.Models;

public class WatchHistory
{
    public int Id { get; set; }

    /// <summary>Playback position in seconds when the user stopped watching.</summary>
    public int StoppedAtTimestamp { get; set; }

    public DateTime LastWatchedAt { get; set; } = DateTime.UtcNow;

    // FK → Profile (cascade delete)
    public int     ProfileId { get; set; }
    public Profile Profile   { get; set; } = null!;

    // FK → Video (restrict delete — we want to keep history even if video is removed from catalog)
    public int   VideoId { get; set; }
    public Video Video   { get; set; } = null!;
}
