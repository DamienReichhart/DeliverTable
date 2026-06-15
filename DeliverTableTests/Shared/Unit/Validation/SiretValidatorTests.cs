using System.ComponentModel.DataAnnotations;
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

    [Test]
    public void SiretAttribute_WhenValid_ReturnsSuccess()
    {
        ValidationContext context = new ValidationContext(new object()) { MemberName = "Siret" };

        Assert.That(new SiretAttribute().GetValidationResult("73282932000074", context), Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void SiretAttribute_WhenInvalid_ReturnsErrorWithMemberName()
    {
        ValidationContext context = new ValidationContext(new object()) { MemberName = "Siret" };

        ValidationResult? result = new SiretAttribute().GetValidationResult("12345678900012", context);

        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
        Assert.That(result!.MemberNames, Does.Contain("Siret"));
    }
}
