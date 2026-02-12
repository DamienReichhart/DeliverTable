using DeliverTableServer.Configuration;
using DeliverTableServer.Extensions;
using Microsoft.Extensions.Options;

EnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDeliverTableServices();
builder.Services.AddDeliverTableOpenApi();
builder.Services.Configure<OpenApiOptions>(builder.Configuration.GetSection(OpenApiOptions.SectionName));
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// OpenAPI document and Swagger UI: enabled in Development or when explicitly enabled via configuration.
// Disabled by default in Production to limit information disclosure (see Microsoft security guidance).
var openApiOptions = app.Services.GetRequiredService<IOptions<OpenApiOptions>>().Value;
bool enableOpenApi = app.Environment.IsDevelopment()
    || openApiOptions.EnableDocumentation;

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

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
