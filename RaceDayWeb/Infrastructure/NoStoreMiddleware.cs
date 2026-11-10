namespace RaceDayWeb.Infrastructure;

/// <summary>
/// Marks every dynamic page as "do not cache", so the browser's Back button cannot show
/// protected pages after the user has logged out. (Static files are served earlier in the
/// pipeline and stay cacheable.)
/// </summary>
public class NoStoreMiddleware
{
    private readonly RequestDelegate _next;

    public NoStoreMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = "no-store, no-cache";
            context.Response.Headers.Pragma = "no-cache";
            return Task.CompletedTask;
        });

        return _next(context);
    }
}
