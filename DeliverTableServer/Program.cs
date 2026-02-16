using System.Text;
using DeliverTableServer.Configuration;
using DeliverTableServer.Extensions;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// JWT
var jwtConfig = JwtConfig.LoadFromEnv();
builder.Services.AddSingleton(jwtConfig);
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidAudience = jwtConfig.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key))
        };
        
        // options.Events = new JwtBearerEvents
        // {
        //     OnAuthenticationFailed = context =>
        //     {
        //         Console.WriteLine($"JWT failed: {context.Exception.Message}");
        //         return Task.CompletedTask;
        //     },
        //     OnTokenValidated = context =>
        //     {
        //         Console.WriteLine($"JWT validated: {context.Principal.Identity.Name}");
        //         return Task.CompletedTask;
        //     }
        // };
    });

builder.Services.AddAuthorization();

// Retrait des détails dans les réponses API ModelState Invalides
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = true;
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

// Test si la connexion s'effectue bien
// DBConfiguration.VerifyConnectionToDB(app.Services);

// OpenAPI document and Swagger UI: enabled in Development or when explicitly enabled via configuration.
// Disabled by default in Production to limit information disclosure (see Microsoft security guidance).
var openApiOptions = app.Services.GetRequiredService<IOptions<OpenApiOptions>>().Value;
var enableOpenApi = app.Environment.IsDevelopment()
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();