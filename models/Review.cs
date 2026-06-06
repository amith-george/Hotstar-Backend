namespace HotstarApi.Models;

/// <summary>
/// A user's rating and review for a specific movie or show.
/// </summary>
public class Review
{
    public int Id { get; set; }

    /// <summary>1 to 5 stars.</summary>
    public int RatingValue { get; set; }

    /// <summary>Optional text commentary.</summary>
    public string? ReviewText { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // FK → Profile (Cascade delete)
    public int ProfileId { get; set; }
    public Profile Profile { get; set; } = null!;

    // FK → Content (Cascade delete)
    public int ContentId { get; set; }
    public Content Content { get; set; } = null!;
}
