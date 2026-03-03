using System.Net;
using System.Security.Claims;
using DeliverTableClient.Services.Auth;
using DeliverTableSharedLibrary.Dtos.Auth;
using DeliverTableTests.Client.Factories;
using DeliverTableTests.Client.Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using NSubstitute;

namespace DeliverTableTests.Client.Unit.Services.Auth;

/// <summary>
///     Tests for <see cref="ApiAuthStateProvider" />.
///     Verifies authentication state resolution from localStorage,
///     claims construction, and state-change notification flows.
/// </summary>
[TestFixture]
public class ApiAuthStateProviderTests
{
    private IJSRuntime _jsRuntime = null!;
    private MockHttpMessageHandler _mockHandler = null!;
    private HttpClient _httpClient = null!;
    private ApiAuthStateProvider _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _jsRuntime = Substitute.For<IJSRuntime>();
        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler) { BaseAddress = new Uri("http://localhost/") };
        _sut = new ApiAuthStateProvider(_jsRuntime, _httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _mockHandler.Dispose();
    }

    #region GetAuthenticationStateAsync

    [Test]
    public async Task GetAuthenticationStateAsync_WithToken_ReturnsAuthenticatedUser()
    {
        ConfigureLocalStorage(ClientTestFactory.ValidToken);
        QueueMeResponse();

        var state = await _sut.GetAuthenticationStateAsync();

        Assert.That(state.User.Identity?.IsAuthenticated, Is.True);
    }

    [Test]
    public async Task GetAuthenticationStateAsync_WithToken_BuildsCorrectClaims()
    {
        ConfigureLocalStorage(ClientTestFactory.ValidToken);
        var expectedUser = ClientTestFactory.CreateValidUserResponse();
        QueueMeResponse(expectedUser);

        var state = await _sut.GetAuthenticationStateAsync();
        var claims = state.User.Claims.ToList();

        Assert.Multiple(() =>
        {
            Assert.That(claims.First(c => c.Type == ClaimTypes.Role).Value,
                Is.EqualTo(expectedUser.Role));
            Assert.That(claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value,
                Is.EqualTo(expectedUser.Id.ToString()));
            Assert.That(claims.First(c => c.Type == ClaimTypes.Name).Value,
                Is.EqualTo(expectedUser.FirstName));
        });
    }

    [Test]
    public async Task GetAuthenticationStateAsync_WithToken_SetsAuthorizationHeader()
    {
        var token = ClientTestFactory.ValidToken;
        ConfigureLocalStorage(token);
        QueueMeResponse();

        await _sut.GetAuthenticationStateAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_httpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.That(_httpClient.DefaultRequestHeaders.Authorization!.Scheme,
                Is.EqualTo("bearer"));
            Assert.That(_httpClient.DefaultRequestHeaders.Authorization.Parameter,
                Is.EqualTo(token));
        });
    }

    [Test]
    public async Task GetAuthenticationStateAsync_WithToken_UsesJwtAuthType()
    {
        ConfigureLocalStorage(ClientTestFactory.ValidToken);
        QueueMeResponse();

        var state = await _sut.GetAuthenticationStateAsync();

        Assert.That(state.User.Identity?.AuthenticationType, Is.EqualTo("jwt"));
    }

    [Test]
    public async Task GetAuthenticationStateAsync_WithoutToken_ReturnsAnonymousUser()
    {
        ConfigureLocalStorage(token: null);

        var state = await _sut.GetAuthenticationStateAsync();

        Assert.That(state.User.Identity?.IsAuthenticated, Is.False);
    }

    [Test]
    public async Task GetAuthenticationStateAsync_WithEmptyToken_ReturnsAnonymousUser()
    {
        ConfigureLocalStorage(token: "   ");

        var state = await _sut.GetAuthenticationStateAsync();

        Assert.That(state.User.Identity?.IsAuthenticated, Is.False);
    }

    [Test]
    public async Task GetAuthenticationStateAsync_WithoutToken_ClearsAuthorizationHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "stale-token");

        ConfigureLocalStorage(token: null);

        await _sut.GetAuthenticationStateAsync();

        Assert.That(_httpClient.DefaultRequestHeaders.Authorization, Is.Null);
    }

    [Test]
    public async Task GetAuthenticationStateAsync_WithFailedApiResponse_ReturnsAnonymousUser()
    {
        ConfigureLocalStorage(ClientTestFactory.ValidToken);
        _mockHandler.QueueErrorResponse(HttpStatusCode.Unauthorized);

        var state = await _sut.GetAuthenticationStateAsync();

        Assert.That(state.User.Identity?.IsAuthenticated, Is.False);
    }

    #endregion

    #region NotifyUserAuthentication

    [Test]
    public void NotifyUserAuthentication_SetsAuthorizationHeader()
    {
        _sut.NotifyUserAuthentication(
            ClientTestFactory.ValidToken,
            ClientTestFactory.ValidRole,
            ClientTestFactory.ValidUserId,
            ClientTestFactory.ValidUserName);

        Assert.Multiple(() =>
        {
            Assert.That(_httpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.That(_httpClient.DefaultRequestHeaders.Authorization!.Scheme, Is.EqualTo("Bearer"));
            Assert.That(_httpClient.DefaultRequestHeaders.Authorization.Parameter,
                Is.EqualTo(ClientTestFactory.ValidToken));
        });
    }

    [Test]
    public async Task NotifyUserAuthentication_RaisesAuthenticationStateChanged()
    {
        AuthenticationState? receivedState = null;
        _sut.AuthenticationStateChanged += async task => { receivedState = await task; };

        _sut.NotifyUserAuthentication(
            ClientTestFactory.ValidToken,
            ClientTestFactory.ValidRole,
            ClientTestFactory.ValidUserId,
            ClientTestFactory.ValidUserName);

        await Task.Delay(50);

        Assert.Multiple(() =>
        {
            Assert.That(receivedState, Is.Not.Null);
            Assert.That(receivedState!.User.Identity?.IsAuthenticated, Is.True);
        });
    }

    [Test]
    public async Task NotifyUserAuthentication_BuildsCorrectClaimsInEvent()
    {
        AuthenticationState? receivedState = null;
        _sut.AuthenticationStateChanged += async task => { receivedState = await task; };

        _sut.NotifyUserAuthentication(
            ClientTestFactory.ValidToken,
            "RestaurantOwner",
            "99",
            "Marie");

        await Task.Delay(50);

        var claims = receivedState!.User.Claims.ToList();
        Assert.Multiple(() =>
        {
            Assert.That(claims.First(c => c.Type == ClaimTypes.Role).Value,
                Is.EqualTo("RestaurantOwner"));
            Assert.That(claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value,
                Is.EqualTo("99"));
            Assert.That(claims.First(c => c.Type == ClaimTypes.Name).Value,
                Is.EqualTo("Marie"));
        });
    }

    #endregion

    #region NotifyUserLogout

    [Test]
    public void NotifyUserLogout_ClearsAuthorizationHeader()
    {
        _sut.NotifyUserAuthentication(
            ClientTestFactory.ValidToken,
            ClientTestFactory.ValidRole,
            ClientTestFactory.ValidUserId,
            ClientTestFactory.ValidUserName);

        _sut.NotifyUserLogout();

        Assert.That(_httpClient.DefaultRequestHeaders.Authorization, Is.Null);
    }

    [Test]
    public async Task NotifyUserLogout_RaisesAuthenticationStateChangedWithAnonymousUser()
    {
        AuthenticationState? receivedState = null;
        _sut.AuthenticationStateChanged += async task => { receivedState = await task; };

        _sut.NotifyUserLogout();

        await Task.Delay(50);

        Assert.Multiple(() =>
        {
            Assert.That(receivedState, Is.Not.Null);
            Assert.That(receivedState!.User.Identity?.IsAuthenticated, Is.False);
        });
    }

    #endregion

    /// <summary>
    ///     Configures the <see cref="IJSRuntime" /> mock to return the given token
    ///     from <c>localStorage.getItem("authToken")</c>.
    /// </summary>
    private void ConfigureLocalStorage(string? token)
    {
        _jsRuntime.InvokeAsync<string>("localStorage.getItem",
                Arg.Is<object[]>(a => a.Length == 1 && (string)a[0] == "authToken"))
            .Returns(new ValueTask<string>(token!));
    }

    /// <summary>
    ///     Queues a successful <c>GET /me</c> response on the mock handler
    ///     returning the given <see cref="UserResponse" />.
    /// </summary>
    private void QueueMeResponse(UserResponse? user = null)
    {
        _mockHandler.QueueJsonResponse(user ?? ClientTestFactory.CreateValidUserResponse());
    }
}
