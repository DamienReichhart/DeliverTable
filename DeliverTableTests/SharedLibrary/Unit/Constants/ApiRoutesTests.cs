using DeliverTableSharedLibrary.Constants;

namespace DeliverTableTests.SharedLibrary.Unit.Constants;

/// <summary>
///     Contract tests for <see cref="ApiRoutes" />.
///     These guard against accidental route changes that would break client/server communication.
/// </summary>
[TestFixture]
[Category("Contract")]
public class ApiRoutesTests
{
    private const string VersionPrefix = "api/v1/";

    #region Exact value contracts

    [Test]
    public void Health_ShouldEqualExpectedRoute()
    {
        Assert.That(ApiRoutes.Health, Is.EqualTo("api/v1/health"));
    }

    [Test]
    public void Authentication_ShouldEqualExpectedRoute()
    {
        Assert.That(ApiRoutes.Authentication, Is.EqualTo("api/v1/auth"));
    }

    [TestCase("Login", "api/v1/auth/login")]
    [TestCase("Register", "api/v1/auth/register")]
    [TestCase("RestaurantRegister", "api/v1/auth/restaurant/register")]
    public void AuthDictionary_ShouldContainExpectedEntry(string key, string expectedRoute)
    {
        Assert.That(ApiRoutes.Auth, Does.ContainKey(key));
        Assert.That(ApiRoutes.Auth[key], Is.EqualTo(expectedRoute));
    }

    #endregion

    #region Structural invariants

    [Test]
    public void AllStaticRoutes_ShouldStartWithVersionPrefix()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ApiRoutes.Health, Does.StartWith(VersionPrefix));
            Assert.That(ApiRoutes.Authentication, Does.StartWith(VersionPrefix));
        });
    }

    [Test]
    public void AuthDictionary_AllValues_ShouldStartWithVersionPrefix()
    {
        foreach (var (key, route) in ApiRoutes.Auth)
            Assert.That(route, Does.StartWith(VersionPrefix),
                $"Route '{key}' violates version prefix convention");
    }

    [Test]
    public void AuthDictionary_AllValues_ShouldBeSubPathsOfAuthenticationBase()
    {
        foreach (var (key, route) in ApiRoutes.Auth)
            Assert.That(route, Does.StartWith(ApiRoutes.Authentication),
                $"Route '{key}' is not a sub-path of the Authentication base");
    }

    [Test]
    public void AllRoutes_ShouldBeRelative_NoLeadingSlash()
    {
        var allRoutes = CollectAllRoutes();

        foreach (var route in allRoutes)
            Assert.That(route, Does.Not.StartWith("/"),
                $"Route '{route}' must be relative (no leading slash)");
    }

    [Test]
    public void AllRoutes_ShouldBeLowerCase()
    {
        var allRoutes = CollectAllRoutes();

        foreach (var route in allRoutes)
            Assert.That(route, Is.EqualTo(route.ToLowerInvariant()),
                $"Route '{route}' contains uppercase characters");
    }

    [Test]
    public void AuthDictionary_ShouldHaveNoDuplicateValues()
    {
        Assert.That(ApiRoutes.Auth.Values, Is.Unique);
    }

    [Test]
    public void AuthDictionary_ShouldNotBeEmpty()
    {
        Assert.That(ApiRoutes.Auth, Is.Not.Empty);
    }

    #endregion

    private static List<string> CollectAllRoutes()
    {
        var routes = new List<string> { ApiRoutes.Health, ApiRoutes.Authentication };
        routes.AddRange(ApiRoutes.Auth.Values);
        return routes;
    }
}
