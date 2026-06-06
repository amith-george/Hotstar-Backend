using System.ComponentModel.DataAnnotations;
using HotstarApi.Models;
using Microsoft.AspNetCore.Http;

namespace HotstarApi.Dtos.Contents;

// ─── Read ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Flattened read shape: genres are a simple list of strings — no nested objects.
/// </summary>
public class ContentDto
{
    public int         Id          { get; set; }
    public string      Title       { get; set; } = string.Empty;
    public string?     Description { get; set; }
    public string?     PosterUrl   { get; set; }
    public string?     BannerUrl   { get; set; }
    public string      ContentType { get; set; } = string.Empty;   // "Movie" | "TVShow"
    public int         ReleaseYear { get; set; }
    public bool        IsPremium   { get; set; }
    public List<string> Genres     { get; set; } = new();

    // Dynamically aggregated metrics
    public double AverageRating    { get; set; }
    public int    TotalReviewCount { get; set; }
}

// ─── Create ───────────────────────────────────────────────────────────────────

public class ContentCreateDto
{
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Poster image file (JPEG / PNG / WebP).</summary>
    public IFormFile? PosterImage { get; set; }

    /// <summary>Banner/hero image file (JPEG / PNG / WebP).</summary>
    public IFormFile? BannerImage { get; set; }

    [Required]
    public ContentType ContentType { get; set; }

    [Required]
    [Range(1888, 2100)]
    public int ReleaseYear { get; set; }

    public bool IsPremium { get; set; }

    /// <summary>Comma-separated Genre IDs to associate with this title.</summary>
    public string? GenreIds { get; set; }
}

// ─── Update ───────────────────────────────────────────────────────────────────

public class ContentUpdateDto
{
    [MaxLength(300)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Replaces the existing poster. Leave null to keep the current image.</summary>
    public IFormFile? PosterImage { get; set; }

    /// <summary>Replaces the existing banner. Leave null to keep the current image.</summary>
    public IFormFile? BannerImage { get; set; }

    public ContentType? ContentType { get; set; }

    [Range(1888, 2100)]
    public int? ReleaseYear { get; set; }

    public bool? IsPremium { get; set; }

    /// <summary>
    /// Fully replaces the genre associations when provided.
    /// Pass an empty string to clear all genres.
    /// </summary>
    public string? GenreIds { get; set; }
}
