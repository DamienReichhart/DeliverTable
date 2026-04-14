using DeliverTableSharedLibrary.Validation;
using NUnit.Framework;

namespace DeliverTableTests.Shared.Unit.Validation;

[TestFixture]
public class SiretValidatorTests
{
    [TestCase("73282932000074", true)]    // SNCF — valid
    [TestCase("40483304800014", true)]    // Air France — valid (corrected check digit)
    [TestCase("12345678900012", false)]   // Luhn-invalid
    [TestCase("12345678", false)]         // too short
    [TestCase("1234567890012345", false)] // too long
    [TestCase("7328293200007A", false)]   // contains letter
    [TestCase("", false)]                 // empty
    [TestCase(null, false)]               // null
    public void IsValid_FixtureCases(string? siret, bool expected)
    {
        Assert.That(SiretValidator.IsValid(siret), Is.EqualTo(expected));
    }
}
