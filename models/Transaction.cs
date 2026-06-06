namespace HotstarApi.Models;

/// <summary>
/// Financial ledger record for every Razorpay payment attempt.
/// Rows are NEVER deleted — even if the user account is deleted (Restrict).
/// </summary>
public class Transaction
{
    public int Id { get; set; }

    // ── Razorpay identifiers ─────────────────────────────────────────────────

    /// <summary>Razorpay order ID returned from order creation (rzp_live_xxx / order_xxx).</summary>
    public string RazorpayOrderId { get; set; } = string.Empty;

    /// <summary>Razorpay payment ID — set after the user completes payment on the frontend.</summary>
    public string? RazorpayPaymentId { get; set; }

    /// <summary>HMAC-SHA256 signature sent by Razorpay frontend SDK — verified during /verify.</summary>
    public string? RazorpaySignature { get; set; }

    // ── Financial fields ─────────────────────────────────────────────────────

    /// <summary>Amount in the smallest currency unit (paise for INR). E.g. ₹299 = 29900.</summary>
    public long Amount { get; set; }

    /// <summary>ISO 4217 currency code. Default: "INR".</summary>
    public string Currency { get; set; } = "INR";

    // ── Status ───────────────────────────────────────────────────────────────

    /// <summary>Pending | Success | Failed</summary>
    public string Status { get; set; } = TransactionStatus.Pending;

    // ── Business context ─────────────────────────────────────────────────────

    /// <summary>Subscription Id the user was attempting to purchase.</summary>
    public int PlanIdRequested { get; set; }

    // ── Timestamps ───────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── FK → User ────────────────────────────────────────────────────────────
    public int  UserId { get; set; }
    public User User   { get; set; } = null!;
}

/// <summary>Strongly-typed status constants to avoid magic strings.</summary>
public static class TransactionStatus
{
    public const string Pending = "Pending";
    public const string Success = "Success";
    public const string Failed  = "Failed";
}
