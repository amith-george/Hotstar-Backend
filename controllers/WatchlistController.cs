using System.Security.Claims;
using HotstarApi.Data;
using HotstarApi.Dtos.Watchlists;
using HotstarApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotstarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WatchlistController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public WatchlistController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ── Helper: extract UserId from the JWT sub claim ─────────────────────────
    private int GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return int.Parse(sub!);
    }

    // ── Helper: verify the profile belongs to the authenticated user ──────────
    private async Task<bool> ProfileBelongsToUserAsync(int profileId, int userId)
        => await _db.Profiles.AnyAsync(p => p.Id == profileId && p.UserId == userId);

    // ─────────────────────────────────────────────────────────────────────────
    // GET api/watchlist?profileId={id}
    // Returns all saved titles for the given profile (the "My List" grid).
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WatchlistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetForProfile([FromQuery] int profileId)
    {
        var userId = GetCurrentUserId();
        if (!await ProfileBelongsToUserAsync(profileId, userId))
            return Forbid();

        var watchlist = await _db.Watchlists
            .Where(wl => wl.ProfileId == profileId)
            .Include(wl => wl.Content)
            .OrderByDescending(wl => wl.AddedAt)
            .Select(wl => new WatchlistDto
            {
                Id          = wl.Id,
                AddedAt     = wl.AddedAt,
                ContentId   = wl.ContentId,
                Title       = wl.Content.Title,
                PosterUrl   = wl.Content.PosterUrl,
                IsPremium   = wl.Content.IsPremium,
                ContentType = wl.Content.ContentType.ToString(),
                ReleaseYear = wl.Content.ReleaseYear
            })
            .ToListAsync();

        return Ok(watchlist);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/watchlist
    // Adds a Content title to the profile's watchlist.
    // Returns 409 Conflict if already in the list (unique index enforces this at
    // the DB level; we surface a friendly message here).
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ProducesResponseType(typeof(WatchlistDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add([FromBody] WatchlistToggleDto dto)
    {
        var userId = GetCurrentUserId();

        if (!await ProfileBelongsToUserAsync(dto.ProfileId, userId))
            return Forbid();

        // Ensure the Content exists
        var content = await _db.Contents.FindAsync(dto.ContentId);
        if (content is null)
            return NotFound(new { message = $"Content with Id {dto.ContentId} was not found." });

        // Duplicate guard — checked in application code before hitting the DB unique index
        if (await _db.Watchlists.AnyAsync(wl => wl.ProfileId == dto.ProfileId && wl.ContentId == dto.ContentId))
            return Conflict(new { message = "This title is already in the watchlist." });

        var item = new Watchlist
        {
            ProfileId = dto.ProfileId,
            ContentId = dto.ContentId,
            AddedAt   = DateTime.UtcNow
        };

        _db.Watchlists.Add(item);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetForProfile), new { profileId = dto.ProfileId }, new WatchlistDto
        {
            Id          = item.Id,
            AddedAt     = item.AddedAt,
            ContentId   = content.Id,
            Title       = content.Title,
            PosterUrl   = content.PosterUrl,
            IsPremium   = content.IsPremium,
            ContentType = content.ContentType.ToString(),
            ReleaseYear = content.ReleaseYear
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DELETE api/watchlist
    // Removes a Content title from the profile's watchlist.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove([FromQuery] int profileId, [FromQuery] int contentId)
    {
        var userId = GetCurrentUserId();

        if (!await ProfileBelongsToUserAsync(profileId, userId))
            return Forbid();

        var item = await _db.Watchlists
            .FirstOrDefaultAsync(wl => wl.ProfileId == profileId && wl.ContentId == contentId);

        if (item is null)
            return NotFound(new { message = "This title is not in the watchlist." });

        _db.Watchlists.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
