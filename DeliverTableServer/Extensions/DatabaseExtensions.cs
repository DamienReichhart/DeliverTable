using DeliverTableServer.Configuration;
using DeliverTableServer.Data;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDeliverTableDatabase(this IServiceCollection services)
    {
        var connectionString = DBConfiguration.BuildConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("CONNECTION_STRING environment variable is not set.");
        }

        services.AddDbContext<DeliverTableContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}