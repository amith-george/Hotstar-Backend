using System.Security.Cryptography;
using System.Text;
using HotstarApi.Data;
using HotstarApi.Dtos.Payments;
using HotstarApi.Models;
using Microsoft.EntityFrameworkCore;
using Razorpay.Api;

namespace HotstarApi.Services;

public interface IPaymentService
{
    Task<OrderResponseDto> CreateOrderAsync(int userId, CreateOrderDto dto);
    Task<TransactionDto>   VerifyAndActivateAsync(int userId, VerifyPaymentDto dto);
    Task<IEnumerable<TransactionDto>> GetHistoryAsync(int userId);
}

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration       _config;

    // Razorpay API credentials — read from configuration
    private string KeyId     => _config["RazorpaySettings:KeyId"]
                                ?? throw new InvalidOperationException("RazorpaySettings:KeyId is not configured.");
    private string KeySecret => _config["RazorpaySettings:KeySecret"]
                                ?? throw new InvalidOperationException("RazorpaySettings:KeySecret is not configured.");

    public PaymentService(ApplicationDbContext db, IConfiguration config)
    {
        _db     = db;
        _config = config;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CREATE ORDER
    // Calls Razorpay to generate an order, saves a Pending transaction.
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<OrderResponseDto> CreateOrderAsync(int userId, CreateOrderDto dto)
    {
        // Validate that the requested plan exists
        var plan = await _db.Subscriptions.FindAsync(dto.PlanId)
            ?? throw new KeyNotFoundException($"Subscription plan with Id {dto.PlanId} does not exist.");

        // Load the user for prefill data
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        // Amount in paise (multiply ₹ by 100). Free plan edge-case: Razorpay requires > 0.
        // We simply skip order creation for Free — handled in controller.
        long amountInPaise = (long)(plan.MonthlyPrice * 100);

        // ── Call Razorpay Orders API ────────────────────────────────────────
        var client = new RazorpayClient(KeyId, KeySecret);

        var orderOptions = new Dictionary<string, object>
        {
            { "amount",          amountInPaise },
            { "currency",        "INR"         },
            { "receipt",         $"hotstar_{userId}_{dto.PlanId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}" },
            { "payment_capture", 1             }   // auto-capture
        };

        var razorpayOrder = client.Order.Create(orderOptions);
        string razorpayOrderId = razorpayOrder["id"].ToString();

        // ── Persist a Pending transaction record ────────────────────────────
        var transaction = new Transaction
        {
            UserId          = userId,
            RazorpayOrderId = razorpayOrderId,
            Amount          = amountInPaise,
            Currency        = "INR",
            Status          = TransactionStatus.Pending,
            PlanIdRequested = dto.PlanId,
            CreatedAt       = DateTime.UtcNow,
            UpdatedAt       = DateTime.UtcNow
        };

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();

        return new OrderResponseDto
        {
            RazorpayOrderId = razorpayOrderId,
            Amount          = amountInPaise,
            Currency        = "INR",
            KeyId           = KeyId,
            UserEmail       = user.Email,
            UserPhone       = user.PhoneNumber,
            PlanName        = plan.Name
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VERIFY & ACTIVATE
    // Cryptographically validates the Razorpay signature.
    // On success: marks transaction Success and upgrades the user's plan.
    // On failure: marks transaction Failed.
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<TransactionDto> VerifyAndActivateAsync(int userId, VerifyPaymentDto dto)
    {
        // Look up the pending transaction — must belong to this user
        var transaction = await _db.Transactions
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.RazorpayOrderId == dto.RazorpayOrderId &&
                t.UserId          == userId               &&
                t.Status          == TransactionStatus.Pending)
            ?? throw new KeyNotFoundException(
                "No pending transaction found for this order. Either it was already processed or does not belong to you.");

        // ── Signature verification (HMAC-SHA256) ───────────────────────────
        // Razorpay specification: sign = HMAC_SHA256(orderId + "|" + paymentId, secret)
        bool isValid = VerifyRazorpaySignature(
            dto.RazorpayOrderId,
            dto.RazorpayPaymentId,
            dto.RazorpaySignature);

        // Update common fields regardless of outcome
        transaction.RazorpayPaymentId = dto.RazorpayPaymentId;
        transaction.RazorpaySignature = dto.RazorpaySignature;
        transaction.UpdatedAt         = DateTime.UtcNow;

        if (isValid)
        {
            transaction.Status = TransactionStatus.Success;

            // Upgrade the user's subscription plan
            var user = await _db.Users.FindAsync(userId)!;
            user!.SubscriptionId = transaction.PlanIdRequested;
        }
        else
        {
            transaction.Status = TransactionStatus.Failed;
        }

        await _db.SaveChangesAsync();

        // Load plan name for the response
        var planName = (await _db.Subscriptions.FindAsync(transaction.PlanIdRequested))?.Name ?? string.Empty;

        var result = ToDto(transaction, planName);

        if (!isValid)
            throw new UnauthorizedAccessException("Payment signature verification failed. Transaction marked as Failed.");

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET HISTORY
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<TransactionDto>> GetHistoryAsync(int userId)
    {
        var transactions = await _db.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Include(t => t.User)
            .ToListAsync();

        // Load plan names in one shot
        var planIds   = transactions.Select(t => t.PlanIdRequested).Distinct().ToList();
        var planNames = await _db.Subscriptions
            .Where(s => planIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return transactions.Select(t =>
            ToDto(t, planNames.TryGetValue(t.PlanIdRequested, out var name) ? name : string.Empty));
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Implements Razorpay's signature verification spec:
    /// HMAC_SHA256(razorpay_order_id + "|" + razorpay_payment_id, key_secret)
    /// and compares it (constant-time) to the signature sent by the frontend.
    /// </summary>
    private bool VerifyRazorpaySignature(string orderId, string paymentId, string signature)
    {
        var payload = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(KeySecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computed = BitConverter.ToString(hash).Replace("-", "").ToLower();

        // Constant-time comparison — prevents timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signature.ToLower()));
    }

    private static TransactionDto ToDto(Transaction t, string planName) => new()
    {
        Id                = t.Id,
        RazorpayOrderId   = t.RazorpayOrderId,
        RazorpayPaymentId = t.RazorpayPaymentId,
        Amount            = t.Amount,
        Currency          = t.Currency,
        Status            = t.Status,
        PlanIdRequested   = t.PlanIdRequested,
        PlanName          = planName,
        CreatedAt         = t.CreatedAt,
        UpdatedAt         = t.UpdatedAt
    };
}
