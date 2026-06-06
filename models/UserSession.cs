namespace HotstarApi.Models;

/// <summary>
/// Represents one authenticated device/browser session.
/// Active sessions are counted against subscription concurrency limits.
/// </summary>
public class UserSession
{
    public int Id { get; set; }

    /// <summary>
    /// Client-generated stable identifier — e.g. browser fingerprint, app install UUID.
    /// Indexed with a unique constraint scoped to the user.
    /// </summary>
    public string DeviceIdentifier { get; set; } = string.Empty;

    /// <summary>IP address captured at login — useful for audit and anomaly detection.</summary>
    public string IpAddress { get; set; } = string.Empty;

    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// false = session revoked remotely.
    /// Only active sessions count toward the concurrency limit.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // FK → User (cascade delete: account deleted → all sessions removed)
    public int  UserId { get; set; }
    public User User   { get; set; } = null!;
}
