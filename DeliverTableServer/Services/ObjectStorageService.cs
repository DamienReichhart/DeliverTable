using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using DeliverTableServer.Configuration;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using Microsoft.Extensions.Logging;

namespace DeliverTableServer.Services;

/// <inheritdoc />
public sealed class ObjectStorageService(
    IAmazonS3 s3Client,
    ObjectStorageConfig config,
    ILogger<ObjectStorageService> logger
) : IObjectStorageService
{
    private static readonly HashSet<string> _allowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private readonly IAmazonS3 _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
    private readonly ObjectStorageConfig _config = config ?? throw new ArgumentNullException(nameof(config));
    private readonly ILogger<ObjectStorageService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<ObjectStorageResult?> GetObjectAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
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
    public async Task<string?> UploadAsync(IFormFile file, string folder = "dish", int? identifier = null, CancellationToken cancellationToken = default)
    {
        if (identifier == null)
        {
            return null;
        }
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"File extension {extension} is not allowed. Allowed extensions: {string.Join(", ", _allowedExtensions)}");
        }

        string imageKey = $"{identifier}{UploadLimits.DefaultImageExtension}";
        string key = string.IsNullOrEmpty(folder) ? imageKey : $"{folder.Trim('/')}/{imageKey}";

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

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
            _logger.LogError(ex,
                "S3 upload failed — ErrorCode: {ErrorCode}, StatusCode: {StatusCode}, RequestId: {RequestId}",
                ex.ErrorCode, ex.StatusCode, ex.RequestId);
            throw;
        }
    }

    public async Task<string> UploadAsync(byte[] content, string contentType, string folder, string fileName, CancellationToken cancellationToken = default)
    {
        string key = $"{folder.Trim('/')}/{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = _config.BucketName,
            Key = key,
            ContentType = contentType,
            InputStream = new MemoryStream(content),
            UseChunkEncoding = false,
            AutoCloseStream = true
        };

        try
        {
            var response = await _s3Client.PutObjectAsync(request, cancellationToken);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                return key;
            }

            throw new Exception($"Upload failed with status code: {response.HttpStatusCode}");
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex,
                "S3 upload failed — ErrorCode: {ErrorCode}, StatusCode: {StatusCode}, RequestId: {RequestId}",
                ex.ErrorCode, ex.StatusCode, ex.RequestId);
            throw;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3Client.DeleteObjectAsync(_config.BucketName, key + UploadLimits.DefaultImageExtension, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode is not HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Failed to delete S3 object with key: {Key}", key);
        }
    }
}
