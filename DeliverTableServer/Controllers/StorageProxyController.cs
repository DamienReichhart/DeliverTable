using System.Collections.Frozen;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace DeliverTableServer.Controllers;

/// <summary>
///     Proxies requests for static assets (images, documents) from S3-compatible object storage (Garage).
///     Objects are streamed directly to the client without buffering the entire body in memory.
/// </summary>
[ApiController]
public class StorageProxyController(IObjectStorageService objectStorage) : ControllerBase
{
    private const string SafeFallbackContentType = "application/octet-stream";

    /// <summary>
    ///     Allowed MIME types per storage prefix.
    ///     Requests whose stored Content-Type falls outside the allowlist are
    ///     forced to <c>application/octet-stream</c> with a download disposition.
    /// </summary>
    private static readonly FrozenDictionary<string, FrozenSet<string>> AllowedContentTypes =
        new Dictionary<string, FrozenSet<string>>(StringComparer.Ordinal)
        {
            ["images"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg", "image/png", "image/gif",
                "image/webp", "image/svg+xml", "image/avif"
            }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),

            ["documents"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "application/pdf"
            }.ToFrozenSet(StringComparer.OrdinalIgnoreCase)
        }.ToFrozenDictionary(StringComparer.Ordinal);

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

        Response.Headers["X-Content-Type-Options"] = "nosniff";

        if (result.ContentLength.HasValue)
            Response.ContentLength = result.ContentLength.Value;

        var contentType = ResolveContentType(prefix, result.ContentType, out var forceDownload);

        if (forceDownload)
        {
            var fileName = segments[^1];
            Response.Headers[HeaderNames.ContentDisposition] =
                new ContentDispositionHeaderValue("attachment") { FileName = fileName }.ToString();
        }

        return File(result.Content, contentType);
    }

    /// <summary>
    ///     Validates the stored MIME type against the prefix's allowlist.
    ///     Returns the original type when allowed, otherwise falls back to a
    ///     safe binary type and signals forced download.
    /// </summary>
    private static string ResolveContentType(
        string prefix, string storedContentType, out bool forceDownload)
    {
        if (AllowedContentTypes.TryGetValue(prefix, out var allowed)
            && allowed.Contains(storedContentType))
        {
            forceDownload = false;
            return storedContentType;
        }

        forceDownload = true;
        return SafeFallbackContentType;
    }
}
