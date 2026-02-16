namespace DeliverTableServer.Configuration;

public class JwtConfig
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpireMinutes { get; set; } = 60;

    public static JwtConfig LoadFromEnv()
    {
        return new JwtConfig
        {
            Key = Environment.GetEnvironmentVariable("JWT_KEY") ?? "",
            Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "",
            Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "",
            ExpireMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRE_MINUTES") ?? "60")
        };
    }
}
