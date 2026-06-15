using DeliverTableInfrastructure.Models;
using DeliverTableServer.Common;

namespace DeliverTableTests.Server.Unit.Common;

[TestFixture]
public class BillingAddressHelperTests
{
    private static User UserWith(
        string line1 = "12 rue de la Paix",
        string line2 = "",
        string postal = "75002",
        string city = "Paris",
        string country = "France") => new()
        {
            FirstName = "Jean",
            LastName = "Dupont",
            Email = "jean@example.fr",
            BillingAddressLine1 = line1,
            BillingAddressLine2 = line2,
            BillingPostalCode = postal,
            BillingCity = city,
            BillingCountry = country,
        };

    [Test]
    public void HasCompleteBillingAddress_AllRequiredPresent_ReturnsTrue()
    {
        Assert.That(BillingAddressHelper.HasCompleteBillingAddress(UserWith()), Is.True);
    }

    [Test]
    public void HasCompleteBillingAddress_LineTwoEmpty_StillTrue()
    {
        Assert.That(BillingAddressHelper.HasCompleteBillingAddress(UserWith(line2: "")), Is.True);
    }

    [TestCase("", "75002", "Paris", "France")]
    [TestCase("12 rue", "", "Paris", "France")]
    [TestCase("12 rue", "75002", "", "France")]
    [TestCase("12 rue", "75002", "Paris", "")]
    [TestCase("   ", "75002", "Paris", "France")]
    public void HasCompleteBillingAddress_AnyRequiredFieldBlank_ReturnsFalse(
        string line1, string postal, string city, string country)
    {
        Assert.That(BillingAddressHelper.HasCompleteBillingAddress(
            UserWith(line1: line1, postal: postal, city: city, country: country)), Is.False);
    }

    [Test]
    public void FormatBillingAddressForSnapshot_FullAddress_ReturnsFourLines()
    {
        User user = UserWith(line2: "Bât. B");
        string formatted = BillingAddressHelper.FormatBillingAddressForSnapshot(user);

        Assert.That(formatted, Is.EqualTo("12 rue de la Paix\nBât. B\n75002 Paris\nFrance"));
    }

    [Test]
    public void FormatBillingAddressForSnapshot_NoLineTwo_ReturnsThreeLines()
    {
        string formatted = BillingAddressHelper.FormatBillingAddressForSnapshot(UserWith());

        Assert.That(formatted, Is.EqualTo("12 rue de la Paix\n75002 Paris\nFrance"));
    }

    [Test]
    public void FormatBillingAddressForSnapshot_AllEmpty_ReturnsEmptyString()
    {
        User user = UserWith(line1: "", postal: "", city: "", country: "");

        Assert.That(BillingAddressHelper.FormatBillingAddressForSnapshot(user), Is.EqualTo(string.Empty));
    }

    [Test]
    public void FormatBillingAddressForSnapshot_TrimsWhitespace()
    {
        User user = UserWith(line1: "  12 rue  ", postal: " 75002 ", city: " Paris ", country: " France ");
        string formatted = BillingAddressHelper.FormatBillingAddressForSnapshot(user);

        Assert.That(formatted, Is.EqualTo("12 rue\n75002 Paris\nFrance"));
    }
}
