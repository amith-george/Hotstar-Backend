using System.ComponentModel.DataAnnotations;

namespace HotstarApi.Dtos.Users;

// ─── Session Read ─────────────────────────────────────────────────────────────

public class UserSessionDto
{
    public int      Id               { get; set; }
    public string   DeviceIdentifier { get; set; } = string.Empty;
    public string   IpAddress        { get; set; } = string.Empty;
    public DateTime LastActiveAt     { get; set; }
    public bool     IsActive         { get; set; }

    /// <summary>Human-readable "last seen" label — e.g. "2 hours ago".</summary>
    public string LastActiveLabel =>
        (DateTime.UtcNow - LastActiveAt) switch
        {
            var t when t.TotalMinutes < 1   => "Just now",
            var t when t.TotalHours   < 1   => $"{(int)t.TotalMinutes}m ago",
            var t when t.TotalDays    < 1   => $"{(int)t.TotalHours}h ago",
            var t when t.TotalDays    < 30  => $"{(int)t.TotalDays}d ago",
            _                               => LastActiveAt.ToString("dd MMM yyyy")
        };
}

// ─── Change Password ──────────────────────────────────────────────────────────

public class ChangePasswordDto
{
    [Required]
    public string OldPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

// ─── Create Session (internal — used by login flow) ──────────────────────────

public class CreateSessionDto
{
    [Required]
    [MaxLength(256)]
    public string DeviceIdentifier { get; set; } = string.Empty;
}
