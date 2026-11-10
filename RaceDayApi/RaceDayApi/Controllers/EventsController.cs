using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Extensions;
using RaceDayApi.Models;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    internal static readonly string[] ValidTypes = { "Run", "Walk", "Cycle" };

    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public EventsController(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    /// <summary>Returns all upcoming events, optionally filtered.</summary>
    /// <remarks>Public endpoint. Filters: search (name/location text), type (Run, Walk, Cycle), from and to (dates).</remarks>
    /// <response code="200">Array of upcoming events with their categories.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventResponse>>> GetUpcoming(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery(Name = "from")] DateOnly? dateFrom,
        [FromQuery(Name = "to")] DateOnly? dateTo)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _db.Events.AsNoTracking().Where(e => e.EventDate >= today);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            query = query.Where(e => e.Name.Contains(text) || e.Location.Contains(text));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var t = type.Trim();
            query = query.Where(e => e.EventType == t);
        }

        if (dateFrom.HasValue) query = query.Where(e => e.EventDate >= dateFrom.Value);
        if (dateTo.HasValue) query = query.Where(e => e.EventDate <= dateTo.Value);

        var events = await query.OrderBy(e => e.EventDate).Select(EventResponse.Projection).ToListAsync();
        return Ok(events);
    }

    /// <summary>Returns the logged-in Organiser's own events (past and upcoming).</summary>
    /// <response code="200">Array of events.</response>
    [HttpGet("mine")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventResponse>>> GetMine()
    {
        var me = User.GetUserId();
        var events = await _db.Events.AsNoTracking()
            .Where(e => e.OrganiserID == me)
            .OrderBy(e => e.EventDate)
            .Select(EventResponse.Projection)
            .ToListAsync();
        return Ok(events);
    }

    /// <summary>Returns the full details of one event, including its categories.</summary>
    /// <response code="200">The event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetById(int id)
    {
        var ev = await LoadResponse(id);
        return ev is null ? NotFound(new { message = "Event not found." }) : Ok(ev);
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <remarks>If distanceKm is supplied, a first category is created automatically.</remarks>
    /// <response code="201">The created event.</response>
    /// <response code="400">Invalid data.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventResponse>> Create(EventRequest request)
    {
        var type = NormaliseType(request.EventType);
        if (type is null)
        {
            return BadRequest(new { message = "Event type must be Run, Walk or Cycle." });
        }

        var ev = new Event
        {
            OrganiserID = User.GetUserId()!.Value,
            Name = request.Name.Trim(),
            EventDate = request.EventDate!.Value,
            Location = request.Location.Trim(),
            RouteDescription = request.RouteDescription.Trim(),
            EventType = type
        };

        if (request.DistanceKm.HasValue)
        {
            ev.Categories.Add(new Category
            {
                Name = $"{request.DistanceKm.Value:0.##}km",
                DistanceKm = request.DistanceKm.Value
            });
        }

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventID }, await LoadResponse(ev.EventID));
    }

    /// <summary>Updates an event that belongs to the logged-in Organiser.</summary>
    /// <response code="200">The updated event.</response>
    /// <response code="403">The event belongs to another Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> Update(int id, EventRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        var type = NormaliseType(request.EventType);
        if (type is null)
        {
            return BadRequest(new { message = "Event type must be Run, Walk or Cycle." });
        }

        ev.Name = request.Name.Trim();
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location.Trim();
        ev.RouteDescription = request.RouteDescription.Trim();
        ev.EventType = type;
        await _db.SaveChangesAsync();

        return Ok(await LoadResponse(id));
    }

    /// <summary>Deletes an event that belongs to the logged-in Organiser.</summary>
    /// <remarks>Events that already have enrolments cannot be deleted.</remarks>
    /// <response code="204">Deleted.</response>
    /// <response code="403">The event belongs to another Organiser.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">The event has enrolments.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .Include(e => e.WeatherInfos)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        if (ev.Categories.Any(c => c.Enrolments.Count > 0))
        {
            return Conflict(new { message = "This event already has enrolments and cannot be deleted." });
        }

        var banner = ev.BannerUrl;
        _db.WeatherInfos.RemoveRange(ev.WeatherInfos);
        _db.Categories.RemoveRange(ev.Categories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        await _blobs.DeleteAsync(banner);

        return NoContent();
    }

    /// <summary>Uploads (or replaces) the banner image of an event. The image is stored in Azure Blob Storage.</summary>
    /// <response code="200">The updated event.</response>
    /// <response code="400">Not a valid image.</response>
    /// <response code="403">The event belongs to another Organiser.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="503">Image storage is unavailable.</response>
    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<EventResponse>> UploadBanner(int id, IFormFile file)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        var problem = _blobs.ValidateImage(file);
        if (problem is not null) return BadRequest(new { message = problem });

        string url;
        try
        {
            url = await _blobs.UploadImageAsync(file, "banners");
        }
        catch (StorageUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }

        var old = ev.BannerUrl;
        ev.BannerUrl = url;
        await _db.SaveChangesAsync();
        await _blobs.DeleteAsync(old);

        return Ok(await LoadResponse(id));
    }

    private Task<EventResponse?> LoadResponse(int id) =>
        _db.Events.AsNoTracking().Where(e => e.EventID == id).Select(EventResponse.Projection).FirstOrDefaultAsync();

    private static string? NormaliseType(string value) =>
        ValidTypes.FirstOrDefault(t => t.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase));
}
