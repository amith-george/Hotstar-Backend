using HotstarApi.Data;
using HotstarApi.Dtos.Auth;
using HotstarApi.Models;
using HotstarApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotstarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IJwtService          _jwt;

    public AuthController(ApplicationDbContext db, IJwtService jwt)
    {
        _db  = db;
        _jwt = jwt;
    }

    // POST api/auth/register
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        // Reject duplicate emails
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email.ToLower()))
            return Conflict(new { message = "An account with this email already exists." });

        // Hash password with BCrypt-style using ASP.NET built-in hasher
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        // Resolve the Free subscription (always seeded as Id = 1)
        var freePlan = await _db.Subscriptions.FindAsync(1);

        var user = new User
        {
            Email        = dto.Email.ToLower(),
            PasswordHash = passwordHash,
            PhoneNumber  = dto.PhoneNumber,
            SubscriptionId = freePlan?.Id,
            CreatedAt    = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Reload with subscription for the token + response
        await _db.Entry(user).Reference(u => u.Subscription).LoadAsync();

        var token = _jwt.GenerateToken(user);

        return CreatedAtAction(nameof(Register), new AuthResponseDto
        {
            Token            = token,
            Email            = user.Email,
            UserId           = user.Id,
            SubscriptionName = user.Subscription?.Name ?? "Free"
        });
    }

    // POST api/auth/login
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var token = _jwt.GenerateToken(user);

        return Ok(new AuthResponseDto
        {
            Token            = token,
            Email            = user.Email,
            UserId           = user.Id,
            SubscriptionName = user.Subscription?.Name ?? "Free"
        });
    }
}
