using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayWeb.Infrastructure;

/// <summary>
/// Protects a controller or action using the session. Unauthenticated users are sent to the
/// login page; users with the wrong role are sent to the "access denied" page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>Optional required role: "Organiser" or "Participant". Leave empty for any signed-in user.</summary>
    public string? Role { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (!session.IsSignedIn())
        {
            session.Clear();
            var request = context.HttpContext.Request;
            var returnUrl = request.Method == "GET" ? request.Path + request.QueryString : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (!string.IsNullOrEmpty(Role) && session.GetRole() != Role)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
