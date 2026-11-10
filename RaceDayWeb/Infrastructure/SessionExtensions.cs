using RaceDayWeb.Models;

namespace RaceDayWeb.Infrastructure;

/// <summary>Reads and writes the signed-in user's details in the session.</summary>
public static class SessionExtensions
{
    private const string TokenKey = "token";
    private const string ExpiresKey = "expires";
    private const string UserIdKey = "userId";
    private const string NameKey = "name";
    private const string RoleKey = "role";
    private const string PictureKey = "picture";

    public static void SignIn(this ISession session, LoginResultDto login)
    {
        session.Clear();
        session.SetString(TokenKey, login.Token);
        session.SetString(ExpiresKey, login.ExpiresAtUtc.ToUniversalTime().Ticks.ToString());
        session.SetInt32(UserIdKey, login.User.UserID);
        session.SetString(NameKey, login.User.Name);
        session.SetString(RoleKey, login.User.Role);
        session.SetString(PictureKey, login.User.ProfilePictureUrl ?? "");
    }

    public static bool IsSignedIn(this ISession session)
    {
        var token = session.GetString(TokenKey);
        var expires = session.GetString(ExpiresKey);
        return !string.IsNullOrEmpty(token)
            && long.TryParse(expires, out var ticks)
            && new DateTime(ticks, DateTimeKind.Utc) > DateTime.UtcNow;
    }

    public static string? GetToken(this ISession session) => session.GetString(TokenKey);
    public static int? GetUserId(this ISession session) => session.GetInt32(UserIdKey);
    public static string? GetUserName(this ISession session) => session.GetString(NameKey);
    public static string? GetRole(this ISession session) => session.GetString(RoleKey);

    public static string? GetPicture(this ISession session)
    {
        var value = session.GetString(PictureKey);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static bool IsOrganiser(this ISession session) => session.IsSignedIn() && session.GetRole() == "Organiser";
    public static bool IsParticipant(this ISession session) => session.IsSignedIn() && session.GetRole() == "Participant";

    public static void SetProfile(this ISession session, string name, string? picture)
    {
        session.SetString(NameKey, name);
        session.SetString(PictureKey, picture ?? "");
    }
}
