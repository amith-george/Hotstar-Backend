using System.Security.Claims;
using HotstarApi.Data;
using HotstarApi.Dtos.Profiles;
using HotstarApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotstarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfilesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProfilesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ── Helper: resolve the authenticated user's Id from JWT claims ──────────
    private int GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return int.Parse(sub!);
    }

    // GET api/profiles
    /// <summary>Returns all profiles belonging to the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var userId   = GetCurrentUserId();
        var profiles = await _db.Profiles
            .Where(p => p.UserId == userId)
            .Select(p => new ProfileDto
            {
                Id           = p.Id,
                Name         = p.Name,
                AvatarUrl    = p.AvatarUrl,
                IsKidsProfile = p.IsKidsProfile,
                UserId       = p.UserId
            })
            .ToListAsync();

        return Ok(profiles);
    }

    // GET api/profiles/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var userId  = GetCurrentUserId();
        var profile = await _db.Profiles
            .Where(p => p.Id == id && p.UserId == userId)
            .Select(p => new ProfileDto
            {
                Id           = p.Id,
                Name         = p.Name,
                AvatarUrl    = p.AvatarUrl,
                IsKidsProfile = p.IsKidsProfile,
                UserId       = p.UserId
            })
            .FirstOrDefaultAsync();

        return profile is null ? NotFound() : Ok(profile);
    }

    // POST api/profiles
    [HttpPost]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProfileDto dto)
    {
        var userId = GetCurrentUserId();

        // Hotstar limits: max 5 profiles per account
        var count = await _db.Profiles.CountAsync(p => p.UserId == userId);
        if (count >= 5)
            return BadRequest(new { message = "Maximum of 5 profiles per account." });

        var profile = new Profile
        {
            Name          = dto.Name,
            AvatarUrl     = dto.AvatarUrl,
            IsKidsProfile = dto.IsKidsProfile,
            UserId        = userId
        };

        _db.Profiles.Add(profile);
        await _db.SaveChangesAsync();

        var response = new ProfileDto
        {
            Id           = profile.Id,
            Name         = profile.Name,
            AvatarUrl    = profile.AvatarUrl,
            IsKidsProfile = profile.IsKidsProfile,
            UserId       = profile.UserId
        };

        return CreatedAtAction(nameof(GetById), new { id = profile.Id }, response);
    }

    // PUT api/profiles/{id}
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProfileDto dto)
    {
        var userId  = GetCurrentUserId();
        var profile = await _db.Profiles
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (profile is null) return NotFound();

        // Patch only supplied fields
        if (dto.Name         is not null) profile.Name         = dto.Name;
        if (dto.IsKidsProfile is not null) profile.IsKidsProfile = dto.IsKidsProfile.Value;
        if (dto.AvatarUrl    is not null) profile.AvatarUrl    = dto.AvatarUrl;

        await _db.SaveChangesAsync();

        return Ok(new ProfileDto
        {
            Id           = profile.Id,
            Name         = profile.Name,
            AvatarUrl    = profile.AvatarUrl,
            IsKidsProfile = profile.IsKidsProfile,
            UserId       = profile.UserId
        });
    }

    // DELETE api/profiles/{id}
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId  = GetCurrentUserId();
        var profile = await _db.Profiles
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (profile is null) return NotFound();

        _db.Profiles.Remove(profile);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
