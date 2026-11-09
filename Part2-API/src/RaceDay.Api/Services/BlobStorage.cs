using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public interface IBlobStorage
{
    Task<string> UploadAsync(IFormFile file, string container, string namePrefix);
    Task<BlobReadResult?> DownloadAsync(string container, string blobUri);
}
public record BlobReadResult(Stream Content, string ContentType);
public sealed class BlobStorage : IBlobStorage
{
    private readonly BlobServiceClient _client;
    public BlobStorage(IConfiguration config)
    {
        var connection = config["BlobStorage:ConnectionString"] ?? "UseDevelopmentStorage=true";
        _client = new BlobServiceClient(connection);
    }

    public async Task<string> UploadAsync(IFormFile file, string container, string prefix)
    {
        var c = _client.GetBlobContainerClient(container);
        await c.CreateIfNotExistsAsync(PublicAccessType.None);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blob = c.GetBlobClient($"{prefix}-{Guid.NewGuid():N}{ext}");
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(
            stream, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType } });
        return blob.Uri.ToString();
    }

    public async Task<BlobReadResult?> DownloadAsync(string container, string blobUri)
    {
        var uri = new Uri(blobUri);
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        var containerIndex = Array.IndexOf(segments, container);
        if (containerIndex < 0 || containerIndex >= segments.Length - 1)
            return null;
        var blobName = string.Join("/", segments.Skip(containerIndex + 1));
        var blob = _client.GetBlobContainerClient(container).GetBlobClient(blobName);
        if (!(await blob.ExistsAsync()).Value)
            return null;
        var response = await blob.DownloadStreamingAsync();
        return new BlobReadResult(response.Value.Content,
                                  response.Value.Details.ContentType ?? "application/octet-stream");
    }
}
