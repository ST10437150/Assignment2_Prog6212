using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

public class EventsController : AppController
{
    private readonly ApiClient _api;

    public EventsController(ApiClient api) => _api = api;

    /// <summary>Full event information and available categories. Public.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var result = await _api.GetAsync<EventDto>($"api/events/{id}");
        if (!result.Success || result.Data is null)
        {
            Failure(result.Error);
            return RedirectToAction("Index", "Home");
        }

        var model = new EventDetailsViewModel { Event = result.Data };

        if (HttpContext.Session.IsParticipant())
        {
            var mine = await _api.GetAsync<List<EnrolmentDto>>($"api/users/{CurrentUserId}/enrolments");
            model.MyEnrolment = mine.Data?.FirstOrDefault(e => e.EventID == id && e.Status != "Cancelled");
        }

        return View(model);
    }

    /// <summary>Enrols the logged-in participant in a category.</summary>
    [SessionAuthorize(Role = "Participant")]
    [HttpPost]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var result = await _api.PostAsync<EnrolmentDto>($"api/categories/{categoryId}/enrol");
        if (result.Unauthorized) return SessionExpired();

        if (result.Success)
            Success("You're in! Your enrolment has been recorded and is awaiting confirmation.");
        else
            Failure(result.Error);

        return RedirectToAction(nameof(Details), new { id = eventId });
    }
}
