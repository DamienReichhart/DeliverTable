using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DeliverTableServer.Configuration;
using DeliverTableInfrastructure.Models;
using DeliverTableServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using DeliverTableSharedLibrary.Constants.Enums;
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
            .Returns(new List<string> { nameof(UserRole.Customer) });
    }

    [TearDown]
    public void TearDown()
    {
        (_userManager as IDisposable)?.Dispose();
    }

    [Test]
    public async Task CreateToken_ReturnsNonEmptyString()
    {
        string token = await _sut.CreateToken(_testUser);

        Assert.That(token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task CreateToken_ContainsCorrectSubjectClaim()
    {
        string token = await _sut.CreateToken(_testUser);

        JwtSecurityToken jwt = ParseToken(token);
        Assert.That(jwt.Subject, Is.EqualTo(_testUser.Id.ToString()));
    }

    [Test]
    public async Task CreateToken_DoesNotContainEmailClaim()
    {
        string token = await _sut.CreateToken(_testUser);

        JwtSecurityToken jwt = ParseToken(token);
        Claim? emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.That(emailClaim, Is.Null);
    }

    [Test]
    public async Task CreateToken_ContainsCorrectRoleClaim()
    {
        string token = await _sut.CreateToken(_testUser);

        string roleValue = ExtractRoleClaim(token);
        Assert.That(roleValue, Is.EqualTo(nameof(UserRole.Customer)));
    }

    [Test]
    public async Task CreateToken_UsesDefaultRole_WhenUserHasNoRoles()
    {
        _userManager.GetRolesAsync(Arg.Any<User>())
            .Returns(new List<string>());

        string token = await _sut.CreateToken(_testUser);

        string roleValue = ExtractRoleClaim(token);
        Assert.That(roleValue, Is.EqualTo(nameof(UserRole.Customer)));
    }

    [Test]
    public async Task CreateToken_HasCorrectIssuerAndAudience()
    {
        string token = await _sut.CreateToken(_testUser);

        JwtSecurityToken jwt = ParseToken(token);
        Assert.That(jwt.Issuer, Is.EqualTo(_jwtConfig.Issuer));
        Assert.That(jwt.Audiences.First(), Is.EqualTo(_jwtConfig.Audience));
    }

    [Test]
    public async Task CreateToken_ExpiresWithinConfiguredWindow()
    {
        DateTime before = DateTime.UtcNow;

        string token = await _sut.CreateToken(_testUser);

        JwtSecurityToken jwt = ParseToken(token);
        DateTime expectedExpiry = before.AddMinutes(_jwtConfig.ExpireMinutes);
        Assert.That(jwt.ValidTo, Is.EqualTo(expectedExpiry).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public async Task CreateToken_CanBeValidatedWithSameKey()
    {
        string token = await _sut.CreateToken(_testUser);

        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
        TokenValidationParameters validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _jwtConfig.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtConfig.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true
        };

        TokenValidationResult result = await handler.ValidateTokenAsync(token, validationParams);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task CreateToken_DoesNotContainEmailClaim_WhenEmailIsNull()
    {
        _testUser.Email = null;

        string token = await _sut.CreateToken(_testUser);

        JwtSecurityToken jwt = ParseToken(token);
        Claim? emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.That(emailClaim, Is.Null);
    }

    private static JwtSecurityToken ParseToken(string token)
    {
        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        return handler.ReadJwtToken(token);
    }

    /// <summary>
    ///     Extracts the role claim value, accounting for the JwtSecurityTokenHandler's
    ///     outbound claim type mapping (ClaimTypes.Role URI vs short "role" key).
    /// </summary>
    private static string ExtractRoleClaim(string token)
    {
        JwtSecurityToken jwt = ParseToken(token);
        Claim roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)
                        ?? jwt.Claims.First(c => c.Type == "role");
        return roleClaim.Value;
    }
}
