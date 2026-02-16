using System.Text;
using DeliverTableServer.Configuration;
using DeliverTableServer.Extensions;
using DotNetEnv;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Identity
builder.Services.AddIdentityParams();

// JWT
builder.Services.AddJwtAuthentication();
builder.Services.AddAuthorization();

// Retirer les objets avec tous les détails des réponses si hors développement
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development";
});

builder.Services.AddDeliverTableServices();
builder.Services.AddDeliverTableOpenApi();
builder.Services.AddDeliverTableDatabase();
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

var openApiOptions = app.Services.GetRequiredService<IOptions<OpenApiOptions>>().Value;
var enableOpenApi = app.Environment.IsDevelopment() || openApiOptions.EnableDocumentation;

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();