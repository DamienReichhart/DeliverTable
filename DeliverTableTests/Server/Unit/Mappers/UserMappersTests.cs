using DeliverTableServer.Mappers;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableTests.Server.Factories;

namespace DeliverTableTests.Server.Unit.Mappers;

[TestFixture]
public class UserMappersTests
{
    [Test]
    public void ToDto_MapsAllFieldsCorrectly()
    {
        var user = ServerEntityFactory.CreateValidUser("map@example.com");
        user.Id = 99;
        user.FirstName = "Marie";
        user.LastName = "Curie";

        var dto = user.ToDto(nameof(UserRole.Customer));

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(99));
            Assert.That(dto.Email, Is.EqualTo("map@example.com"));
            Assert.That(dto.FirstName, Is.EqualTo("Marie"));
            Assert.That(dto.LastName, Is.EqualTo("Curie"));
            Assert.That(dto.Role, Is.EqualTo(nameof(UserRole.Customer)));
        });
    }

    [Test]
    public void ToDto_ReplacesNullEmailWithEmptyString()
    {
        var user = ServerEntityFactory.CreateValidUser();
        user.Email = null;

        var dto = user.ToDto(nameof(UserRole.Customer));

        Assert.That(dto.Email, Is.EqualTo(string.Empty));
    }

    [Test]
    [TestCase(nameof(UserRole.Customer))]
    [TestCase(nameof(UserRole.RestaurantOwner))]
    [TestCase(nameof(UserRole.Administrator))]
    public void ToDto_PreservesRole(string role)
    {
        var user = ServerEntityFactory.CreateValidUser();

        var dto = user.ToDto(role);

        Assert.That(dto.Role, Is.EqualTo(role));
    }
}
