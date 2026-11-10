using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;

namespace RaceDayWeb.Controllers;

/// <summary>Shared helpers for all controllers.</summary>
public abstract class AppController : Controller
{
    protected void Success(string message) => TempData["Success"] = message;
    protected void Failure(string? message) => TempData["Error"] = message ?? "Something went wrong. Please try again.";

    /// <summary>Called when the API says the token is no longer valid: clears the session and goes to login.</summary>
    protected IActionResult SessionExpired()
    {
        HttpContext.Session.Clear();
        Failure("Your session has expired. Please log in again.");
        return RedirectToAction("Login", "Account");
    }

    protected int CurrentUserId => HttpContext.Session.GetUserId() ?? 0;
}
