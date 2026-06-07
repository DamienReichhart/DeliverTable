using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SiretAttribute : ValidationAttribute
{
    public SiretAttribute() => ErrorMessage = "Le Siret n'est pas valide";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (SiretValidator.IsValid(value as string)) return ValidationResult.Success;

        return new ValidationResult(
            ErrorMessage,
            validationContext.MemberName is null ? null : [validationContext.MemberName]);
    }
}
