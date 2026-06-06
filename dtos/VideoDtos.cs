using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HotstarApi.Dtos.Videos;

// ─── Read ─────────────────────────────────────────────────────────────────────

public class VideoDto
{
    public int     Id                { get; set; }
    public string  Title             { get; set; } = string.Empty;
    public string  VideoUrl          { get; set; } = string.Empty;
    public int     DurationInSeconds { get; set; }
    public int?    SeasonNumber      { get; set; }
    public int?    EpisodeNumber     { get; set; }
    public int     ContentId         { get; set; }

    // Human-readable duration (e.g. "1h 47m")
    public string DurationFormatted =>
        DurationInSeconds >= 3600
            ? $"{DurationInSeconds / 3600}h {(DurationInSeconds % 3600) / 60}m"
            : $"{DurationInSeconds / 60}m";
}

// ─── Create ───────────────────────────────────────────────────────────────────

public class VideoCreateDto
{
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    /// <summary>The actual video file (MP4 / MKV / WebM). Up to 2 GB.</summary>
    [Required]
    public IFormFile VideoFile { get; set; } = null!;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Duration must be greater than 0 seconds.")]
    public int DurationInSeconds { get; set; }

    /// <summary>Omit or set to null for Movies.</summary>
    public int? SeasonNumber { get; set; }

    /// <summary>Omit or set to null for Movies.</summary>
    public int? EpisodeNumber { get; set; }

    [Required]
    public int ContentId { get; set; }
}

// ─── Update ───────────────────────────────────────────────────────────────────

public class VideoUpdateDto
{
    [MaxLength(300)]
    public string? Title { get; set; }

    /// <summary>Provide a new file to replace the existing video asset.</summary>
    public IFormFile? VideoFile { get; set; }

    [Range(1, int.MaxValue)]
    public int? DurationInSeconds { get; set; }

    public int? SeasonNumber  { get; set; }
    public int? EpisodeNumber { get; set; }
}
