using HotstarApi.Data;
using HotstarApi.Dtos.Search;
using HotstarApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotstarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SearchController(ApplicationDbContext db) => _db = db;

    // ─────────────────────────────────────────────────────────────────────────
    // GET api/search?keyword=avengers&genreId=3&contentType=Movie&page=1&size=20
    //
    // Searches across Title, Description, and Genre names.
    // AsNoTracking() — purely read-only, no change tracking overhead.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] SearchQueryDto query)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var keyword = query.Keyword.Trim().ToLower();

        var q = _db.Contents
            .AsNoTracking()
            .Include(c => c.Genres)
            .Where(c =>
                // Title / Description partial match
                c.Title.ToLower().Contains(keyword) ||
                (c.Description != null && c.Description.ToLower().Contains(keyword)) ||
                // Genre name partial match (e.g. searching "action" returns Action movies)
                c.Genres.Any(g => g.Name.ToLower().Contains(keyword))
            );

        // ── Optional filters ─────────────────────────────────────────────────
        if (query.GenreId.HasValue)
            q = q.Where(c => c.Genres.Any(g => g.Id == query.GenreId.Value));

        if (!string.IsNullOrWhiteSpace(query.ContentType) &&
            Enum.TryParse<ContentType>(query.ContentType, out var parsedType))
        {
            q = q.Where(c => c.ContentType == parsedType);
        }

        // ── Pagination ───────────────────────────────────────────────────────
        var totalCount = await q.CountAsync();

        var results = await q
            .OrderBy(c => c.Title)
            .Skip((query.Page - 1) * query.Size)
            .Take(query.Size)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Description,
                c.PosterUrl,
                c.BannerUrl,
                ContentType = c.ContentType.ToString(),
                c.ReleaseYear,
                c.IsPremium,
                Genres = c.Genres.Select(g => g.Name).ToList()
            })
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page",        query.Page.ToString());
        Response.Headers.Append("X-Page-Size",   query.Size.ToString());

        return Ok(results);
    }
}
