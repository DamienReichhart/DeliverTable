
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableServer.Middleware
{
    public class NotFoundMiddleware(RequestDelegate next, ILogger<NotFoundMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<NotFoundMiddleware> _logger = logger;

        public async Task Invoke(HttpContext context)
        {
            await _next(context);
            if (context.Response.StatusCode == 404)
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new ErrorResponse
                    {
                        Error = "Path not found",
                        Status = 404
                    });
                }
            }
        }

    }
}