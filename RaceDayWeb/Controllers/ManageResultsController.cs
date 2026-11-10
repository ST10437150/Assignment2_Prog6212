using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

[SessionAuthorize(Role = "Organiser")]
public class ManageResultsController : AppController
{
    private readonly ApiClient _api;

    public ManageResultsController(ApiClient api) => _api = api;

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

        return View(new EventEnrolmentsViewModel
        {
            Event = ev.Data,
            Enrolments = (list.Data ?? new()).Where(e => e.Status == "Confirmed").ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save(ResultFormModel model)
    {
        if (!ModelState.IsValid)
        {
            Failure(ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage);
            return RedirectToAction(nameof(Index), new { eventId = model.EventId });
        }

        var result = await _api.PostAsync<ResultDto>($"api/enrolments/{model.EnrolmentId}/results",
            new { FinishTime = TimeSpan.Parse(model.FinishTime), model.Position });
        if (result.Unauthorized) return SessionExpired();

        if (result.Success) Success("Result saved.");
        else Failure(result.Error);
        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }
}
