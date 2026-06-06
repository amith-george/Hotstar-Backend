using System.ComponentModel.DataAnnotations;

namespace HotstarApi.Dtos.Watchlists;

// ─── Write: add or remove a title from a profile's watchlist ─────────────────

public class WatchlistToggleDto
{
    [Required]
    public int ProfileId { get; set; }

    [Required]
    public int ContentId { get; set; }
}

// ─── Read: one saved-title card in the "My List" grid ────────────────────────

public class WatchlistDto
{
    public int      Id        { get; set; }
    public DateTime AddedAt   { get; set; }

    // Flattened Content metadata — everything the frontend needs to render a grid card
    public int      ContentId    { get; set; }
    public string   Title        { get; set; } = string.Empty;
    public string?  PosterUrl    { get; set; }
    public bool     IsPremium    { get; set; }
    public string   ContentType  { get; set; } = string.Empty;  // "Movie" | "TVShow"
    public int      ReleaseYear  { get; set; }
}
