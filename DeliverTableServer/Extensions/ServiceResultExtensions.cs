using DeliverTableServer.Common;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Extensions;

/// <summary>
///     Maps <see cref="ServiceResult"/> and <see cref="ServiceResult{T}"/> to <see cref="IActionResult"/>
///     so controllers stay thin and consistent.
/// </summary>
public static class ServiceResultExtensions
{
    /// <summary>
    ///     Returns 200 OK with the value on success, or the appropriate error status code.
    /// </summary>
    public static IActionResult ToOkResult<T>(this ServiceResult<T> result)
    {
        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ToErrorResult(result.Error!);
    }

    /// <summary>
    ///     Returns 204 No Content on success, or the appropriate error status code.
    /// </summary>
    public static IActionResult ToNoContentResult(this ServiceResult result)
    {
        return result.IsSuccess
            ? new NoContentResult()
            : ToErrorResult(result.Error!);
    }

    /// <summary>
    ///     Returns the appropriate error status code with a consistent error body.
    ///     Use when the success path requires a different response shape (e.g. CreatedAtAction).
    /// </summary>
    public static IActionResult ToErrorResult(this ServiceError error)
    {
        return new ObjectResult(new { Error = error.Message }) { StatusCode = error.StatusCode };
    }
}
