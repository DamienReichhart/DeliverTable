namespace DeliverTableServer.Configuration;

/// <summary>
///     JWT signing and validation parameters. Populated by <see cref="AppEnvironment" />.
/// </summary>
public sealed class JwtConfig
{
    public required string Key { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public int ExpireMinutes { get; init; } = 60;
}
