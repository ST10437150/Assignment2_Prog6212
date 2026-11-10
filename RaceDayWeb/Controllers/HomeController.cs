using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

public class HomeController : AppController
{
    private readonly ApiClient _api;

    public HomeController(ApiClient api) => _api = api;

    /// <summary>Upcoming events with filtering. Public.</summary>
    public async Task<IActionResult> Index([FromQuery] EventFilterModel filter)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.Search)) query.Add($"search={Uri.EscapeDataString(filter.Search.Trim())}");
        if (!string.IsNullOrWhiteSpace(filter.Type)) query.Add($"type={Uri.EscapeDataString(filter.Type)}");
        if (filter.From.HasValue) query.Add($"from={filter.From:yyyy-MM-dd}");
        if (filter.To.HasValue) query.Add($"to={filter.To:yyyy-MM-dd}");

        var url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var result = await _api.GetAsync<List<EventDto>>(url);

        if (!result.Success)
        {
            Failure(result.Error);
        }

        return View(new EventListViewModel { Filter = filter, Events = result.Data ?? new() });
    }

    [IgnoreAntiforgeryToken]
    [Route("Home/Error")]
    public IActionResult Error() =>
        View("Error", new ErrorViewModel());

    [IgnoreAntiforgeryToken]
    [Route("Home/StatusCode")]
    public IActionResult StatusCodePage(int code = 0)
    {
        var model = code == 404
            ? new ErrorViewModel { Title = "Page not found", Message = "That page doesn't exist. Let's get you back to the start line." }
            : new ErrorViewModel();
        Response.StatusCode = code == 0 ? 500 : code;
        return View("Error", model);
    }
}
