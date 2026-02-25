using System.Text;
using DeliverTableServer.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace DeliverTableServer.Extensions;

public static class JwtExtensions
{
    /// <param name="services">The service collection.</param>
    /// <param name="jwtConfig">
    ///     Pre-validated JWT configuration provided by <see cref="AppEnvironment" />.
    /// </param>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        JwtConfig jwtConfig)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfig.Issuer,
                    ValidAudience = jwtConfig.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key))
                };
            });

        return services;
    }
}