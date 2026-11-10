using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Extensions;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Returns all categories of an event.</summary>
    /// <response code="200">Array of categories.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CategoryResponse>>> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
        {
            return NotFound(new { message = "Event not found." });
        }

        var list = await _db.Categories.AsNoTracking()
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.DistanceKm)
            .Select(CategoryResponse.Projection)
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Adds a category to an event owned by the logged-in Organiser.</summary>
    /// <response code="201">The created category.</response>
    /// <response code="403">The event belongs to another Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost("events/{eventId:int}/categories")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> Create(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        var name = request.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventID == eventId && c.Name == name))
        {
            return Conflict(new { message = "This event already has a category with that name." });
        }

        var category = new Category { EventID = eventId, Name = name, DistanceKm = request.DistanceKm!.Value };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var response = await _db.Categories.AsNoTracking()
            .Where(c => c.CategoryID == category.CategoryID).Select(CategoryResponse.Projection).FirstAsync();
        return Created($"/api/events/{eventId}/categories", response);
    }

    /// <summary>Updates a category of an event owned by the logged-in Organiser.</summary>
    /// <response code="200">The updated category.</response>
    /// <response code="403">The event belongs to another Organiser.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> Update(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category is null) return NotFound(new { message = "Category not found." });
        if (category.Event.OrganiserID != User.GetUserId()) return Forbid();

        var name = request.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventID == category.EventID && c.Name == name && c.CategoryID != id))
        {
            return Conflict(new { message = "This event already has a category with that name." });
        }

        category.Name = name;
        category.DistanceKm = request.DistanceKm!.Value;
        await _db.SaveChangesAsync();

        var response = await _db.Categories.AsNoTracking()
            .Where(c => c.CategoryID == id).Select(CategoryResponse.Projection).FirstAsync();
        return Ok(response);
    }

    /// <summary>Removes a category. Categories that already have enrolments cannot be removed.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="403">The event belongs to another Organiser.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">The category has enrolments.</response>
    [HttpDelete("categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category is null) return NotFound(new { message = "Category not found." });
        if (category.Event.OrganiserID != User.GetUserId()) return Forbid();

        if (category.Enrolments.Count > 0)
        {
            return Conflict(new { message = "This category already has enrolments and cannot be removed." });
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
