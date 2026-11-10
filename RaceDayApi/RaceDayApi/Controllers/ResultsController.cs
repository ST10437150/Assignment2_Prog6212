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
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Captures (or corrects) the finish time and position for an enrolment.</summary>
    /// <remarks>
    /// Organiser of the event only. The event must have taken place, the enrolment must be Confirmed,
    /// and positions must be unique within a category.
    /// </remarks>
    /// <response code="201">Result created.</response>
    /// <response code="200">Existing result updated.</response>
    /// <response code="400">Event not concluded, or enrolment not confirmed.</response>
    /// <response code="403">The event belongs to another Organiser.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">Another finisher already has that position.</response>
    [HttpPost("enrolments/{enrolmentId:int}/results")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultResponse>> Save(int enrolmentId, ResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Result)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);

        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Category.Event.OrganiserID != User.GetUserId()) return Forbid();

        if (enrolment.Category.Event.EventDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new { message = "Results can only be captured after the event has taken place." });
        }

        if (enrolment.Status != "Confirmed")
        {
            return BadRequest(new { message = "Results can only be captured for confirmed enrolments." });
        }

        if (request.FinishTime!.Value < TimeSpan.Zero || request.FinishTime.Value >= TimeSpan.FromDays(1))
        {
            return BadRequest(new { message = "Finish time must be between 00:00:00 and 23:59:59." });
        }

        var position = request.Position!.Value;
        var taken = await _db.Results.AnyAsync(r =>
            r.Position == position &&
            r.Enrolment.CategoryID == enrolment.CategoryID &&
            r.EnrolmentID != enrolmentId);
        if (taken)
        {
            return Conflict(new { message = $"Position {position} is already assigned in this category." });
        }

        var isNew = enrolment.Result is null;
        if (isNew)
        {
            enrolment.Result = new Result { EnrolmentID = enrolmentId };
        }

        enrolment.Result!.FinishTime = request.FinishTime;
        enrolment.Result.Position = position;
        await _db.SaveChangesAsync();

        var response = await QueryResults().Where(r => r.EnrolmentID == enrolmentId).FirstAsync();
        return isNew ? Created($"/api/events/{response.EventID}/results", response) : Ok(response);
    }

    /// <summary>Returns the personal race history of the logged-in Participant.</summary>
    /// <response code="200">Array of results, newest first.</response>
    /// <response code="403">The id belongs to a different user.</response>
    [HttpGet("users/{id:int}/results")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ResultResponse>>> GetMine(int id)
    {
        if (User.GetUserId() != id) return Forbid();

        var list = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment.ParticipantID == id)
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .Select(ToResponse)
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Returns all results of an event owned by the logged-in Organiser.</summary>
    /// <response code="200">Array of results.</response>
    /// <response code="403">The event belongs to another Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/results")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ResultResponse>>> GetForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        var list = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment.Category.EventID == eventId)
            .OrderBy(r => r.Enrolment.Category.DistanceKm).ThenBy(r => r.Position)
            .Select(ToResponse)
            .ToListAsync();
        return Ok(list);
    }

    private IQueryable<ResultResponse> QueryResults() => _db.Results.AsNoTracking().Select(ToResponse);

    private static readonly System.Linq.Expressions.Expression<Func<Result, ResultResponse>> ToResponse = r => new ResultResponse
    {
        ResultID = r.ResultID,
        EnrolmentID = r.EnrolmentID,
        EventID = r.Enrolment.Category.EventID,
        EventName = r.Enrolment.Category.Event.Name,
        EventDate = r.Enrolment.Category.Event.EventDate,
        CategoryName = r.Enrolment.Category.Name,
        DistanceKm = r.Enrolment.Category.DistanceKm,
        ParticipantName = r.Enrolment.Participant.Name,
        FinishTime = r.FinishTime,
        Position = r.Position,
        TotalFinishers = r.Enrolment.Category.Enrolments.Count(x => x.Result != null && x.Result.FinishTime != null)
    };
}
