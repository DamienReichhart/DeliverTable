using DeliverTableServer.Configuration;
using DeliverTableServer.Data;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDeliverTableDatabase(this IServiceCollection services)
    {
        var connectionString = DBConfiguration.BuildConnectionString();

        services.AddDbContext<DeliverTableContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}