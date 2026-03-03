using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DeliverTableServer.Configuration;
using DeliverTableServer.Models;
using DeliverTableServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using DeliverTableTests.Global.Helpers;
using DeliverTableTests.Server.Factories;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class TokenServiceTests
{
    private JwtConfig _jwtConfig = null!;
    private UserManager<User> _userManager = null!;
    private TokenService _sut = null!;
    private User _testUser = null!;

    [SetUp]
    public void SetUp()
    {
        _jwtConfig = ServerEntityFactory.CreateTestJwtConfig();
        _userManager = UserManagerMockHelper.Create();
        _sut = new TokenService(_jwtConfig, _userManager);

        _testUser = ServerEntityFactory.CreateValidUser("token@example.com");
        _testUser.Id = 42;

        _userManager.GetRolesAsync(Arg.Any<User>())
            .Returns(new List<string> { "Customer" });
    }

    [TearDown]
    public void TearDown()
    {
        (_userManager as IDisposable)?.Dispose();
    }

    [Test]
    public async Task CreateToken_ReturnsNonEmptyString()
    {
        var token = await _sut.CreateToken(_testUser);

        Assert.That(token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task CreateToken_ContainsCorrectSubjectClaim()
    {
        var token = await _sut.CreateToken(_testUser);

        var jwt = ParseToken(token);
        Assert.That(jwt.Subject, Is.EqualTo(_testUser.Id.ToString()));
    }

    [Test]
    public async Task CreateToken_DoesNotContainEmailClaim()
    {
        var token = await _sut.CreateToken(_testUser);

        var jwt = ParseToken(token);
        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.That(emailClaim, Is.Null);
    }

    [Test]
    public async Task CreateToken_ContainsCorrectRoleClaim()
    {
        var token = await _sut.CreateToken(_testUser);

        var roleValue = ExtractRoleClaim(token);
        Assert.That(roleValue, Is.EqualTo("Customer"));
    }

    [Test]
    public async Task CreateToken_UsesDefaultRole_WhenUserHasNoRoles()
    {
        _userManager.GetRolesAsync(Arg.Any<User>())
            .Returns(new List<string>());

        var token = await _sut.CreateToken(_testUser);

        var roleValue = ExtractRoleClaim(token);
        Assert.That(roleValue, Is.EqualTo("Customer"));
    }

    [Test]
    public async Task CreateToken_HasCorrectIssuerAndAudience()
    {
        var token = await _sut.CreateToken(_testUser);

        var jwt = ParseToken(token);
        Assert.That(jwt.Issuer, Is.EqualTo(_jwtConfig.Issuer));
        Assert.That(jwt.Audiences.First(), Is.EqualTo(_jwtConfig.Audience));
    }

    [Test]
    public async Task CreateToken_ExpiresWithinConfiguredWindow()
    {
        var before = DateTime.UtcNow;

        var token = await _sut.CreateToken(_testUser);

        var jwt = ParseToken(token);
        var expectedExpiry = before.AddMinutes(_jwtConfig.ExpireMinutes);
        Assert.That(jwt.ValidTo, Is.EqualTo(expectedExpiry).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public async Task CreateToken_CanBeValidatedWithSameKey()
    {
        var token = await _sut.CreateToken(_testUser);

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _jwtConfig.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtConfig.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true
        };

        var result = await handler.ValidateTokenAsync(token, validationParams);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task CreateToken_DoesNotContainEmailClaim_WhenEmailIsNull()
    {
        _testUser.Email = null;

        var token = await _sut.CreateToken(_testUser);

        var jwt = ParseToken(token);
        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.That(emailClaim, Is.Null);
    }

    private static JwtSecurityToken ParseToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ReadJwtToken(token);
    }

    /// <summary>
    ///     Extracts the role claim value, accounting for the JwtSecurityTokenHandler's
    ///     outbound claim type mapping (ClaimTypes.Role URI vs short "role" key).
    /// </summary>
    private static string ExtractRoleClaim(string token)
    {
        var jwt = ParseToken(token);
        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)
                        ?? jwt.Claims.First(c => c.Type == "role");
        return roleClaim.Value;
    }
}
