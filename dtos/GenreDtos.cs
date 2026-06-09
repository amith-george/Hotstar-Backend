using System.ComponentModel.DataAnnotations;

namespace HotstarApi.Dtos.Genres;

// ─── Read ─────────────────────────────────────────────────────────────────────

public class GenreDto
{
    public int    Id   { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ─── Create ───────────────────────────────────────────────────────────────────

public class GenreCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

// ─── Update ───────────────────────────────────────────────────────────────────

public class GenreUpdateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
