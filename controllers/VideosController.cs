using HotstarApi.Data;
using HotstarApi.Dtos.Videos;
using HotstarApi.Models;
using HotstarApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotstarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideosController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService  _storage;

    public VideosController(ApplicationDbContext db, IFileStorageService storage)
    {
        _db      = db;
        _storage = storage;
    }

    // ─── Private mapper ───────────────────────────────────────────────────────
    private static VideoDto ToDto(Video v) => new()
    {
        Id                = v.Id,
        Title             = v.Title,
        VideoUrl          = v.VideoUrl,
        DurationInSeconds = v.DurationInSeconds,
        SeasonNumber      = v.SeasonNumber,
        EpisodeNumber     = v.EpisodeNumber,
        ContentId         = v.ContentId
    };

    // GET api/videos/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VideoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var video = await _db.Videos.FindAsync(id);
        return video is null ? NotFound() : Ok(ToDto(video));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/videos
    // Accepts multipart/form-data. 2 GB request limit for large video files.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(2_147_483_648)]           // 2 GB
    [RequestFormLimits(MultipartBodyLengthLimit = 2_147_483_648)]
    [ProducesResponseType(typeof(VideoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromForm] VideoCreateDto dto)
    {
        // Validate that the parent Content exists
        if (!await _db.Contents.AnyAsync(c => c.Id == dto.ContentId))
            return NotFound(new { message = $"Content with Id {dto.ContentId} was not found." });

        // Validate episode fields for TV shows
        var parentType = await _db.Contents
            .Where(c => c.Id == dto.ContentId)
            .Select(c => c.ContentType)
            .FirstAsync();

        if (parentType == ContentType.TVShow &&
            (dto.SeasonNumber is null || dto.EpisodeNumber is null))
            return BadRequest(new { message = "SeasonNumber and EpisodeNumber are required for TV show episodes." });

        // Save video file to wwwroot/uploads/videos/
        var videoUrl = await _storage.SaveFileAsync(dto.VideoFile, "videos");

        var video = new Video
        {
            Title             = dto.Title,
            VideoUrl          = videoUrl,
            DurationInSeconds = dto.DurationInSeconds,
            SeasonNumber      = dto.SeasonNumber,
            EpisodeNumber     = dto.EpisodeNumber,
            ContentId         = dto.ContentId
        };

        _db.Videos.Add(video);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = video.Id }, ToDto(video));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUT api/videos/{id}
    // 2 GB limit — caller may upload a replacement video file.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPut("{id:int}")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(2_147_483_648)]
    [RequestFormLimits(MultipartBodyLengthLimit = 2_147_483_648)]
    [ProducesResponseType(typeof(VideoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromForm] VideoUpdateDto dto)
    {
        var video = await _db.Videos.FindAsync(id);
        if (video is null) return NotFound();

        if (dto.Title             is not null) video.Title             = dto.Title;
        if (dto.DurationInSeconds.HasValue)    video.DurationInSeconds = dto.DurationInSeconds.Value;
        if (dto.SeasonNumber.HasValue)         video.SeasonNumber      = dto.SeasonNumber;
        if (dto.EpisodeNumber.HasValue)        video.EpisodeNumber     = dto.EpisodeNumber;

        // Replace video file — delete old asset first
        if (dto.VideoFile is not null)
        {
            _storage.DeleteFile(video.VideoUrl);
            video.VideoUrl = await _storage.SaveFileAsync(dto.VideoFile, "videos");
        }

        await _db.SaveChangesAsync();
        return Ok(ToDto(video));
    }

    // DELETE api/videos/{id}
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var video = await _db.Videos.FindAsync(id);
        if (video is null) return NotFound();

        // Remove the physical video file from disk before deleting the record
        _storage.DeleteFile(video.VideoUrl);

        _db.Videos.Remove(video);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
