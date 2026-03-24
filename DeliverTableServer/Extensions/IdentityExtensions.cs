using DeliverTableServer.Data;
using DeliverTableServer.Models;
using Microsoft.AspNetCore.Identity;

namespace DeliverTableServer.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddIdentityParams(this IServiceCollection services)
    {
        services.AddIdentity<User, IdentityRole<int>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 12;
            }).AddEntityFrameworkStores<DeliverTableContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}
