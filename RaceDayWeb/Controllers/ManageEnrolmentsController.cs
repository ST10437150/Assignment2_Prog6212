using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

[SessionAuthorize(Role = "Organiser")]
public class ManageEnrolmentsController : AppController
{
    private readonly ApiClient _api;

    public ManageEnrolmentsController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{eventId}");
        if (ev.Unauthorized) return SessionExpired();
        if (!ev.Success || ev.Data is null)
        {
            Failure(ev.Error);
            return RedirectToAction("Index", "ManageEvents");
        }

        var list = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
        if (list.Unauthorized) return SessionExpired();
        if (!list.Success)
        {
            Failure(list.Error);
            return RedirectToAction("Index", "ManageEvents");
        }

        return View(new EventEnrolmentsViewModel { Event = ev.Data, Enrolments = list.Data ?? new() });
    }

    [HttpPost]
    public async Task<IActionResult> SetStatus(int eventId, int id, string status)
    {
        var result = await _api.PutAsync<EnrolmentDto>($"api/enrolments/{id}/status", new { Status = status });
        if (result.Unauthorized) return SessionExpired();

        if (result.Success) Success($"Enrolment marked as {status}.");
        else Failure(result.Error);
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
