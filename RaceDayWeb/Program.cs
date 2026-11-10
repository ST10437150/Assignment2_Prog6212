using RaceDayWeb.Infrastructure;
using RaceDayWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- MVC (all POST forms are protected against CSRF) ----------
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));

// ---------- Session: keeps the JWT, user id, name and role between requests ----------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(60);
});
builder.Services.AddHttpContextAccessor();

// ---------- The MVC app only ever talks to the RaceDay API ----------
var apiBase = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl is missing from the configuration.");

builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseMiddleware<NoStoreMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
