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
    ///     Returns 200 OK with a message on success, or the appropriate error status code.
    ///     Use for void operations that should return a success message (e.g. password change).
    /// </summary>
    public static IActionResult ToOkMessageResult(this ServiceResult result, string message)
    {
        return result.IsSuccess
            ? new OkObjectResult(new { Message = message })
            : ToErrorResult(result.Error!);
    }

    /// <summary>
    ///     Returns 201 Created with the value on success (pointing at <paramref name="actionName"/>),
    ///     or the appropriate error status code.
    /// </summary>
    public static IActionResult ToCreatedResult<T>(
        this ServiceResult<T> result,
        string actionName,
        Func<T, object> routeValues
    )
    {
        return result.IsSuccess
            ? new CreatedAtActionResult(actionName, controllerName: null, routeValues(result.Value!), result.Value)
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
