using DeliverTableServer.Constants;
using DeliverTableSharedLibrary.Dtos;
using Microsoft.AspNetCore.Diagnostics;

namespace DeliverTableServer.Middleware;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment
) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, message) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, ErrorMessages.ResourceNotFound),
            _ => (StatusCodes.Status500InternalServerError,
                  _environment.IsDevelopment() ? exception.Message : ErrorMessages.InternalServerError)
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            new ErrorResponse { Status = statusCode, Error = message },
            cancellationToken);

        return true;
    }
}
