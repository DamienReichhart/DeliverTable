using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Auth;

public class RestaurantRegister
{
    [Required] public string FirstName { get; set; } = "";

    [Required] public string LastName { get; set; } = "";
    
    [Required]
    public string CompanyName { get; set; } = "";
    
    [Required]
    public string VatNumber { get; set; } = "";
    
    [Required]
    [MinLength(10, ErrorMessage = "Ce numéro de téléphone n'est pas valide")]
    public string ContactPhoneNumber { get; set; } = "";

    [Required]
    [EmailAddress(ErrorMessage = "L'adresse mail n'est pas valide")]
    public string Email { get; set; } = "";

    [Required]
    [MinLength(12, ErrorMessage = "Le mot de passe doit contenir au moins 12 caractères")]
    public string Password { get; set; } = "";

    [Required]
    [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmPassword { get; set; } = "";
}