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
[Authorize]
public class EnrolmentsController : ControllerBase
{
    private static readonly string[] ValidStatuses = { "Pending", "Confirmed", "Cancelled" };

    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Enrols the logged-in Participant in a category. The enrolment starts as Pending.</summary>
    /// <response code="201">The enrolment.</response>
    /// <response code="400">The event has already taken place.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Already enrolled in this event.</response>
    [HttpPost("categories/{categoryId:int}/enrol")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentResponse>> Enrol(int categoryId)
    {
        var me = User.GetUserId()!.Value;
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == categoryId);
        if (category is null) return NotFound(new { message = "Category not found." });

        if (category.Event.EventDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new { message = "This event has already taken place." });
        }

        var existing = await _db.Enrolments
            .Where(e => e.ParticipantID == me && e.Category.EventID == category.EventID)
            .ToListAsync();

        if (existing.Any(e => e.Status != "Cancelled"))
        {
            return Conflict(new { message = "You are already enrolled in this event." });
        }

        // A cancelled enrolment for the same category is re-activated (unique index: participant + category).
        var enrolment = existing.FirstOrDefault(e => e.CategoryID == categoryId);
        if (enrolment is null)
        {
            enrolment = new Enrolment { ParticipantID = me, CategoryID = categoryId };
            _db.Enrolments.Add(enrolment);
        }

        enrolment.Status = "Pending";
        enrolment.EnrolmentDate = DateTime.Now;
        await _db.SaveChangesAsync();

        var response = await _db.Enrolments.AsNoTracking()
            .Where(e => e.EnrolmentID == enrolment.EnrolmentID).Select(EnrolmentResponse.Projection).FirstAsync();
        return Created($"/api/users/{me}/enrolments", response);
    }

    /// <summary>Returns all enrolments of the logged-in Participant.</summary>
    /// <response code="200">Array of enrolments.</response>
    /// <response code="403">The id belongs to a different user.</response>
    [HttpGet("users/{id:int}/enrolments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EnrolmentResponse>>> GetMine(int id)
    {
        if (User.GetUserId() != id) return Forbid();

        var list = await _db.Enrolments.AsNoTracking()
            .Where(e => e.ParticipantID == id)
            .OrderByDescending(e => e.Category.Event.EventDate)
            .Select(EnrolmentResponse.Projection)
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Returns all participants enrolled in an event owned by the logged-in Organiser.</summary>
    /// <response code="200">Array of enrolments.</response>
    /// <response code="403">The event belongs to another Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EnrolmentResponse>>> GetForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        var list = await _db.Enrolments.AsNoTracking()
            .Where(e => e.Category.EventID == eventId)
            .OrderBy(e => e.Category.DistanceKm).ThenBy(e => e.Participant.Name)
            .Select(EnrolmentResponse.Projection)
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Cancels the logged-in Participant's own enrolment.</summary>
    /// <response code="204">Cancelled.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result has already been recorded.</response>
    [HttpDelete("enrolments/{id:int}")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var me = User.GetUserId();
        var enrolment = await _db.Enrolments.Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentID == id && e.ParticipantID == me);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Result is not null)
        {
            return Conflict(new { message = "A result has already been recorded for this enrolment." });
        }

        enrolment.Status = "Cancelled";
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Lets the Organiser of the event confirm, re-open or cancel an enrolment.</summary>
    /// <response code="200">The updated enrolment.</response>
    /// <response code="400">Invalid status.</response>
    /// <response code="403">The event belongs to another Organiser.</response>
    /// <response code="404">Enrolment not found.</response>
    [HttpPut("enrolments/{id:int}/status")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrolmentResponse>> SetStatus(int id, UpdateEnrolmentStatusRequest request)
    {
        var status = ValidStatuses.FirstOrDefault(s => s.Equals(request.Status?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (status is null)
        {
            return BadRequest(new { message = "Status must be Pending, Confirmed or Cancelled." });
        }

        var enrolment = await _db.Enrolments.Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentID == id);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Category.Event.OrganiserID != User.GetUserId()) return Forbid();

        enrolment.Status = status;
        await _db.SaveChangesAsync();

        var response = await _db.Enrolments.AsNoTracking()
            .Where(e => e.EnrolmentID == id).Select(EnrolmentResponse.Projection).FirstAsync();
        return Ok(response);
    }
}
