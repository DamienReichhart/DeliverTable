using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Auth;

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Votre prénom est nécessaire")]
    [MaxLength(50, ErrorMessage = "Le prénom ne peut pas dépasser 50 caractères")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Votre nom de famille est nécessaire")]
    [MaxLength(50, ErrorMessage = "Le nom de famille ne peut pas dépasser 50 caractères")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Votre adresse mail est nécessaire")]
    [EmailAddress(ErrorMessage = "L'adresse mail n'est pas valide")]
    [MaxLength(100, ErrorMessage = "L'adresse mail ne peut pas dépasser 100 caractères")]
    public string Email { get; set; } = "";

    [MaxLength(200, ErrorMessage = "L'adresse ne peut pas dépasser 200 caractères")]
    public string BillingAddressLine1 { get; set; } = "";

    [MaxLength(200, ErrorMessage = "Le complément d'adresse ne peut pas dépasser 200 caractères")]
    public string BillingAddressLine2 { get; set; } = "";

    [MaxLength(10, ErrorMessage = "Le code postal ne peut pas dépasser 10 caractères")]
    public string BillingPostalCode { get; set; } = "";

    [MaxLength(100, ErrorMessage = "La ville ne peut pas dépasser 100 caractères")]
    public string BillingCity { get; set; } = "";

    [MaxLength(100, ErrorMessage = "Le pays ne peut pas dépasser 100 caractères")]
    public string BillingCountry { get; set; } = "";

    public UpdateProfileRequest Clone() => (UpdateProfileRequest)MemberwiseClone();

    public void CopyFrom(UpdateProfileRequest source)
    {
        FirstName = source.FirstName;
        LastName = source.LastName;
        Email = source.Email;
        BillingAddressLine1 = source.BillingAddressLine1;
        BillingAddressLine2 = source.BillingAddressLine2;
        BillingPostalCode = source.BillingPostalCode;
        BillingCity = source.BillingCity;
        BillingCountry = source.BillingCountry;
    }
}
