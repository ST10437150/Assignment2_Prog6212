using System.Security.Claims;

namespace RaceDayApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Returns the id of the logged-in user from the JWT, or null if there is none.</summary>
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("sub")?.Value
        ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(value, out var id) ? id : null;
    }
}