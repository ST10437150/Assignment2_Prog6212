using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

[SessionAuthorize(Role = "Organiser")]
public class OrganiserController : AppController
{
    private readonly ApiClient _api;

    public OrganiserController(ApiClient api) => _api = api;

    public async Task<IActionResult> Dashboard()
    {
        var result = await _api.GetAsync<List<EventDto>>("api/events/mine");
        if (result.Unauthorized) return SessionExpired();
        if (!result.Success) Failure(result.Error);
        return View(new DashboardViewModel { Events = result.Data ?? new() });
    }
}
