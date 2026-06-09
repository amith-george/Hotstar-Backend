using System.Linq.Expressions;
using HotstarApi.Data;
using HotstarApi.Dtos.Contents;
using HotstarApi.Models;
using HotstarApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotstarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService  _storage;

    public ContentsController(ApplicationDbContext db, IFileStorageService storage)
    {
        _db      = db;
        _storage = storage;
    }

    // ─── Private helper: parse the comma-separated GenreIds string ────────────
    private static List<int> ParseGenreIds(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? new List<int>()
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .Select(s => int.TryParse(s, out var id) ? id : 0)
                 .Where(id => id > 0)
                 .Distinct()
                 .ToList();

    // ─── Private mapper (DB Projection) ───────────────────────────────────────
    private static Expression<Func<Content, ContentDto>> ToDtoProjection(int? profileId = null) => c => new ContentDto
    {
        Id          = c.Id,
        Title       = c.Title,
        Description = c.Description,
        PosterUrl   = c.PosterUrl,
        BannerUrl   = c.BannerUrl,
        ContentType = c.ContentType.ToString(),
        ReleaseYear = c.ReleaseYear,
        IsPremium   = c.IsPremium,
        Genres      = c.Genres.Select(g => g.Name).OrderBy(n => n).ToList(),
        TotalReviewCount = c.Reviews.Count,
        AverageRating    = c.Reviews.Any() ? c.Reviews.Average(r => (double)r.RatingValue) : 0.0,
        IsInWatchlist    = profileId.HasValue && c.Watchlists.Any(w => w.ProfileId == profileId.Value),
        ResumeTimestamp  = profileId.HasValue
            ? c.Videos.SelectMany(v => v.WatchHistories)
                      .Where(wh => wh.ProfileId == profileId.Value)
                      .OrderByDescending(wh => wh.LastWatchedAt)
                      .Select(wh => (int?)wh.StoppedAtTimestamp)
                      .FirstOrDefault()
            : null
    };

    // ─── Private mapper (Memory mapping for Create/Update responses) ──────────
    private static ContentDto ToDtoMemory(Content c) => new()
    {
        Id          = c.Id,
        Title       = c.Title,
        Description = c.Description,
        PosterUrl   = c.PosterUrl,
        BannerUrl   = c.BannerUrl,
        ContentType = c.ContentType.ToString(),
        ReleaseYear = c.ReleaseYear,
        IsPremium   = c.IsPremium,
        Genres      = c.Genres.Select(g => g.Name).OrderBy(n => n).ToList(),
        TotalReviewCount = 0, // Defaults for immediate response
        AverageRating    = 0.0,
        IsInWatchlist    = false,
        ResumeTimestamp  = null
    };

    // ─────────────────────────────────────────────────────────────────────────
    // GET api/contents
    // Catalog browse — eager-loads Genres so the frontend can display them.
    // Optional query params: ?type=Movie|TVShow  &  ?premium=true|false
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ContentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string?  search  = null,
        [FromQuery] string?  type    = null,
        [FromQuery] bool?    premium = null,
        [FromQuery] int?     genreId = null,
        [FromQuery] int      page    = 1,
        [FromQuery] int      size    = 20)
    {
        var query = _db.Contents
            .Include(c => c.Genres)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(type) &&
            Enum.TryParse<ContentType>(type, ignoreCase: true, out var ct))
            query = query.Where(c => c.ContentType == ct);

        if (premium.HasValue)
            query = query.Where(c => c.IsPremium == premium.Value);

        if (genreId.HasValue)
            query = query.Where(c => c.Genres.Any(g => g.Id == genreId.Value));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Title.ToLower().Contains(search.ToLower()));

        var total   = await query.CountAsync();
        var content = await query
            .OrderByDescending(c => c.ReleaseYear)
            .ThenBy(c => c.Title)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(ToDtoProjection(null))
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", total.ToString());
        Response.Headers.Append("X-Page",        page.ToString());
        Response.Headers.Append("X-Page-Size",   size.ToString());

        return Ok(content);
    }

    // GET api/contents/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, [FromQuery] int? profileId = null)
    {
        var content = await _db.Contents
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(ToDtoProjection(profileId))
            .FirstOrDefaultAsync();

        return content is null ? NotFound() : Ok(content);
    }

    // GET api/contents/{id}/videos
    // Returns all videos / episodes for a given title.
    [HttpGet("{id:int}/videos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVideos(int id)
    {
        if (!await _db.Contents.AnyAsync(c => c.Id == id))
            return NotFound();

        var videos = await _db.Videos
            .Where(v => v.ContentId == id)
            .OrderBy(v => v.SeasonNumber)
            .ThenBy(v => v.EpisodeNumber)
            .Select(v => new Dtos.Videos.VideoDto
            {
                Id                = v.Id,
                Title             = v.Title,
                VideoUrl          = v.VideoUrl,
                DurationInSeconds = v.DurationInSeconds,
                SeasonNumber      = v.SeasonNumber,
                EpisodeNumber     = v.EpisodeNumber,
                ContentId         = v.ContentId
            })
            .ToListAsync();

        return Ok(videos);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/contents
    // Uses [FromForm] because the request is multipart/form-data.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromForm] ContentCreateDto dto)
    {
        var content = new Content
        {
            Title       = dto.Title,
            Description = dto.Description,
            ContentType = dto.ContentType,
            ReleaseYear = dto.ReleaseYear,
            IsPremium   = dto.IsPremium
        };

        // Save poster & banner images
        if (dto.PosterImage is not null)
            content.PosterUrl = await _storage.SaveFileAsync(dto.PosterImage, "posters");

        if (dto.BannerImage is not null)
            content.BannerUrl = await _storage.SaveFileAsync(dto.BannerImage, "banners");

        // Attach genres
        var genreIds = ParseGenreIds(dto.GenreIds);
        if (genreIds.Any())
        {
            var genres = await _db.Genres.Where(g => genreIds.Contains(g.Id)).ToListAsync();
            content.Genres = genres;
        }

        _db.Contents.Add(content);
        await _db.SaveChangesAsync();

        // Reload with genres for the response DTO
        await _db.Entry(content).Collection(c => c.Genres).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = content.Id }, ToDtoMemory(content));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUT api/contents/{id}
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromForm] ContentUpdateDto dto)
    {
        var content = await _db.Contents
            .Include(c => c.Genres)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (content is null) return NotFound();

        if (dto.Title       is not null) content.Title       = dto.Title;
        if (dto.Description is not null) content.Description = dto.Description;
        if (dto.ContentType.HasValue)    content.ContentType = dto.ContentType.Value;
        if (dto.ReleaseYear.HasValue)    content.ReleaseYear = dto.ReleaseYear.Value;
        if (dto.IsPremium.HasValue)      content.IsPremium   = dto.IsPremium.Value;

        // Replace poster — delete old file first
        if (dto.PosterImage is not null)
        {
            _storage.DeleteFile(content.PosterUrl);
            content.PosterUrl = await _storage.SaveFileAsync(dto.PosterImage, "posters");
        }

        // Replace banner — delete old file first
        if (dto.BannerImage is not null)
        {
            _storage.DeleteFile(content.BannerUrl);
            content.BannerUrl = await _storage.SaveFileAsync(dto.BannerImage, "banners");
        }

        // Replace genre associations when provided
        if (dto.GenreIds is not null)
        {
            var genreIds = ParseGenreIds(dto.GenreIds);
            var genres   = await _db.Genres.Where(g => genreIds.Contains(g.Id)).ToListAsync();
            content.Genres.Clear();
            foreach (var g in genres) content.Genres.Add(g);
        }

        await _db.SaveChangesAsync();
        return Ok(ToDtoMemory(content));
    }

    // DELETE api/contents/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var content = await _db.Contents.FindAsync(id);
        if (content is null) return NotFound();

        // Clean up image assets from disk
        _storage.DeleteFile(content.PosterUrl);
        _storage.DeleteFile(content.BannerUrl);

        // EF cascade-deletes Videos (+ their files are cleaned by VideosController on their own deletes)
        _db.Contents.Remove(content);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
