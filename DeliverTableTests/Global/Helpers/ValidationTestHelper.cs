using System.ComponentModel.DataAnnotations;

namespace DeliverTableTests.Global.Helpers;

/// <summary>
///     Reusable validation utilities for testing DTOs decorated with
///     <see cref="System.ComponentModel.DataAnnotations" /> attributes.
///     Centralizes <see cref="Validator" /> calls so individual test classes stay focused on scenarios.
/// </summary>
public static class ValidationTestHelper
{
    /// <summary>
    ///     Validates <paramref name="model" /> against all DataAnnotation attributes.
    /// </summary>
    /// <returns>Every <see cref="ValidationResult" /> produced; empty when the model is valid.</returns>
    public static IList<ValidationResult> Validate(object model)
    {
        List<ValidationResult> results = new List<ValidationResult>();
        ValidationContext context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    /// <summary>Asserts the model passes all validation rules.</summary>
    public static void AssertValid(object model)
    {
        IList<ValidationResult> results = Validate(model);
        Assert.That(results, Is.Empty,
            $"Expected valid model but got: {FormatErrors(results)}");
    }

    /// <summary>Asserts that <paramref name="memberName" /> has at least one validation error.</summary>
    public static void AssertHasError(object model, string memberName)
    {
        IList<ValidationResult> results = Validate(model);
        Assert.That(results.Any(r => r.MemberNames.Contains(memberName)), Is.True,
            $"Expected error on '{memberName}' but got: {FormatErrors(results)}");
    }

    /// <summary>
    ///     Asserts that <paramref name="memberName" /> has a validation error whose message
    ///     contains <paramref name="expectedSubstring" />.
    /// </summary>
    public static void AssertHasErrorContaining(
        object model,
        string memberName,
        string expectedSubstring)
    {
        IList<ValidationResult> results = Validate(model);
        ValidationResult? match = results.FirstOrDefault(r =>
            r.MemberNames.Contains(memberName) &&
            (r.ErrorMessage?.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase) ?? false));

        Assert.That(match, Is.Not.Null,
            $"Expected error on '{memberName}' containing '{expectedSubstring}'. Got: {FormatErrors(results)}");
    }

    /// <summary>Asserts that <paramref name="memberName" /> has no validation errors.</summary>
    public static void AssertNoError(object model, string memberName)
    {
        IList<ValidationResult> results = Validate(model);
        Assert.That(results.Any(r => r.MemberNames.Contains(memberName)), Is.False,
            $"Expected no error on '{memberName}' but found: {FormatErrors(results)}");
    }

    private static string FormatErrors(IList<ValidationResult> results)
    {
        if (results.Count == 0) return "(none)";
        return string.Join("; ", results.Select(r =>
            $"[{string.Join(", ", r.MemberNames)}] {r.ErrorMessage}"));
    }
}
