using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

public class AccountController : AppController
{
    private readonly ApiClient _api;

    public AccountController(ApiClient api) => _api = api;

    // ---------- Login ----------
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.IsSignedIn()) return RedirectToAction(nameof(Landing));
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.PostAsync<LoginResultDto>("api/auth/login", new { model.Email, model.Password });
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError("", result.Error ?? "We couldn't log you in.");
            model.Password = "";
            return View(model);
        }

        HttpContext.Session.SignIn(result.Data);
        Success($"Welcome back, {result.Data.User.Name}!");

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }
        return RedirectToAction(nameof(Landing));
    }

    /// <summary>Sends each role to its own starting page.</summary>
    public IActionResult Landing()
    {
        if (HttpContext.Session.IsOrganiser()) return RedirectToAction("Dashboard", "Organiser");
        return RedirectToAction("Index", "Home");
    }

    // ---------- Register ----------
    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsSignedIn()) return RedirectToAction(nameof(Landing));
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.PostAsync<UserDto>("api/auth/register",
            new { model.Name, model.Email, model.Password, model.Role });

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "We couldn't create your account.");
            model.Password = model.ConfirmPassword = "";
            return View(model);
        }

        Success("Your account is ready. Please log in.");
        return RedirectToAction(nameof(Login));
    }

    // ---------- Logout ----------
    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        Success("You have been logged out.");
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    // ---------- Profile ----------
    [SessionAuthorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var result = await _api.GetAsync<UserDto>($"api/users/{CurrentUserId}");
        if (result.Unauthorized) return SessionExpired();
        if (!result.Success || result.Data is null)
        {
            Failure(result.Error);
            return RedirectToAction("Index", "Home");
        }

        return View(ToModel(result.Data));
    }

    [SessionAuthorize]
    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        model.Role = HttpContext.Session.GetRole() ?? "";
        model.ProfilePictureUrl = HttpContext.Session.GetPicture();
        if (!ModelState.IsValid) return View(model);

        var result = await _api.PutAsync<UserDto>($"api/users/{CurrentUserId}", new { model.Name, model.Email });
        if (result.Unauthorized) return SessionExpired();
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError("", result.Error ?? "We couldn't save your changes.");
            return View(model);
        }

        HttpContext.Session.SetProfile(result.Data.Name, result.Data.ProfilePictureUrl);
        Success("Your profile has been updated.");
        return RedirectToAction(nameof(Profile));
    }

    [SessionAuthorize]
    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            Failure("Please choose an image to upload.");
            return RedirectToAction(nameof(Profile));
        }

        var result = await _api.UploadAsync<UserDto>($"api/users/{CurrentUserId}/picture", picture);
        if (result.Unauthorized) return SessionExpired();
        if (!result.Success || result.Data is null)
        {
            Failure(result.Error);
            return RedirectToAction(nameof(Profile));
        }

        HttpContext.Session.SetProfile(result.Data.Name, result.Data.ProfilePictureUrl);
        Success("Profile picture updated.");
        return RedirectToAction(nameof(Profile));
    }

    private static ProfileViewModel ToModel(UserDto u) => new()
    {
        Name = u.Name,
        Email = u.Email,
        Role = u.Role,
        ProfilePictureUrl = u.ProfilePictureUrl
    };
}
