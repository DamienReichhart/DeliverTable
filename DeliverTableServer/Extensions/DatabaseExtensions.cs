using DeliverTableServer.Data;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Extensions;

public static class DatabaseExtensions
{
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">
    ///     Pre-validated database connection string provided by <see cref="Configuration.AppEnvironment" />.
    /// </param>
    public static IServiceCollection AddDeliverTableDatabase(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<DeliverTableContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}