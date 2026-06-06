using System.ComponentModel.DataAnnotations;

namespace HotstarApi.Dtos.Profiles;

// ─── Create ──────────────────────────────────────────────────────────────────

public class CreateProfileDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsKidsProfile { get; set; } = false;

    // Relative path returned after uploading an avatar image (optional at creation time)
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
}

// ─── Update ──────────────────────────────────────────────────────────────────

public class UpdateProfileDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    public bool? IsKidsProfile { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
}

// ─── Read ────────────────────────────────────────────────────────────────────

public class ProfileDto
{
    public int    Id           { get; set; }
    public string Name         { get; set; } = string.Empty;
    public string? AvatarUrl   { get; set; }
    public bool   IsKidsProfile { get; set; }
    public int    UserId       { get; set; }
}
