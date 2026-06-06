using System.ComponentModel.DataAnnotations;
using HotstarApi.Models;

namespace HotstarApi.Dtos.Search;

// ─── Search Query ─────────────────────────────────────────────────────────────

public class SearchQueryDto
{
    /// <summary>Partial match searched across Title, Description, and Genre names.</summary>
    [Required]
    [MinLength(1, ErrorMessage = "Keyword cannot be empty.")]
    [MaxLength(200)]
    public string Keyword { get; set; } = string.Empty;

    /// <summary>Optional: filter to a specific genre by Id.</summary>
    public int? GenreId { get; set; }

    /// <summary>Optional: "Movie" or "TVShow".</summary>
    [RegularExpression("^(Movie|TVShow)$",
        ErrorMessage = "ContentType must be 'Movie' or 'TVShow'.")]
    public string? ContentType { get; set; }

    // Pagination
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int Size { get; set; } = 20;
}
