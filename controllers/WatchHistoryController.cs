using System.Security.Claims;
using HotstarApi.Data;
using HotstarApi.Dtos.WatchHistories;
using HotstarApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotstarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WatchHistoryController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public WatchHistoryController(ApplicationDbContext db)
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
    // GET api/watchhistory?profileId={id}
    // Returns the "Continue Watching" list for a profile, sorted most-recent first.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WatchHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetForProfile([FromQuery] int profileId)
    {
        var userId = GetCurrentUserId();
        if (!await ProfileBelongsToUserAsync(profileId, userId))
            return Forbid();

        var history = await _db.WatchHistories
            .Where(wh => wh.ProfileId == profileId)
            .Include(wh => wh.Video)
                .ThenInclude(v => v.Content)
            .OrderByDescending(wh => wh.LastWatchedAt)
            .Select(wh => new WatchHistoryDto
            {
                Id                 = wh.Id,
                VideoId            = wh.VideoId,
                VideoTitle         = wh.Video.Title,
                StoppedAtTimestamp = wh.StoppedAtTimestamp,
                LastWatchedAt      = wh.LastWatchedAt,
                ContentId          = wh.Video.ContentId,
                ContentTitle       = wh.Video.Content.Title,
                PosterUrl          = wh.Video.Content.PosterUrl,
                IsPremium          = wh.Video.Content.IsPremium,
                ContentType        = wh.Video.Content.ContentType.ToString(),
                SeasonNumber       = wh.Video.SeasonNumber,
                EpisodeNumber      = wh.Video.EpisodeNumber,
                DurationInSeconds  = wh.Video.DurationInSeconds
            })
            .ToListAsync();

        return Ok(history);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/watchhistory
    // Upsert — the frontend pulses this every ~10 seconds during playback.
    // If a WatchHistory record already exists for ProfileId + VideoId, update it.
    // If not, create a new one.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ProducesResponseType(typeof(WatchHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Log([FromBody] WatchHistoryLogDto dto)
    {
        var userId = GetCurrentUserId();

        // Ownership check — the profile must belong to this user
        if (!await ProfileBelongsToUserAsync(dto.ProfileId, userId))
            return Forbid();

        // Ensure the video exists
        var video = await _db.Videos
            .Include(v => v.Content)
            .FirstOrDefaultAsync(v => v.Id == dto.VideoId);

        if (video is null)
            return NotFound(new { message = $"Video with Id {dto.VideoId} was not found." });

        // Upsert — look for an existing record for this Profile + Video pair
        var existing = await _db.WatchHistories
            .FirstOrDefaultAsync(wh => wh.ProfileId == dto.ProfileId && wh.VideoId == dto.VideoId);

        if (existing is not null)
        {
            // Update existing record
            existing.StoppedAtTimestamp = dto.StoppedAtTimestamp;
            existing.LastWatchedAt      = DateTime.UtcNow;
        }
        else
        {
            // Create new record
            existing = new WatchHistory
            {
                ProfileId          = dto.ProfileId,
                VideoId            = dto.VideoId,
                StoppedAtTimestamp = dto.StoppedAtTimestamp,
                LastWatchedAt      = DateTime.UtcNow
            };
            _db.WatchHistories.Add(existing);
        }

        await _db.SaveChangesAsync();

        return Ok(new WatchHistoryDto
        {
            Id                 = existing.Id,
            VideoId            = existing.VideoId,
            VideoTitle         = video.Title,
            StoppedAtTimestamp = existing.StoppedAtTimestamp,
            LastWatchedAt      = existing.LastWatchedAt,
            ContentId          = video.ContentId,
            ContentTitle       = video.Content.Title,
            PosterUrl          = video.Content.PosterUrl,
            IsPremium          = video.Content.IsPremium,
            ContentType        = video.Content.ContentType.ToString(),
            SeasonNumber       = video.SeasonNumber,
            EpisodeNumber      = video.EpisodeNumber,
            DurationInSeconds  = video.DurationInSeconds
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DELETE api/watchhistory/{id}
    // Removes a specific watch history entry (e.g. user manually clears a title).
    // ─────────────────────────────────────────────────────────────────────────
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();

        var entry = await _db.WatchHistories
            .Include(wh => wh.Profile)
            .FirstOrDefaultAsync(wh => wh.Id == id);

        if (entry is null) return NotFound();

        // Verify ownership via the profile's UserId
        if (entry.Profile.UserId != userId) return Forbid();

        _db.WatchHistories.Remove(entry);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
