using System.Net;
using DeliverTableClient.Services.Auth;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos.Auth;
using DeliverTableTests.Client.Factories;
using DeliverTableTests.Client.Helpers;
using Microsoft.JSInterop;
using NSubstitute;

namespace DeliverTableTests.Client.Unit.Services.Auth;

/// <summary>
///     Tests for <see cref="AuthService" />.
///     Verifies login, registration and logout flows including
///     HTTP communication, response parsing, and auth state propagation.
/// </summary>
[TestFixture]
public class AuthServiceTests
{
    private MockHttpMessageHandler _httpHandler = null!;
    private HttpClient _httpClient = null!;
    private IJSRuntime _jsRuntime = null!;
    private ApiAuthStateProvider _authStateProvider = null!;
    private AuthService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _httpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler) { BaseAddress = new Uri("http://localhost/") };
        _jsRuntime = Substitute.For<IJSRuntime>();
        _authStateProvider = new ApiAuthStateProvider(_jsRuntime, _httpClient);
        _sut = new AuthService(_httpClient, _authStateProvider, _jsRuntime);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _httpHandler.Dispose();
    }

    #region Login

    [Test]
    public async Task Login_WithValidCredentials_ReturnsSuccess()
    {
        _httpHandler.QueueJsonResponse(
            ClientTestFactory.CreateValidConnectionResponse());

        var result = await _sut.Login(new LoginRequest
        {
            Email = "user@example.com",
            Password = "SecurePass123!"
        });

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task Login_WithValidCredentials_SetsAuthorizationHeader()
    {
        _httpHandler.QueueJsonResponse(
            ClientTestFactory.CreateValidConnectionResponse());

        await _sut.Login(new LoginRequest
        {
            Email = "user@example.com",
            Password = "SecurePass123!"
        });

        Assert.Multiple(() =>
        {
            Assert.That(_httpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.That(_httpClient.DefaultRequestHeaders.Authorization!.Scheme, Is.EqualTo("Bearer"));
            Assert.That(_httpClient.DefaultRequestHeaders.Authorization.Parameter,
                Is.EqualTo(ClientTestFactory.ValidToken));
        });
    }

    [Test]
    public async Task Login_WithValidCredentials_PostsToCorrectEndpoint()
    {
        _httpHandler.QueueJsonResponse(
            ClientTestFactory.CreateValidConnectionResponse());

        await _sut.Login(new LoginRequest
        {
            Email = "user@example.com",
            Password = "SecurePass123!"
        });

        Assert.Multiple(() =>
        {
            Assert.That(_httpHandler.SentRequests, Has.Count.EqualTo(1));
            Assert.That(_httpHandler.SentRequests[0].Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(_httpHandler.SentRequests[0].RequestUri!.PathAndQuery,
                Does.EndWith(ApiRoutes.Auth["Login"]));
        });
    }

    [Test]
    public async Task Login_WithApiError_ReturnsErrorMessage()
    {
        _httpHandler.QueueJsonResponse(
            ClientTestFactory.CreateApiErrorBody("Invalid credentials"),
            HttpStatusCode.Unauthorized);

        var result = await _sut.Login(new LoginRequest
        {
            Email = "user@example.com",
            Password = "SecurePass123!"
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Invalid credentials"));
        });
    }

    [Test]
    public async Task Login_WithUnparsableErrorBody_ReturnsFallbackError()
    {
        _httpHandler.QueueErrorResponse(
            HttpStatusCode.InternalServerError, "<html>Server Error</html>");

        var result = await _sut.Login(new LoginRequest
        {
            Email = "user@example.com",
            Password = "SecurePass123!"
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task Login_WithSuccessButEmptyToken_ReturnsError()
    {
        _httpHandler.QueueJsonResponse(
            ClientTestFactory.CreateConnectionResponseWithEmptyToken());

        var result = await _sut.Login(new LoginRequest
        {
            Email = "user@example.com",
            Password = "SecurePass123!"
        });

        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task Login_WithSuccessButUnparsableBody_ReturnsError()
    {
        _httpHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-json")
        });

        var result = await _sut.Login(new LoginRequest
        {
            Email = "user@example.com",
            Password = "SecurePass123!"
        });

        Assert.That(result.Success, Is.False);
    }

    #endregion

    #region Register

    [Test]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        _httpHandler.QueueJsonResponse(
            ClientTestFactory.CreateValidConnectionResponse());

        var result = await _sut.Register(new RegisterRequest
        {
            FirstName = "Jean",
            LastName = "Dupont",
            Email = "jean@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        });

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task Register_WithValidData_PostsToCorrectEndpoint()
    {
        _httpHandler.QueueJsonResponse(
            ClientTestFactory.CreateValidConnectionResponse());

        await _sut.Register(new RegisterRequest
        {
            FirstName = "Jean",
            LastName = "Dupont",
            Email = "jean@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        });

        Assert.That(_httpHandler.SentRequests[0].RequestUri!.PathAndQuery,
            Does.EndWith(ApiRoutes.Auth["Register"]));
    }

    [Test]
    public async Task Register_WithApiError_ReturnsErrorMessage()
    {
        _httpHandler.QueueJsonResponse(
            ClientTestFactory.CreateApiErrorBody("Email already exists"),
            HttpStatusCode.Conflict);

        var result = await _sut.Register(new RegisterRequest
        {
            FirstName = "Jean",
            LastName = "Dupont",
            Email = "taken@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Email already exists"));
        });
    }

    #endregion

    #region RegisterRestaurant

    [Test]
    public async Task RegisterRestaurant_WithValidData_ReturnsSuccess()
    {
        _httpHandler.QueueJsonResponse(
            ClientTestFactory.CreateValidConnectionResponse());

        var result = await _sut.RegisterRestaurant(new RestaurantRegister
        {
            FirstName = "Marie",
            LastName = "Curie",
            CompanyName = "Le Bon Restaurant",
            VatNumber = "BE0123456789",
            ContactPhoneNumber = "+32470123456",
            Email = "contact@restaurant.be",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        });

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task RegisterRestaurant_WithValidData_PostsToCorrectEndpoint()
    {
        _httpHandler.QueueJsonResponse(
            ClientTestFactory.CreateValidConnectionResponse());

        await _sut.RegisterRestaurant(new RestaurantRegister
        {
            FirstName = "Marie",
            LastName = "Curie",
            CompanyName = "Le Bon Restaurant",
            VatNumber = "BE0123456789",
            ContactPhoneNumber = "+32470123456",
            Email = "contact@restaurant.be",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        });

        Assert.That(_httpHandler.SentRequests[0].RequestUri!.PathAndQuery,
            Does.EndWith(ApiRoutes.Auth["RestaurantRegister"]));
    }

    [Test]
    public async Task RegisterRestaurant_WithApiError_ReturnsErrorMessage()
    {
        _httpHandler.QueueJsonResponse(
            ClientTestFactory.CreateApiErrorBody("VAT number already registered"),
            HttpStatusCode.Conflict);

        var result = await _sut.RegisterRestaurant(new RestaurantRegister
        {
            FirstName = "Marie",
            LastName = "Curie",
            CompanyName = "Le Bon Restaurant",
            VatNumber = "BE0123456789",
            ContactPhoneNumber = "+32470123456",
            Email = "contact@restaurant.be",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("VAT number already registered"));
        });
    }

    #endregion

    #region Logout

    [Test]
    public async Task Logout_ClearsAuthorizationHeader()
    {
        _httpHandler.QueueJsonResponse(
            ClientTestFactory.CreateValidConnectionResponse());
        await _sut.Login(new LoginRequest
        {
            Email = "user@example.com",
            Password = "SecurePass123!"
        });

        await _sut.Logout();

        Assert.That(_httpClient.DefaultRequestHeaders.Authorization, Is.Null);
    }

    #endregion
}
