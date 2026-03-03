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
}
