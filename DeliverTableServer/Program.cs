using DeliverTableServer.Configuration;
using DeliverTableServer.Extensions;
using DeliverTableServer.Middleware;

EnvLoader.Load();
var env = AppEnvironment.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(env);
builder.Services.AddSingleton(env.Jwt);
builder.Services.AddSingleton(env.ObjectStorage);

builder.Services.AddIdentityParams();
builder.Services.AddJwtAuthentication(env.Jwt);
builder.Services.AddObjectStorage(env.ObjectStorage);
builder.Services.AddAuthorization();

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    if (!builder.Environment.IsDevelopment())
    {
        options.InvalidModelStateResponseFactory = _ =>
            new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { error = "Invalid request." });
    }
});


builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddDeliverTableServices();
builder.Services.AddDeliverTableOpenApi();
builder.Services.AddDeliverTableDatabase(env.DatabaseConnectionString);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment() || env.CorsAllowedOrigins.Length == 0)
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(env.CorsAllowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// app.UseMiddleware<NotFoundMiddleware>();

var enableOpenApi = app.Environment.IsDevelopment() || env.OpenApiEnableDocumentation;

if (enableOpenApi)
{
    app.MapOpenApi(OpenApiConstants.OpenApiRouteTemplate);
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(OpenApiConstants.OpenApiJsonPath, "DeliverTable API v1");
        options.RoutePrefix = OpenApiConstants.DocumentationPath;
        options.DocumentTitle = "DeliverTable API Documentation";
        options.DisplayRequestDuration();
    });
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();