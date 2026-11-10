using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDayApi.Services;

public interface IBlobStorageService
{
    /// <summary>Returns an error message if the file is not an acceptable image, otherwise null.</summary>
    string? ValidateImage(IFormFile? file);

    /// <summary>Uploads an image to Azure Blob Storage and returns its public URL.</summary>
    Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default);

    /// <summary>Deletes a previously uploaded blob (best effort - never throws).</summary>
    Task DeleteAsync(string? url, CancellationToken ct = default);
}

/// <summary>Raised when image storage is not configured or cannot be reached.</summary>
public class StorageUnavailableException : Exception
{
    public StorageUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>
/// All Azure Blob Storage access lives here, inside the API. The MVC application
/// never talks to Azure directly - it only sends files to the API.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp"
    };

    private readonly IConfiguration _config;
    private readonly ILogger<BlobStorageService> _logger;
    private BlobContainerClient? _container;

    public BlobStorageService(IConfiguration config, ILogger<BlobStorageService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public string? ValidateImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return "Please choose an image file to upload.";
        }

        if (file.Length > MaxBytes)
        {
            return "The image is too large. The maximum size is 5 MB.";
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedTypes.ContainsKey(ext))
        {
            return "Only JPG, PNG, GIF or WEBP images are allowed.";
        }

        return null;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        var container = await GetContainerAsync(ct);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blob = container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{ext}");

        try
        {
            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = AllowedTypes[ext] }
            }, ct);
        }
        catch (Exception ex) when (ex is Azure.RequestFailedException or HttpRequestException or AggregateException)
        {
            _logger.LogError(ex, "Blob upload failed.");
            throw new StorageUnavailableException("Image storage is currently unavailable.", ex);
        }

        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string? url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            var container = await GetContainerAsync(ct);
            var blobName = new BlobUriBuilder(new Uri(url)).BlobName;
            await container.DeleteBlobIfExistsAsync(blobName, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete blob {Url}.", url);
        }
    }

    private async Task<BlobContainerClient> GetContainerAsync(CancellationToken ct)
    {
        if (_container is not null)
        {
            return _container;
        }

        var connection = _config["AzureBlob:ConnectionString"];
        var name = _config["AzureBlob:Container"] ?? "raceday-images";

        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new StorageUnavailableException("Image storage is not configured.");
        }

        try
        {
            var client = new BlobContainerClient(connection, name);
            // Public read access on blobs so browsers can show the images directly.
            await client.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
            _container = client;
            return client;
        }
        catch (Exception ex) when (ex is Azure.RequestFailedException or HttpRequestException or AggregateException or FormatException or ArgumentException)
        {
            _logger.LogError(ex, "Could not open the blob container.");
            throw new StorageUnavailableException("Image storage is currently unavailable.", ex);
        }
    }
}
