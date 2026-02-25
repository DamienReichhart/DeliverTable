using System.Security.Claims;
using DeliverTableServer.Controllers;
using DeliverTableServer.Models;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using DeliverTableTests.Global.Helpers;
using DeliverTableTests.Server.Factories;
using DeliverTableTests.Server.Fixtures;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private TestDatabase _db = null!;
    private UserManager<User> _userManager = null!;
    private ITokenService _tokenService = null!;
    private IHostEnvironment _env = null!;
    private AuthController _sut = null!;

    private const string ValidPassword = "SecurePass123!";
    private const string TestToken = "test-jwt-token";

    [SetUp]
    public void SetUp()
    {
        _db = new TestDatabase();
        _userManager = UserManagerMockHelper.Create();
        _tokenService = Substitute.For<ITokenService>();
        _env = Substitute.For<IHostEnvironment>();
        _env.EnvironmentName.Returns("Development");

        _tokenService.CreateToken(Arg.Any<User>()).Returns(TestToken);
        _userManager.GetRolesAsync(Arg.Any<User>()).Returns(new List<string> { "Customer" });

        _sut = new AuthController(_db.Context, _tokenService, _userManager, _env);
    }

    [TearDown]
    public void TearDown()
    {
        (_sut as IDisposable)?.Dispose();
        (_userManager as IDisposable)?.Dispose();
        _db.Dispose();
    }

    #region Login

    [Test]
    public async Task Login_WithValidCredentials_ReturnsOkWithConnectionResponse()
    {
        var user = await SeedUserAsync("login@example.com");
        _userManager.CheckPasswordAsync(Arg.Any<User>(), ValidPassword).Returns(true);

        var request = new LoginRequest { Email = "login@example.com", Password = ValidPassword };
        var result = await _sut.Login(request);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var body = ((OkObjectResult)result).Value as ConnectionResponse;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.Token, Is.EqualTo(TestToken));
        Assert.That(body.User.Email, Is.EqualTo(user.Email));
    }

    [Test]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var request = new LoginRequest { Email = "unknown@example.com", Password = ValidPassword };

        var result = await _sut.Login(request);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await SeedUserAsync("wrong-pw@example.com");
        _userManager.CheckPasswordAsync(Arg.Any<User>(), Arg.Any<string>()).Returns(false);

        var request = new LoginRequest { Email = "wrong-pw@example.com", Password = "WrongPassword1!" };
        var result = await _sut.Login(request);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task Login_WithInvalidModelState_ReturnsBadRequest()
    {
        _sut.ModelState.AddModelError("Email", "Required");

        var request = new LoginRequest { Email = "", Password = "" };
        var result = await _sut.Login(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    #endregion

    #region Register

    [Test]
    public async Task Register_WithValidData_ReturnsOkWithConnectionResponse()
    {
        SetupSuccessfulUserCreation();

        var request = new RegisterRequest
        {
            FirstName = "Jean",
            LastName = "Dupont",
            Email = "new@example.com",
            Password = ValidPassword,
            ConfirmPassword = ValidPassword
        };

        var result = await _sut.Register(request);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var body = ((OkObjectResult)result).Value as ConnectionResponse;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.Token, Is.EqualTo(TestToken));
        Assert.That(body.User.FirstName, Is.EqualTo("Jean"));
    }

    [Test]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        await SeedUserAsync("dup@example.com");

        var request = new RegisterRequest
        {
            FirstName = "Jean",
            LastName = "Dupont",
            Email = "dup@example.com",
            Password = ValidPassword,
            ConfirmPassword = ValidPassword
        };

        var result = await _sut.Register(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Register_WhenUserCreationFails_ReturnsBadRequest()
    {
        _userManager.CreateAsync(Arg.Any<User>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

        var request = new RegisterRequest
        {
            FirstName = "Jean",
            LastName = "Dupont",
            Email = "fail@example.com",
            Password = ValidPassword,
            ConfirmPassword = ValidPassword
        };

        var result = await _sut.Register(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Register_WithInvalidModelState_ReturnsBadRequest()
    {
        _sut.ModelState.AddModelError("Email", "Required");

        var request = new RegisterRequest();
        var result = await _sut.Register(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    #endregion

    #region RegisterRestaurant

    [Test]
    public async Task RegisterRestaurant_WithValidData_ReturnsOkWithConnectionResponse()
    {
        SetupSuccessfulUserCreation("Restaurant_Owner");

        var request = new RestaurantRegister
        {
            FirstName = "Marie",
            LastName = "Curie",
            CompanyName = "Le Bon Restaurant",
            VatNumber = "BE0123456789",
            ContactPhoneNumber = "+32470123456",
            Email = "resto@example.com",
            Password = ValidPassword,
            ConfirmPassword = ValidPassword
        };

        var result = await _sut.RegisterRestaurant(request);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var body = ((OkObjectResult)result).Value as ConnectionResponse;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.Token, Is.EqualTo(TestToken));
    }

    [Test]
    public async Task RegisterRestaurant_WithDuplicateEmail_ReturnsBadRequest()
    {
        await SeedUserAsync("dup-resto@example.com");

        var request = new RestaurantRegister
        {
            FirstName = "Marie",
            LastName = "Curie",
            CompanyName = "Le Bon Restaurant",
            VatNumber = "BE0123456789",
            ContactPhoneNumber = "+32470123456",
            Email = "dup-resto@example.com",
            Password = ValidPassword,
            ConfirmPassword = ValidPassword
        };

        var result = await _sut.RegisterRestaurant(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RegisterRestaurant_WhenCreationFails_ReturnsBadRequest()
    {
        _userManager.CreateAsync(Arg.Any<User>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Failure" }));

        var request = new RestaurantRegister
        {
            FirstName = "Marie",
            LastName = "Curie",
            CompanyName = "Le Bon Restaurant",
            VatNumber = "BE0123456789",
            ContactPhoneNumber = "+32470123456",
            Email = "fail-resto@example.com",
            Password = ValidPassword,
            ConfirmPassword = ValidPassword
        };

        var result = await _sut.RegisterRestaurant(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    #endregion

    #region GetProfile

    [Test]
    public async Task GetProfile_WithValidToken_ReturnsOkWithUserResponse()
    {
        var user = await SeedUserAsync("profile@example.com");
        SetupAuthenticatedUser(user.Id.ToString());

        var result = await _sut.GetProfile();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var body = ((OkObjectResult)result).Value as UserResponse;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.Email, Is.EqualTo("profile@example.com"));
    }

    [Test]
    public async Task GetProfile_WithMissingNameIdentifier_ReturnsUnauthorized()
    {
        SetupAuthenticatedUser(userId: null);

        var result = await _sut.GetProfile();

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetProfile_WithNonNumericId_ReturnsUnauthorized()
    {
        SetupAuthenticatedUser("not-a-number");

        var result = await _sut.GetProfile();

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetProfile_WhenUserNotFound_ReturnsNotFound()
    {
        SetupAuthenticatedUser("99999");

        var result = await _sut.GetProfile();

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    #endregion

    #region Helpers

    private async Task<User> SeedUserAsync(string email)
    {
        var user = ServerEntityFactory.CreateValidUser(email);
        _db.Context.Users.Add(user);
        await _db.Context.SaveChangesAsync();
        return user;
    }

    private void SetupSuccessfulUserCreation(string role = "Customer")
    {
        _userManager.CreateAsync(Arg.Any<User>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<User>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        _userManager.GetRolesAsync(Arg.Any<User>())
            .Returns(new List<string> { role });
    }

    private void SetupAuthenticatedUser(string? userId)
    {
        var claims = new List<Claim>();
        if (userId != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #endregion
}
