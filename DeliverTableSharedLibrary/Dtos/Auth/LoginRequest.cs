using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage =  "L'email est requis")]
    [EmailAddress(ErrorMessage = "L'adresse mail ou le mot de passe est incorrect")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(12, ErrorMessage = "L'adresse mail ou le mot de passe est incorrect")]
    public string Password { get; set; } = "";
}