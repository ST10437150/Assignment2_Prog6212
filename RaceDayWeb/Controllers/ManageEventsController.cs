using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

[SessionAuthorize(Role = "Organiser")]
public class ManageEventsController : AppController
{
    private readonly ApiClient _api;

    public ManageEventsController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<EventDto>>("api/events/mine");
        if (result.Unauthorized) return SessionExpired();
        if (!result.Success) Failure(result.Error);
        return View(result.Data ?? new List<EventDto>());
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new EventFormModel { EventDate = DateTime.Today.AddDays(30) });

    [HttpPost]
    public async Task<IActionResult> Create(EventFormModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var result = await _api.PostAsync<EventDto>("api/events", ToRequest(model, includeDistance: true));
        if (result.Unauthorized) return SessionExpired();
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError("", result.Error ?? "We couldn't create the event.");
            return View("Form", model);
        }

        if (model.Banner is { Length: > 0 })
        {
            var upload = await _api.UploadAsync<EventDto>($"api/events/{result.Data.EventID}/banner", model.Banner);
            if (!upload.Success)
            {
                Failure($"Event created, but the banner could not be uploaded: {upload.Error}");
                return RedirectToAction(nameof(Edit), new { id = result.Data.EventID });
            }
        }

        Success("Event created. You can now add more categories.");
        return RedirectToAction("Index", "ManageCategories", new { eventId = result.Data.EventID });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _api.GetAsync<EventDto>($"api/events/{id}");
        if (result.Unauthorized) return SessionExpired();
        if (!result.Success || result.Data is null)
        {
            Failure(result.Error);
            return RedirectToAction(nameof(Index));
        }

        if (result.Data.OrganiserID != CurrentUserId)
        {
            Failure("You can only edit your own events.");
            return RedirectToAction(nameof(Index));
        }

        var e = result.Data;
        return View("Form", new EventFormModel
        {
            EventID = e.EventID,
            Name = e.Name,
            Description = e.RouteDescription ?? "",
            EventDate = e.EventDate.ToDateTime(TimeOnly.MinValue),
            Location = e.Location,
            EventType = e.EventType,
            BannerUrl = e.BannerUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EventFormModel model)
    {
        model.EventID = id;
        if (!ModelState.IsValid) return View("Form", model);

        var result = await _api.PutAsync<EventDto>($"api/events/{id}", ToRequest(model, includeDistance: false));
        if (result.Unauthorized) return SessionExpired();
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError("", result.Error ?? "We couldn't save the event.");
            model.BannerUrl = result.Data?.BannerUrl;
            return View("Form", model);
        }

        if (model.Banner is { Length: > 0 })
        {
            var upload = await _api.UploadAsync<EventDto>($"api/events/{id}/banner", model.Banner);
            if (!upload.Success)
            {
                Failure($"Event saved, but the banner could not be uploaded: {upload.Error}");
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        Success("Event updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _api.DeleteAsync($"api/events/{id}");
        if (result.Unauthorized) return SessionExpired();

        if (result.Success) Success("Event deleted.");
        else Failure(result.Error);
        return RedirectToAction(nameof(Index));
    }

    private static object ToRequest(EventFormModel m, bool includeDistance) => new
    {
        m.Name,
        EventDate = DateOnly.FromDateTime(m.EventDate!.Value),
        m.Location,
        RouteDescription = m.Description,
        m.EventType,
        DistanceKm = includeDistance ? m.DistanceKm : null
    };
}
