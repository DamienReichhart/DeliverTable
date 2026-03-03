using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

/// <summary>
///     Proxies requests for static assets (images, documents) from S3-compatible object storage (Garage).
///     Objects are streamed directly to the client without buffering the entire body in memory.
/// </summary>
[ApiController]
public class StorageProxyController(IObjectStorageService objectStorage) : ControllerBase
{
    private readonly IObjectStorageService _objectStorage =
        objectStorage ?? throw new ArgumentNullException(nameof(objectStorage));

    /// <summary>
    ///     Serves an image from object storage.
    /// </summary>
    /// <param name="path">The path within the images prefix (e.g. "restaurants/logo.png").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet(ApiRoutes.StorageImages)]
    [ResponseCache(Duration = 86_400, Location = ResponseCacheLocation.Any)]
    public Task<IActionResult> GetImage(string path, CancellationToken cancellationToken)
    {
        return StreamObjectAsync("images", path, cancellationToken);
    }

    /// <summary>
    ///     Serves a document from object storage.
    /// </summary>
    /// <param name="path">The path within the documents prefix (e.g. "menus/spring-2026.pdf").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet(ApiRoutes.StorageDocuments)]
    [ResponseCache(Duration = 86_400, Location = ResponseCacheLocation.Any)]
    public Task<IActionResult> GetDocument(string path, CancellationToken cancellationToken)
    {
        return StreamObjectAsync("documents", path, cancellationToken);
    }

    private async Task<IActionResult> StreamObjectAsync(
        string prefix, string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains('\\')){
            return BadRequest();
        }

        var segments = path.Trim('/').Split('/');

        if (segments.Any(s => s is "" or "." or "..")){
            return BadRequest();
        }

        var key = $"{prefix}/{string.Join('/', segments)}";
        var result = await _objectStorage.GetObjectAsync(key, cancellationToken);

        if (result is null)
            return NotFound();

        if (result.ContentLength.HasValue)
            Response.ContentLength = result.ContentLength.Value;

        return File(result.Content, result.ContentType);
    }
}
