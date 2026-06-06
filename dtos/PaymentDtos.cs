using System.ComponentModel.DataAnnotations;

namespace HotstarApi.Dtos.Payments;

// ─── Create Order ─────────────────────────────────────────────────────────────

/// <summary>Sent by the frontend to initiate a payment.</summary>
public class CreateOrderDto
{
    /// <summary>The Subscription.Id the user wants to purchase (1=Free, 2=Basic, 3=Premium).</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PlanId must be a positive integer.")]
    public int PlanId { get; set; }
}

// ─── Order Response ───────────────────────────────────────────────────────────

/// <summary>
/// Returned to the frontend so it can initialise the Razorpay Checkout script.
/// The frontend passes this directly into the Razorpay SDK options object.
/// </summary>
public class OrderResponseDto
{
    /// <summary>Razorpay internal order ID — e.g. "order_OFq7Abc123".</summary>
    public string RazorpayOrderId { get; set; } = string.Empty;

    /// <summary>Amount in paise (₹299 = 29900).</summary>
    public long Amount { get; set; }

    /// <summary>ISO 4217 currency — always "INR" for now.</summary>
    public string Currency { get; set; } = "INR";

    /// <summary>Razorpay Key ID (public key) to initialise the JS SDK on the frontend.</summary>
    public string KeyId { get; set; } = string.Empty;

    // ── Prefill fields for Razorpay Checkout UI ───────────────────────────────
    public string UserEmail { get; set; } = string.Empty;
    public string? UserPhone { get; set; }

    /// <summary>Name of the plan being purchased — shown in Razorpay modal.</summary>
    public string PlanName { get; set; } = string.Empty;
}

// ─── Verify Payment ───────────────────────────────────────────────────────────

/// <summary>
/// Payload sent by the frontend AFTER the Razorpay popup reports a successful payment.
/// The three fields are exactly what Razorpay's JS SDK returns in the `handler` callback.
/// </summary>
public class VerifyPaymentDto
{
    [Required]
    public string RazorpayOrderId   { get; set; } = string.Empty;

    [Required]
    public string RazorpayPaymentId { get; set; } = string.Empty;

    [Required]
    public string RazorpaySignature { get; set; } = string.Empty;
}

// ─── Transaction Read DTO ─────────────────────────────────────────────────────

public class TransactionDto
{
    public int      Id                 { get; set; }
    public string   RazorpayOrderId    { get; set; } = string.Empty;
    public string?  RazorpayPaymentId  { get; set; }
    public long     Amount             { get; set; }

    /// <summary>Human-readable amount — e.g. "₹299.00".</summary>
    public string   AmountFormatted    => $"₹{Amount / 100m:0.00}";

    public string   Currency           { get; set; } = string.Empty;
    public string   Status             { get; set; } = string.Empty;
    public int      PlanIdRequested    { get; set; }
    public string   PlanName           { get; set; } = string.Empty;
    public DateTime CreatedAt          { get; set; }
    public DateTime UpdatedAt          { get; set; }
}
