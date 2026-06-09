using System.Security.Claims;
using HotstarApi.Data;
using HotstarApi.Dtos.Users;
using HotstarApi.Models;
using HotstarApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotstarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IStreamGuardService  _streamGuard;

    public UserController(ApplicationDbContext db, IStreamGuardService streamGuard)
    {
        _db          = db;
        _streamGuard = streamGuard;
    }

    private int GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return int.Parse(sub!);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET api/user/sessions
    // Returns all sessions for the authenticated user.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IEnumerable<UserSessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions()
    {
        var userId = GetCurrentUserId();

        var sessions = await _db.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastActiveAt)
            .Select(s => new UserSessionDto
            {
                Id               = s.Id,
                DeviceIdentifier = s.DeviceIdentifier,
                IpAddress        = s.IpAddress,
                LastActiveAt     = s.LastActiveAt,
                IsActive         = s.IsActive
            })
            .ToListAsync();

        return Ok(sessions);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DELETE api/user/sessions/{id}
    // Remotely revokes (soft-deletes) a specific device session.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpDelete("sessions/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession(int id)
    {
        var userId  = GetCurrentUserId();
        var session = await _db.UserSessions.FindAsync(id);

        if (session is null)                   return NotFound();
        if (session.UserId != userId)          return Forbid();
        if (!session.IsActive)                 return NoContent(); // idempotent

        session.IsActive     = false;
        session.LastActiveAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/user/sessions
    // Creates a new session record for the authenticated user.
    // Called by the frontend on login to register the current device.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(UserSessionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionDto dto)
    {
        var userId    = GetCurrentUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // If this device already has an active session, refresh it instead of creating a duplicate
        var existing = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.UserId == userId &&
                                      s.DeviceIdentifier == dto.DeviceIdentifier &&
                                      s.IsActive);

        if (existing is not null)
        {
            existing.LastActiveAt = DateTime.UtcNow;
            existing.IpAddress    = ipAddress;
            await _db.SaveChangesAsync();

            return Ok(new UserSessionDto
            {
                Id               = existing.Id,
                DeviceIdentifier = existing.DeviceIdentifier,
                IpAddress        = existing.IpAddress,
                LastActiveAt     = existing.LastActiveAt,
                IsActive         = existing.IsActive
            });
        }

        var session = new UserSession
        {
            UserId           = userId,
            DeviceIdentifier = dto.DeviceIdentifier,
            IpAddress        = ipAddress,
            LastActiveAt     = DateTime.UtcNow,
            IsActive         = true
        };

        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSessions), new UserSessionDto
        {
            Id               = session.Id,
            DeviceIdentifier = session.DeviceIdentifier,
            IpAddress        = session.IpAddress,
            LastActiveAt     = session.LastActiveAt,
            IsActive         = session.IsActive
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/user/check-stream
    // Stream concurrency guard — frontend calls this before initiating playback.
    // Returns 200 OK if allowed, 403 Forbidden if the plan limit is exceeded.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("check-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckStreamAllowed()
    {
        var userId = GetCurrentUserId();

        if (await _streamGuard.CanStartStreamAsync(userId))
            return Ok(new { allowed = true });

        return StatusCode(StatusCodes.Status403Forbidden,
            new { allowed = false, message = _streamGuard.ConcurrencyLimitMessage });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUT api/user/change-password
    // Authenticated password change (requires old password verification).
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPut("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var user   = await _db.Users.FindAsync(userId);

        if (user is null) return Unauthorized();

        // Verify the current password
        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully." });
    }
}
