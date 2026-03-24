using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Auth;
using DeliverTableTests.Global.Helpers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private IAuthService _authService = null!;
    private AuthController _sut = null!;

    private const string TestToken = "test-jwt-token";

    [SetUp]
    public void SetUp()
    {
        _authService = Substitute.For<IAuthService>();
        _sut = new AuthController(_authService);
    }

    #region Login

    [Test]
    public async Task Login_WithSuccessResult_ReturnsOk()
    {
        var request = new LoginRequest { Email = "login@example.com", Password = "SecurePass123!" };
        var connection = CreateConnectionResponse();
        _authService.LoginAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ConnectionResponse>.Success(connection));

        var result = await _sut.Login(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Login_WithFailureResult_ReturnsError()
    {
        var request = new LoginRequest { Email = "bad@example.com", Password = "wrong" };
        _authService.LoginAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ConnectionResponse>.Failure(new ServiceError("Identifiants invalides", 401)));

        var result = await _sut.Login(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(401));
    }

    #endregion

    #region Register

    [Test]
    public async Task Register_WithSuccessResult_ReturnsOk()
    {
        var request = new RegisterRequest
        {
            FirstName = "Jean",
            LastName = "Dupont",
            Email = "new@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        };
        _authService.RegisterAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ConnectionResponse>.Success(CreateConnectionResponse()));

        var result = await _sut.Register(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    #endregion

    #region GetProfile

    [Test]
    public async Task GetProfile_WithValidUser_ReturnsOk()
    {
        SetupAuthenticatedUser("42");
        _authService.GetProfileAsync(42, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<UserResponse>.Success(new UserResponse
            {
                Id = 42,
                FirstName = "Jean",
                LastName = "Dupont",
                Email = "jean@example.com",
                Role = nameof(UserRole.Customer)
            }));

        var result = await _sut.GetProfile(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetProfile_WithMissingClaim_ReturnsUnauthorized()
    {
        SetupAuthenticatedUser(null);

        var result = await _sut.GetProfile(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    #endregion

    #region Helpers

    private static ConnectionResponse CreateConnectionResponse() => new()
    {
        Token = TestToken,
        User = new UserResponse
        {
            Id = 42,
            Email = "jean@example.com",
            FirstName = "Jean",
            LastName = "Dupont",
            Role = nameof(UserRole.Customer)
        }
    };

    private void SetupAuthenticatedUser(string? userId)
        => AuthenticationTestHelper.SetupAuthenticatedUser(_sut, userId ?? string.Empty);

    #endregion
}
