using System.Security.Claims;
using HotstarApi.Dtos.Payments;
using HotstarApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotstarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;

    public PaymentsController(IPaymentService payments)
    {
        _payments = payments;
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private int GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return int.Parse(sub!);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/payments/create-order
    // Validates the plan, calls Razorpay, saves a Pending transaction,
    // and returns the data the frontend needs to open the payment popup.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("create-order")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var userId = GetCurrentUserId();

        try
        {
            var response = await _payments.CreateOrderAsync(userId, dto);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Razorpay credentials not configured
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Payment gateway is not configured. " + ex.Message });
        }
        catch (Exception ex)
        {
            // Razorpay API errors surface here (network, bad credentials, etc.)
            return BadRequest(new { message = "Failed to create Razorpay order.", detail = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/payments/verify
    // Receives the three Razorpay fields from the frontend handler callback.
    // Verifies HMAC-SHA256 signature; on success upgrades the user's plan.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("verify")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Verify([FromBody] VerifyPaymentDto dto)
    {
        var userId = GetCurrentUserId();

        try
        {
            var result = await _payments.VerifyAndActivateAsync(userId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            // Signature mismatch — fraud attempt or tampered data
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Payment verification failed.", detail = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET api/payments/history
    // Returns the authenticated user's full payment history.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory()
    {
        var userId = GetCurrentUserId();

        try
        {
            var history = await _payments.GetHistoryAsync(userId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
