using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Auth;
using DeliverTableTests.Server.Factories;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class AuthServiceTests
{
    private IUserRepository _userRepository = null!;
    private ITokenService _tokenService = null!;
    private IEmailJobService _emailJobService = null!;
    private AuthService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _tokenService = Substitute.For<ITokenService>();
        _emailJobService = Substitute.For<IEmailJobService>();
        _sut = new AuthService(_userRepository, _tokenService, _emailJobService);
    }

    [Test]
    public async Task UpdateProfile_WithBillingFields_PersistsThemTrimmed()
    {
        var user = ServerEntityFactory.CreateValidUser("u@example.fr");
        user.Id = 42;
        user.BillingAddressLine1 = string.Empty;
        user.BillingPostalCode = string.Empty;
        user.BillingCity = string.Empty;
        user.BillingCountry = string.Empty;

        _userRepository.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(user);
        _userRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var request = new UpdateProfileRequest
        {
            FirstName = "Jean",
            LastName = "Dupont",
            Email = "u@example.fr",
            BillingAddressLine1 = "  12 rue de la Paix  ",
            BillingAddressLine2 = "",
            BillingPostalCode = " 75002 ",
            BillingCity = " Paris ",
            BillingCountry = " France ",
        };

        var result = await _sut.UpdateProfileAsync(42, request, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(user.BillingAddressLine1, Is.EqualTo("12 rue de la Paix"));
        Assert.That(user.BillingAddressLine2, Is.EqualTo(""));
        Assert.That(user.BillingPostalCode, Is.EqualTo("75002"));
        Assert.That(user.BillingCity, Is.EqualTo("Paris"));
        Assert.That(user.BillingCountry, Is.EqualTo("France"));
    }
}
