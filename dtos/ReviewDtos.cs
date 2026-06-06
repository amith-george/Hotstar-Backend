using System.ComponentModel.DataAnnotations;

namespace HotstarApi.Dtos.Reviews;

// ─── Input DTOs ──────────────────────────────────────────────────────────────

public class SubmitReviewDto
{
    [Required]
    public int ProfileId { get; set; }

    [Required]
    public int ContentId { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
    public int RatingValue { get; set; }

    [MaxLength(500)]
    public string? ReviewText { get; set; }
}

public class UpdateReviewDto
{
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
    public int? RatingValue { get; set; }

    [MaxLength(500)]
    public string? ReviewText { get; set; }
}

// ─── Output DTOs ─────────────────────────────────────────────────────────────

public class ReviewDetailsDto
{
    public int      Id           { get; set; }
    public int      ContentId    { get; set; }
    public int      RatingValue  { get; set; }
    public string?  ReviewText   { get; set; }
    public DateTime CreatedAt    { get; set; }
    public DateTime UpdatedAt    { get; set; }

    // Flattened Profile Data
    public int     ProfileId     { get; set; }
    public string  ProfileName   { get; set; } = string.Empty;
    public string? AvatarUrl     { get; set; }
}

public class ContentRatingSummaryDto
{
    public int    ContentId        { get; set; }
    public double AverageRating    { get; set; }
    public int    TotalReviewCount { get; set; }
}
