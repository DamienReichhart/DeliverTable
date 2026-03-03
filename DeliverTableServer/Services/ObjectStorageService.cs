using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using DeliverTableServer.Configuration;
using DeliverTableServer.Services.Interfaces;

namespace DeliverTableServer.Services;

/// <inheritdoc />
public sealed class ObjectStorageService(IAmazonS3 s3Client, ObjectStorageConfig config) : IObjectStorageService
{
    private readonly IAmazonS3 _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
    private readonly ObjectStorageConfig _config = config ?? throw new ArgumentNullException(nameof(config));

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

            var contentLength = response.ContentLength > 0 ? response.ContentLength : (long?)null;

            return new ObjectStorageResult(response.ResponseStream, contentType, contentLength);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
