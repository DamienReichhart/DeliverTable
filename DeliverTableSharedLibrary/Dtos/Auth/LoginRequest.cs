using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage =  "L'email est requis")]
    [EmailAddress(ErrorMessage = "L'adresse mail ne respecte pas le bon format")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(12, ErrorMessage = "Votre mot d epasse doit contenir au moins 12 caractères")]
    public string Password { get; set; } = "";
}