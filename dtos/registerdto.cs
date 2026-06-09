using System.ComponentModel.DataAnnotations;

namespace HotstarApi.Dtos.Auth;

// ─── Register ───────────────────────────────────────────────────────────────

public class RegisterDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}

// ─── Login ──────────────────────────────────────────────────────────────────

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

// ─── Auth Response ──────────────────────────────────────────────────────────

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int    UserId { get; set; }
    public string SubscriptionName { get; set; } = string.Empty;
}
