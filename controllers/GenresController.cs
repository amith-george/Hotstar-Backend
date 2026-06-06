using HotstarApi.Data;
using HotstarApi.Dtos.Genres;
using HotstarApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotstarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenresController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public GenresController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET api/genres
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GenreDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var genres = await _db.Genres
            .OrderBy(g => g.Name)
            .Select(g => new GenreDto { Id = g.Id, Name = g.Name })
            .ToListAsync();

        return Ok(genres);
    }

    // GET api/genres/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GenreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var genre = await _db.Genres.FindAsync(id);
        return genre is null
            ? NotFound()
            : Ok(new GenreDto { Id = genre.Id, Name = genre.Name });
    }

    // POST api/genres — admin only in production; open here for dev convenience
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(GenreDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] GenreCreateDto dto)
    {
        if (await _db.Genres.AnyAsync(g => g.Name == dto.Name))
            return Conflict(new { message = $"Genre '{dto.Name}' already exists." });

        var genre = new Genre { Name = dto.Name };
        _db.Genres.Add(genre);
        await _db.SaveChangesAsync();

        var result = new GenreDto { Id = genre.Id, Name = genre.Name };
        return CreatedAtAction(nameof(GetById), new { id = genre.Id }, result);
    }

    // PUT api/genres/{id}
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(GenreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] GenreUpdateDto dto)
    {
        var genre = await _db.Genres.FindAsync(id);
        if (genre is null) return NotFound();

        if (await _db.Genres.AnyAsync(g => g.Name == dto.Name && g.Id != id))
            return Conflict(new { message = $"Genre '{dto.Name}' already exists." });

        genre.Name = dto.Name;
        await _db.SaveChangesAsync();

        return Ok(new GenreDto { Id = genre.Id, Name = genre.Name });
    }

    // DELETE api/genres/{id}
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var genre = await _db.Genres.FindAsync(id);
        if (genre is null) return NotFound();

        _db.Genres.Remove(genre);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
