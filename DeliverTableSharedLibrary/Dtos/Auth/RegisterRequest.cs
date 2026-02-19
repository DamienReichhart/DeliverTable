using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "Votre prénom est nécessaire")] public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Votre nom de famille est nécessaire")] public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Votre adresse mail est nécessaire")]
    [EmailAddress(ErrorMessage = "L'adresse mail n'est pas valide")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Votre mot de passe ne peut pas être vide")]
    [MinLength(12, ErrorMessage = "Le mot de passe doit contenir au moins 12 caractères")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Veuillez retaper votre mot de passe")]
    [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmPassword { get; set; } = "";
}