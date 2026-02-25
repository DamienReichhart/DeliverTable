using System.Net;
using DeliverTableClient.Configuration;
using DeliverTableTests.Client.Helpers;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NSubstitute;

namespace DeliverTableTests.Client.Unit.Configuration;

// Mirrors the internal AppConfigurationOptions file names so tests stay in sync
// without requiring InternalsVisibleTo.
file static class ConfigFileNames
{
    public const string Production = "appconfig.json";
    public const string Development = "appconfig.Development.json";
}

/// <summary>
///     Tests for <see cref="AppConfigurationImplementation" />.
///     Verifies configuration loading, environment-aware file selection,
///     fallback behaviour, and idempotency.
/// </summary>
[TestFixture]
public class AppConfigurationImplementationTests
{
    private const string FallbackBaseAddress = "http://fallback.local/";

    private MockHttpMessageHandler _httpHandler = null!;
    private HttpClient _configHttpClient = null!;
    private IWebAssemblyHostEnvironment _hostEnvironment = null!;

    [SetUp]
    public void SetUp()
    {
        _httpHandler = new MockHttpMessageHandler();
        _configHttpClient = new HttpClient(_httpHandler) { BaseAddress = new Uri("http://localhost/") };
        _hostEnvironment = Substitute.For<IWebAssemblyHostEnvironment>();
        _hostEnvironment.BaseAddress.Returns("http://localhost/");
    }

    [TearDown]
    public void TearDown()
    {
        _configHttpClient.Dispose();
        _httpHandler.Dispose();
    }

    private AppConfigurationImplementation CreateSut(string? fallback = FallbackBaseAddress) =>
        new(_configHttpClient, _hostEnvironment, fallback!);

    #region Constructor

    [Test]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AppConfigurationImplementation(null!, _hostEnvironment, FallbackBaseAddress));
    }

    [Test]
    public void Constructor_WithNullHostEnvironment_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AppConfigurationImplementation(_configHttpClient, null!, FallbackBaseAddress));
    }

    [Test]
    public void Constructor_Defaults_NotLoadedWithEmptyValues()
    {
        var sut = CreateSut();

        Assert.Multiple(() =>
        {
            Assert.That(sut.IsLoaded, Is.False);
            Assert.That(sut.ApiBaseUrl, Is.Empty);
            Assert.That(sut.Environment, Is.Empty);
        });
    }

    #endregion

    #region LoadAsync — Production environment

    [Test]
    public async Task LoadAsync_InProduction_FetchesProductionConfigFile()
    {
        ConfigureEnvironment("Production");
        QueueConfigResponse(apiBaseUrl: "https://api.prod.com", environment: "Production");

        var sut = CreateSut();
        await sut.LoadAsync();

        Assert.That(_httpHandler.SentRequests[0].RequestUri!.PathAndQuery,
            Does.EndWith(ConfigFileNames.Production));
    }

    [Test]
    public async Task LoadAsync_WithValidConfig_SetsApiBaseUrl()
    {
        ConfigureEnvironment("Production");
        QueueConfigResponse(apiBaseUrl: "https://api.prod.com/", environment: "Production");

        var sut = CreateSut();
        await sut.LoadAsync();

        Assert.That(sut.ApiBaseUrl, Is.EqualTo("https://api.prod.com"));
    }

    [Test]
    public async Task LoadAsync_WithValidConfig_TrimsTrailingSlash()
    {
        ConfigureEnvironment("Production");
        QueueConfigResponse(apiBaseUrl: "https://api.prod.com/v1/", environment: "Production");

        var sut = CreateSut();
        await sut.LoadAsync();

        Assert.That(sut.ApiBaseUrl, Does.Not.EndWith("/"));
    }

    [Test]
    public async Task LoadAsync_WithValidConfig_SetsEnvironment()
    {
        ConfigureEnvironment("Production");
        QueueConfigResponse(apiBaseUrl: "https://api.prod.com", environment: "Production");

        var sut = CreateSut();
        await sut.LoadAsync();

        Assert.That(sut.Environment, Is.EqualTo("Production"));
    }

    [Test]
    public async Task LoadAsync_WithValidConfig_MarksAsLoaded()
    {
        ConfigureEnvironment("Production");
        QueueConfigResponse(apiBaseUrl: "https://api.prod.com", environment: "Production");

        var sut = CreateSut();
        await sut.LoadAsync();

        Assert.That(sut.IsLoaded, Is.True);
    }

    [Test]
    public async Task LoadAsync_WithEmptyBaseUrl_UsesFallbackBaseAddress()
    {
        ConfigureEnvironment("Production");
        QueueConfigResponse(apiBaseUrl: "", environment: "Production");

        var sut = CreateSut();
        await sut.LoadAsync();

        Assert.That(sut.ApiBaseUrl, Is.EqualTo(FallbackBaseAddress));
    }

    [Test]
    public async Task LoadAsync_WithNullApiSection_UsesFallbackBaseAddress()
    {
        ConfigureEnvironment("Production");
        _httpHandler.QueueJsonResponse(new { environment = "Production" });

        var sut = CreateSut();
        await sut.LoadAsync();

        Assert.That(sut.ApiBaseUrl, Is.EqualTo(FallbackBaseAddress));
    }

    #endregion

    #region LoadAsync — Development environment

    [Test]
    public async Task LoadAsync_InDevelopment_FetchesDevelopmentConfigFile()
    {
        ConfigureEnvironment("Development");
        QueueConfigResponse(apiBaseUrl: "http://localhost:5268", environment: "Development");

        var sut = CreateSut();
        await sut.LoadAsync();

        Assert.That(_httpHandler.SentRequests[0].RequestUri!.PathAndQuery,
            Does.EndWith(ConfigFileNames.Development));
    }

    [Test]
    public async Task LoadAsync_InDevelopment_WhenDevConfigMissing_FallsBackToBaseConfig()
    {
        ConfigureEnvironment("Development");
        _httpHandler.QueueErrorResponse(HttpStatusCode.NotFound);
        QueueConfigResponse(apiBaseUrl: "http://localhost:5268", environment: "Development");

        var sut = CreateSut();
        await sut.LoadAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_httpHandler.SentRequests, Has.Count.EqualTo(2));
            Assert.That(_httpHandler.SentRequests[1].RequestUri!.PathAndQuery,
                Does.EndWith(ConfigFileNames.Production));
            Assert.That(sut.ApiBaseUrl, Is.EqualTo("http://localhost:5268"));
        });
    }

    [Test]
    public async Task LoadAsync_InDevelopment_WhenBothConfigsMissing_UsesFallback()
    {
        ConfigureEnvironment("Development");
        _httpHandler.QueueErrorResponse(HttpStatusCode.NotFound);
        _httpHandler.QueueErrorResponse(HttpStatusCode.NotFound);

        var sut = CreateSut();
        await sut.LoadAsync();

        Assert.Multiple(() =>
        {
            Assert.That(sut.IsLoaded, Is.True);
            Assert.That(sut.ApiBaseUrl, Is.EqualTo(FallbackBaseAddress));
        });
    }

    #endregion

    #region LoadAsync — Idempotency

    [Test]
    public async Task LoadAsync_WhenAlreadyLoaded_DoesNotFetchAgain()
    {
        ConfigureEnvironment("Production");
        QueueConfigResponse(apiBaseUrl: "https://api.prod.com", environment: "Production");

        var sut = CreateSut();
        await sut.LoadAsync();
        await sut.LoadAsync();

        Assert.That(_httpHandler.SentRequests, Has.Count.EqualTo(1));
    }

    #endregion

    #region LoadAsync — Error resilience

    [Test]
    public async Task LoadAsync_InProduction_WhenRequestFails_MarksAsLoadedWithFallback()
    {
        ConfigureEnvironment("Production");
        _httpHandler.QueueErrorResponse(HttpStatusCode.InternalServerError);

        var sut = CreateSut();
        await sut.LoadAsync();

        Assert.Multiple(() =>
        {
            Assert.That(sut.IsLoaded, Is.True);
            Assert.That(sut.ApiBaseUrl, Is.EqualTo(FallbackBaseAddress));
        });
    }

    [Test]
    public async Task LoadAsync_WhenHttpClientThrows_MarksAsLoadedWithFallback()
    {
        ConfigureEnvironment("Production");
        _configHttpClient.Dispose();

        var sut = new AppConfigurationImplementation(
            _configHttpClient, _hostEnvironment, FallbackBaseAddress);
        await sut.LoadAsync();

        Assert.Multiple(() =>
        {
            Assert.That(sut.IsLoaded, Is.True);
            Assert.That(sut.ApiBaseUrl, Is.EqualTo(FallbackBaseAddress));
        });
    }

    #endregion

    private void ConfigureEnvironment(string environment)
    {
        _hostEnvironment.Environment.Returns(environment);
    }

    private void QueueConfigResponse(string apiBaseUrl, string environment)
    {
        _httpHandler.QueueJsonResponse(new
        {
            api = new { baseUrl = apiBaseUrl },
            environment
        });
    }
}
