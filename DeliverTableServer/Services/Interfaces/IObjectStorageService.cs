namespace DeliverTableServer.Services.Interfaces;

/// <summary>
///     Abstraction over S3-compatible object storage for retrieving stored objects.
/// </summary>
public interface IObjectStorageService
{
    /// <summary>
    ///     Retrieves an object by its storage key.
    /// </summary>
    /// <param name="key">The full object key (e.g. "images/photo.jpg").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The object result containing stream and metadata, or null if not found.</returns>
    Task<ObjectStorageResult?> GetObjectAsync(string key, CancellationToken cancellationToken = default);

    Task<string?> UploadAsync(IFormFile file, string folder = "dishes", int? identifier = null, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
///     Represents a retrieved object from storage with its content stream and metadata.
/// </summary>
/// <param name="Content">The readable stream of the object body. Caller is responsible for disposal.</param>
/// <param name="ContentType">MIME type of the object (e.g. "image/png").</param>
/// <param name="ContentLength">Size in bytes, if known.</param>
public sealed record ObjectStorageResult(
    Stream Content,
    string ContentType,
    long? ContentLength);
