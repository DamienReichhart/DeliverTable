using DeliverTableServer.Configuration;

namespace DeliverTableTests.Server.Unit.Configuration;

[TestFixture]
public class JwtConfigTests
{
    private readonly string[] _envVarNames =
        ["JWT_KEY", "JWT_ISSUER", "JWT_AUDIENCE", "JWT_EXPIRE_MINUTES"];

    [TearDown]
    public void TearDown()
    {
        foreach (var name in _envVarNames)
            Environment.SetEnvironmentVariable(name, null);
    }

    [Test]
    public void LoadFromEnv_ReadsAllEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("JWT_KEY", "my-secret-key");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "my-issuer");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "my-audience");
        Environment.SetEnvironmentVariable("JWT_EXPIRE_MINUTES", "120");

        var config = JwtConfig.LoadFromEnv();

        Assert.Multiple(() =>
        {
            Assert.That(config.Key, Is.EqualTo("my-secret-key"));
            Assert.That(config.Issuer, Is.EqualTo("my-issuer"));
            Assert.That(config.Audience, Is.EqualTo("my-audience"));
            Assert.That(config.ExpireMinutes, Is.EqualTo(120));
        });
    }

    [Test]
    public void LoadFromEnv_DefaultsExpireMinutesTo60_WhenNotSet()
    {
        Environment.SetEnvironmentVariable("JWT_KEY", "key");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "issuer");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "audience");

        var config = JwtConfig.LoadFromEnv();

        Assert.That(config.ExpireMinutes, Is.EqualTo(60));
    }

    [Test]
    public void LoadFromEnv_DefaultsStringsToEmpty_WhenNotSet()
    {
        var config = JwtConfig.LoadFromEnv();

        Assert.Multiple(() =>
        {
            Assert.That(config.Key, Is.EqualTo(string.Empty));
            Assert.That(config.Issuer, Is.EqualTo(string.Empty));
            Assert.That(config.Audience, Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void LoadFromEnv_ThrowsFormatException_WhenExpireMinutesIsNotNumeric()
    {
        Environment.SetEnvironmentVariable("JWT_EXPIRE_MINUTES", "invalid");

        Assert.Throws<FormatException>(() => JwtConfig.LoadFromEnv());
    }
}
