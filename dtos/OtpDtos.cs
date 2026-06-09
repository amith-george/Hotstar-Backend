using System.ComponentModel.DataAnnotations;
using HotstarApi.Models;

namespace HotstarApi.Dtos.Auth;

// ─── OTP Request ──────────────────────────────────────────────────────────────

public class RequestOtpDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>"Registration" | "PasswordReset"</summary>
    [Required]
    [RegularExpression("^(Registration|PasswordReset)$",
        ErrorMessage = "Purpose must be 'Registration' or 'PasswordReset'.")]
    public string Purpose { get; set; } = string.Empty;
}

// ─── OTP Verify ───────────────────────────────────────────────────────────────

public class VerifyOtpDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits.")]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "OTP must be numeric.")]
    public string OtpCode { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Registration|PasswordReset)$",
        ErrorMessage = "Purpose must be 'Registration' or 'PasswordReset'.")]
    public string Purpose { get; set; } = string.Empty;
}
