using System.Net;
using System.Reflection.Metadata.Ecma335;
using Amazon.S3;
using Amazon.S3.Model;
using DeliverTableServer.Configuration;
using DeliverTableServer.Services.Interfaces;

namespace DeliverTableServer.Services;

/// <inheritdoc />
public sealed class ObjectStorageService(IAmazonS3 s3Client, ObjectStorageConfig config) : IObjectStorageService
{
    private static readonly HashSet<string> _allowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private readonly IAmazonS3 _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
    private readonly ObjectStorageConfig _config = config ?? throw new ArgumentNullException(nameof(config));

    /// <inheritdoc />
    public async Task<ObjectStorageResult?> GetObjectAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            key = key.Substring(key.IndexOf("/") + 1);

            var request = new GetObjectRequest
            {
                BucketName = _config.BucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectAsync(request, cancellationToken);

            var contentType = !string.IsNullOrWhiteSpace(response.Headers.ContentType)
                ? response.Headers.ContentType
                : "application/octet-stream";

            long? contentLength = response.ContentLength >= 0 ? response.ContentLength : null;

            return new ObjectStorageResult(response.ResponseStream, contentType, contentLength);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }
    }
    public async Task<string?> UploadAsync(IFormFile file, string folder = "dish", CancellationToken cancellationToken = default)
    {
        // Validate file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"File extension {extension} is not allowed. Allowed extensions: {string.Join(", ", _allowedExtensions)}");
        }

        string imageKey = $"{Guid.NewGuid()}{extension}";
        string key = string.IsNullOrEmpty(folder) ? imageKey : $"{folder.Trim('/')}/{imageKey}";

        // CRITICAL FIX FOR GARAGE: Copy to MemoryStream first
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0; // Reset stream position

        var request = new PutObjectRequest
        {
            BucketName = _config.BucketName,
            Key = key,
            InputStream = memoryStream,
            ContentType = file.ContentType ?? "application/octet-stream",

            UseChunkEncoding = false,

            AutoCloseStream = true
        };

        try
        {
            var response = await _s3Client.PutObjectAsync(request, cancellationToken);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                return imageKey;
            }

            throw new Exception($"Upload failed with status code: {response.HttpStatusCode}");
        }
        catch (AmazonS3Exception ex)
        {
            // Log the exception details
            Console.WriteLine($"S3 Upload Error: {ex.Message}");
            Console.WriteLine($"Error Code: {ex.ErrorCode}");
            Console.WriteLine($"Status Code: {ex.StatusCode}");
            Console.WriteLine($"Request ID: {ex.RequestId}");
            throw; // Re-throw or handle as needed
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Deleting object with key: {key}");
        await _s3Client.DeleteObjectAsync(_config.BucketName, key, cancellationToken);
    }
}
