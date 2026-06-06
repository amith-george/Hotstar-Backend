using System.Security.Claims;
using HotstarApi.Data;
using HotstarApi.Dtos.Reviews;
using HotstarApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotstarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ReviewsController(ApplicationDbContext db) => _db = db;

    private int GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.Parse(sub!);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/reviews
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewDto dto)
    {
        var userId  = GetCurrentUserId();
        var profile = await _db.Profiles.FindAsync(dto.ProfileId);

        // Security: Ensure the profile belongs to the logged-in user
        if (profile is null) return BadRequest("Profile not found.");
        if (profile.UserId != userId) return Forbid();

        // Check if content exists
        if (!await _db.Contents.AnyAsync(c => c.Id == dto.ContentId))
            return BadRequest("Content not found.");

        // Check for existing review (Composite Unique Index enforcement)
        var existing = await _db.Reviews
            .FirstOrDefaultAsync(r => r.ProfileId == dto.ProfileId && r.ContentId == dto.ContentId);

        if (existing is not null)
            return BadRequest(new { message = "You have already reviewed this content. Please update your existing review." });

        var review = new Review
        {
            ProfileId   = dto.ProfileId,
            ContentId   = dto.ContentId,
            RatingValue = dto.RatingValue,
            ReviewText  = dto.ReviewText,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetContentReviews), new { contentId = review.ContentId }, new { review.Id });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUT api/reviews/{id}
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdateReviewDto dto)
    {
        var userId = GetCurrentUserId();
        var review = await _db.Reviews
            .Include(r => r.Profile)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review is null) return NotFound();

        // Security: Ensure the profile that owns the review belongs to the logged-in user
        if (review.Profile.UserId != userId) return Forbid();

        if (dto.RatingValue.HasValue) review.RatingValue = dto.RatingValue.Value;
        if (dto.ReviewText is not null) review.ReviewText  = dto.ReviewText;
        
        review.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Review updated successfully." });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DELETE api/reviews/{id}
    // ─────────────────────────────────────────────────────────────────────────
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var userId = GetCurrentUserId();
        var review = await _db.Reviews
            .Include(r => r.Profile)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review is null) return NotFound();
        if (review.Profile.UserId != userId) return Forbid();

        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET api/reviews/content/{contentId}
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("content/{contentId:int}")]
    [ProducesResponseType(typeof(IEnumerable<ReviewDetailsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContentReviews(int contentId, [FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var query = _db.Reviews
            .AsNoTracking()
            .Include(r => r.Profile)
            .Where(r => r.ContentId == contentId);

        var total = await query.CountAsync();

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(r => new ReviewDetailsDto
            {
                Id          = r.Id,
                ContentId   = r.ContentId,
                RatingValue = r.RatingValue,
                ReviewText  = r.ReviewText,
                CreatedAt   = r.CreatedAt,
                UpdatedAt   = r.UpdatedAt,
                ProfileId   = r.ProfileId,
                ProfileName = r.Profile.Name,
                AvatarUrl   = r.Profile.AvatarUrl
            })
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", total.ToString());
        Response.Headers.Append("X-Page",        page.ToString());
        Response.Headers.Append("X-Page-Size",   size.ToString());

        return Ok(reviews);
    }
}
