namespace HotstarApi.Models;

/// <summary>Strongly-typed purpose constants — avoids magic strings.</summary>
public static class OtpPurpose
{
    public const string Registration  = "Registration";
    public const string PasswordReset = "PasswordReset";
}

/// <summary>
/// Temporal one-time password record.
/// Expires after 5 minutes and is marked used on consumption — prevents replay attacks.
/// NOT linked to a user row so pre-registration OTPs (for email verification) still work.
/// </summary>
public class OtpToken
{
    public int    Id      { get; set; }

    public string Email   { get; set; } = string.Empty;

    /// <summary>6-digit numeric code stored as string to preserve any leading zeros.</summary>
    public string OtpCode { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; } = false;

    /// <summary>"Registration" | "PasswordReset" — see <see cref="OtpPurpose"/>.</summary>
    public string Purpose { get; set; } = string.Empty;
}
