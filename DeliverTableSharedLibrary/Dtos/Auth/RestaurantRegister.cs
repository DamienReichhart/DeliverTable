using System.ComponentModel.DataAnnotations;
using DeliverTableSharedLibrary.Dtos.Restaurant;

namespace DeliverTableSharedLibrary.Dtos.Auth;

public class RestaurantRegister
{
    [Required(ErrorMessage = "Le prénom est requis")]
    [MaxLength(50, ErrorMessage = "Le prénom ne peut pas dépasser 50 caractères")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Le nom est requis")]
    [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Le numéro de téléphone est requis")]
    [MinLength(10, ErrorMessage = "Ce numéro de téléphone n'est pas valide")]
    [MaxLength(20, ErrorMessage = "Le numéro de téléphone ne peut pas dépasser 20 caractères")]
    public string ContactPhoneNumber { get; set; } = "";

    [Required(ErrorMessage = "L'adresse mail est requise")]
    [MaxLength(50, ErrorMessage = "L'adresse mail ne peut pas dépasser 50 caractères")]
    [EmailAddress(ErrorMessage = "L'adresse mail n'est pas valide")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(12, ErrorMessage = "Le mot de passe doit contenir au moins 12 caractères")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Veuillez confirmer votre mot de passe")]
    [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Les informations du restaurant sont requises")]
    public CreateRestaurantDto Restaurant { get; set; } = new();
}
