using Amazon.S3;
using DeliverTableServer.Configuration;
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;

namespace DeliverTableServer.Extensions;

/// <summary>
///     Registers the S3-compatible object storage client and related services.
/// </summary>
public static class ObjectStorageExtensions
{
    /// <param name="services">The service collection.</param>
    /// <param name="config">
    ///     Pre-validated object storage configuration provided by <see cref="AppEnvironment" />.
    /// </param>
    public static IServiceCollection AddObjectStorage(
        this IServiceCollection services,
        ObjectStorageConfig config)
    {
        var s3Config = new AmazonS3Config
        {
            ServiceURL = config.ServiceUrl,
            ForcePathStyle = config.ForcePathStyle,
            AuthenticationRegion = config.Region
        };

        services.AddSingleton<IAmazonS3>(
            new AmazonS3Client(config.AccessKey, config.SecretKey, s3Config));
        services.AddScoped<IObjectStorageService, ObjectStorageService>();

        return services;
    }
}
