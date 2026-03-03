namespace DeliverTableServer.Configuration;

/// <summary>
///     S3-compatible object storage credentials and endpoint configuration.
/// </summary>
public sealed record ObjectStorageConfig(
    string ServiceUrl,
    string AccessKey,
    string SecretKey,
    string BucketName,
    bool ForcePathStyle,
    string Region);
