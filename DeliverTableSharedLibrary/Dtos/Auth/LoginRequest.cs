using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "L'adresse mail n'est pas valide")]
    [MaxLength(100, ErrorMessage = "L'adresse mail ne peut pas dépasser 100 caractères")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(12, ErrorMessage = "Le mot de passe doit contenir au moins 12 caractères")]
    public string Password { get; set; } = "";
}