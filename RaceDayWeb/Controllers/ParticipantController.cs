using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

[SessionAuthorize(Role = "Participant")]
public class ParticipantController : AppController
{
    private readonly ApiClient _api;

    public ParticipantController(ApiClient api) => _api = api;

    public async Task<IActionResult> MyEnrolments()
    {
        var result = await _api.GetAsync<List<EnrolmentDto>>($"api/users/{CurrentUserId}/enrolments");
        if (result.Unauthorized) return SessionExpired();
        if (!result.Success) Failure(result.Error);
        return View(result.Data ?? new List<EnrolmentDto>());
    }

    [HttpPost]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var result = await _api.DeleteAsync($"api/enrolments/{id}");
        if (result.Unauthorized) return SessionExpired();

        if (result.Success) Success("Your enrolment has been cancelled.");
        else Failure(result.Error);
        return RedirectToAction(nameof(MyEnrolments));
    }

    public async Task<IActionResult> MyResults()
    {
        var result = await _api.GetAsync<List<ResultDto>>($"api/users/{CurrentUserId}/results");
        if (result.Unauthorized) return SessionExpired();
        if (!result.Success) Failure(result.Error);
        return View(result.Data ?? new List<ResultDto>());
    }
}
