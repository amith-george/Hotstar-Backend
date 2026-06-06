namespace HotstarApi.Models;

public class Watchlist
{
    public int Id { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // FK → Profile (cascade delete)
    public int     ProfileId { get; set; }
    public Profile Profile   { get; set; } = null!;

    // FK → Content
    public int     ContentId { get; set; }
    public Content Content   { get; set; } = null!;
}
