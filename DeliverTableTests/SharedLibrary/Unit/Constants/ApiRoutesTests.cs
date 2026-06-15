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
    public void AuthBase_ShouldEqualExpectedRoute()
    {
        Assert.That(ApiRoutes.Auth.Base, Is.EqualTo("api/v1/auth"));
    }

    [TestCase(nameof(ApiRoutes.Auth.Login), "api/v1/auth/login")]
    [TestCase(nameof(ApiRoutes.Auth.Register), "api/v1/auth/register")]
    [TestCase(nameof(ApiRoutes.Auth.RestaurantRegister), "api/v1/auth/restaurant/register")]
    [TestCase(nameof(ApiRoutes.Auth.Me), "api/v1/auth/me")]
    public void AuthFullPath_ShouldEqualExpectedRoute(string name, string expectedRoute)
    {
        string actual = name switch
        {
            nameof(ApiRoutes.Auth.Login) => ApiRoutes.Auth.Login,
            nameof(ApiRoutes.Auth.Register) => ApiRoutes.Auth.Register,
            nameof(ApiRoutes.Auth.RestaurantRegister) => ApiRoutes.Auth.RestaurantRegister,
            nameof(ApiRoutes.Auth.Me) => ApiRoutes.Auth.Me,
            _ => throw new ArgumentException($"Unknown route: {name}")
        };

        Assert.That(actual, Is.EqualTo(expectedRoute));
    }

    [Test]
    public void RestaurantBase_ShouldEqualExpectedRoute()
    {
        Assert.That(ApiRoutes.Restaurant.Base, Is.EqualTo("api/v1/restaurant"));
    }

    [Test]
    public void RestaurantUserMe_ShouldEqualExpectedRoute()
    {
        Assert.That(ApiRoutes.Restaurant.UserMe, Is.EqualTo("api/v1/restaurant/user/me"));
    }

    #endregion

    #region Structural invariants

    [Test]
    public void AllBaseRoutes_ShouldStartWithVersionPrefix()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ApiRoutes.Health, Does.StartWith(VersionPrefix));
            Assert.That(ApiRoutes.Auth.Base, Does.StartWith(VersionPrefix));
            Assert.That(ApiRoutes.Restaurant.Base, Does.StartWith(VersionPrefix));
        });
    }

    [Test]
    public void AuthFullPaths_ShouldStartWithAuthBase()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ApiRoutes.Auth.Login, Does.StartWith(ApiRoutes.Auth.Base));
            Assert.That(ApiRoutes.Auth.Register, Does.StartWith(ApiRoutes.Auth.Base));
            Assert.That(ApiRoutes.Auth.RestaurantRegister, Does.StartWith(ApiRoutes.Auth.Base));
            Assert.That(ApiRoutes.Auth.Me, Does.StartWith(ApiRoutes.Auth.Base));
        });
    }

    [Test]
    public void RestaurantFullPaths_ShouldStartWithRestaurantBase()
    {
        Assert.That(ApiRoutes.Restaurant.UserMe, Does.StartWith(ApiRoutes.Restaurant.Base));
    }

    [Test]
    public void AllRoutes_ShouldBeRelative_NoLeadingSlash()
    {
        List<string> allRoutes = CollectAllRoutes();

        foreach (string route in allRoutes)
            Assert.That(route, Does.Not.StartWith("/"),
                $"Route '{route}' must be relative (no leading slash)");
    }

    [Test]
    public void AllRoutes_ShouldBeLowerCase()
    {
        List<string> allRoutes = CollectAllRoutes();

        foreach (string route in allRoutes)
            Assert.That(route, Is.EqualTo(route.ToLowerInvariant()),
                $"Route '{route}' contains uppercase characters");
    }

    #endregion

    private static List<string> CollectAllRoutes()
    {
        return
        [
            ApiRoutes.Health,
            ApiRoutes.Auth.Base,
            ApiRoutes.Auth.Login,
            ApiRoutes.Auth.Register,
            ApiRoutes.Auth.RestaurantRegister,
            ApiRoutes.Auth.Me,
            ApiRoutes.Restaurant.Base,
            ApiRoutes.Restaurant.UserMe
        ];
    }
}
