using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableMigrator.Seeding;
using DeliverTableSharedLibrary.Constants.Enums;
using NSubstitute;

namespace DeliverTableTests.Migrator.Unit;

[TestFixture]
public class AdminSeederTests
{
    private const string Email = "admin@delivertable.example";
    private const string Password = "Sup3r!Secret-Pw";
    private const string FirstName = "Admin";
    private const string LastName = "DeliverTable";

    private IUserRepository _userRepository = null!;
    private AdminSeeder _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _sut = new AdminSeeder(_userRepository);
    }

    [Test]
    public async Task SeedAsync_WhenAdministratorAlreadyExists_SkipsCreation()
    {
        _userRepository
            .ListByRoleAsync(nameof(UserRole.Administrator), Arg.Any<CancellationToken>())
            .Returns([new User { Email = "existing@delivertable.example" }]);

        AdminSeedResult result = await _sut.SeedAsync(Email, Password, FirstName, LastName);

        Assert.That(result.Outcome, Is.EqualTo(AdminSeedOutcome.AlreadyExists));
        await _userRepository.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<string>());
        await _userRepository.DidNotReceive().AddToRoleAsync(Arg.Any<User>(), Arg.Any<string>());
    }

    [Test]
    public async Task SeedAsync_WhenNoAdministratorExists_CreatesUserWithAdministratorRole()
    {
        _userRepository
            .ListByRoleAsync(nameof(UserRole.Administrator), Arg.Any<CancellationToken>())
            .Returns([]);
        _userRepository.CreateAsync(Arg.Any<User>(), Password).Returns((true, Array.Empty<string>()));
        _userRepository.AddToRoleAsync(Arg.Any<User>(), nameof(UserRole.Administrator))
            .Returns((true, Array.Empty<string>()));

        AdminSeedResult result = await _sut.SeedAsync(Email, Password, FirstName, LastName);

        Assert.That(result.Outcome, Is.EqualTo(AdminSeedOutcome.Created));
        await _userRepository.Received(1).CreateAsync(
            Arg.Is<User>(u =>
                u.Email == Email &&
                u.UserName == Email &&
                u.FirstName == FirstName &&
                u.LastName == LastName),
            Password);
        await _userRepository.Received(1).AddToRoleAsync(
            Arg.Any<User>(), nameof(UserRole.Administrator));
    }

    [Test]
    public async Task SeedAsync_WhenCreationFails_ReturnsFailedAndDoesNotAssignRole()
    {
        _userRepository
            .ListByRoleAsync(nameof(UserRole.Administrator), Arg.Any<CancellationToken>())
            .Returns([]);
        _userRepository.CreateAsync(Arg.Any<User>(), Password)
            .Returns((false, new[] { "Passwords must have at least one digit." }));

        AdminSeedResult result = await _sut.SeedAsync(Email, Password, FirstName, LastName);

        Assert.That(result.Outcome, Is.EqualTo(AdminSeedOutcome.Failed));
        Assert.That(result.Errors, Does.Contain("Passwords must have at least one digit."));
        await _userRepository.DidNotReceive().AddToRoleAsync(Arg.Any<User>(), Arg.Any<string>());
    }

    [Test]
    public async Task SeedAsync_WhenRoleAssignmentFails_ReturnsFailed()
    {
        _userRepository
            .ListByRoleAsync(nameof(UserRole.Administrator), Arg.Any<CancellationToken>())
            .Returns([]);
        _userRepository.CreateAsync(Arg.Any<User>(), Password).Returns((true, Array.Empty<string>()));
        _userRepository.AddToRoleAsync(Arg.Any<User>(), nameof(UserRole.Administrator))
            .Returns((false, new[] { "Role Administrator does not exist." }));

        AdminSeedResult result = await _sut.SeedAsync(Email, Password, FirstName, LastName);

        Assert.That(result.Outcome, Is.EqualTo(AdminSeedOutcome.Failed));
        Assert.That(result.Errors, Does.Contain("Role Administrator does not exist."));
    }
}
