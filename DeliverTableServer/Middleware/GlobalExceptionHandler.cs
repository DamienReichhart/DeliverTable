using DeliverTableSharedLibrary.Dtos;
using Microsoft.AspNetCore.Diagnostics;

namespace DeliverTableServer.Middleware
{
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger):IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger = logger;

        public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Une erreur non gérée est survenue : {Message}", exception.Message);

        var problemDetails = new ErrorResponse
        {
            Status = StatusCodes.Status500InternalServerError,
            Error = exception.Message
        };

        if (exception is KeyNotFoundException)
        {
            problemDetails.Status = StatusCodes.Status404NotFound;
            problemDetails.Error = "Ressource non trouvée";
        }

        httpContext.Response.StatusCode = problemDetails.Status;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
        }
    }
}