using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

[SessionAuthorize(Role = "Organiser")]
public class ManageCategoriesController : AppController
{
    private readonly ApiClient _api;

    public ManageCategoriesController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index(int eventId)
    {
        var result = await _api.GetAsync<EventDto>($"api/events/{eventId}");
        if (result.Unauthorized) return SessionExpired();
        if (!result.Success || result.Data is null || result.Data.OrganiserID != CurrentUserId)
        {
            Failure(result.Success ? "You can only manage your own events." : result.Error);
            return RedirectToAction("Index", "ManageEvents");
        }

        ViewBag.NewCategory = new CategoryFormModel { EventID = eventId };
        return View(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CategoryFormModel model)
    {
        if (!ModelState.IsValid)
        {
            Failure(ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage);
            return RedirectToAction(nameof(Index), new { eventId = model.EventID });
        }

        var result = await _api.PostAsync<CategoryDto>($"api/events/{model.EventID}/categories",
            new { model.Name, model.DistanceKm });
        if (result.Unauthorized) return SessionExpired();

        if (result.Success) Success("Category added.");
        else Failure(result.Error);
        return RedirectToAction(nameof(Index), new { eventId = model.EventID });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        var result = await _api.GetAsync<EventDto>($"api/events/{eventId}");
        if (result.Unauthorized) return SessionExpired();

        var category = result.Data?.Categories.FirstOrDefault(c => c.CategoryID == id);
        if (!result.Success || result.Data is null || category is null || result.Data.OrganiserID != CurrentUserId)
        {
            Failure(result.Success ? "Category not found." : result.Error);
            return RedirectToAction("Index", "ManageEvents");
        }

        ViewBag.EventName = result.Data.Name;
        return View(new CategoryFormModel
        {
            CategoryID = category.CategoryID,
            EventID = eventId,
            Name = category.Name,
            DistanceKm = category.DistanceKm
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CategoryFormModel model)
    {
        model.CategoryID = id;
        if (!ModelState.IsValid) return View(model);

        var result = await _api.PutAsync<CategoryDto>($"api/categories/{id}", new { model.Name, model.DistanceKm });
        if (result.Unauthorized) return SessionExpired();
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "We couldn't save the category.");
            return View(model);
        }

        Success("Category updated.");
        return RedirectToAction(nameof(Index), new { eventId = model.EventID });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var result = await _api.DeleteAsync($"api/categories/{id}");
        if (result.Unauthorized) return SessionExpired();

        if (result.Success) Success("Category removed.");
        else Failure(result.Error);
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
