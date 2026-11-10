using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RaceDayWeb.Infrastructure;

namespace RaceDayWeb.Services;

/// <summary>The outcome of an API call: either data, or a friendly error message.</summary>
public class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public HttpStatusCode? StatusCode { get; init; }

    public bool Unauthorized => StatusCode == HttpStatusCode.Unauthorized;
}

/// <summary>
/// The single place the MVC app talks to the RaceDay API. It attaches the JWT stored in the
/// session and turns failures into short, friendly messages (never raw errors).
/// </summary>
public class ApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _context;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient http, IHttpContextAccessor context, ILogger<ApiClient> logger)
    {
        _http = http;
        _context = context;
        _logger = logger;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
        SendAsync<T>(HttpMethod.Post, url, body is null ? null : JsonBody(body));

    public Task<ApiResult<T>> PutAsync<T>(string url, object body) =>
        SendAsync<T>(HttpMethod.Put, url, JsonBody(body));

    public Task<ApiResult<bool>> DeleteAsync(string url) => SendAsync<bool>(HttpMethod.Delete, url);

    /// <summary>Forwards an uploaded file to the API (which stores it in Azure Blob Storage).</summary>
    public async Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var form = new MultipartFormDataContent();
        var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        form.Add(content, "file", file.FileName);
        return await SendAsync<T>(HttpMethod.Post, url, form);
    }

    private static StringContent JsonBody(object body) =>
        new(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };

            var token = _context.HttpContext?.Session.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await _http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResult<T>
                {
                    Success = false,
                    StatusCode = response.StatusCode,
                    Error = ExtractMessage(response.StatusCode, text)
                };
            }

            T? data = default;
            if (typeof(T) == typeof(bool))
            {
                data = (T)(object)true;
            }
            else if (!string.IsNullOrWhiteSpace(text))
            {
                data = JsonSerializer.Deserialize<T>(text, Json);
            }

            return new ApiResult<T> { Success = true, Data = data, StatusCode = response.StatusCode };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "API call {Method} {Url} failed.", method, url);
            return new ApiResult<T>
            {
                Success = false,
                Error = "We couldn't reach the RaceDay service. Please try again in a moment."
            };
        }
    }

    /// <summary>Turns an API error response into a short message that is safe to show to a user.</summary>
    private static string ExtractMessage(HttpStatusCode status, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                {
                    return message.GetString()!;
                }

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in errors.EnumerateObject())
                    {
                        foreach (var item in field.Value.EnumerateArray())
                        {
                            return item.GetString() ?? "Please check the details you entered.";
                        }
                    }
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            // fall through to the generic messages below
        }

        return status switch
        {
            HttpStatusCode.BadRequest => "Please check the details you entered and try again.",
            HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
            HttpStatusCode.Conflict => "That conflicts with existing data.",
            HttpStatusCode.ServiceUnavailable => "That feature is temporarily unavailable. Please try again later.",
            _ => "Something went wrong on our side. Please try again."
        };
    }
}
