using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos;

public class RegisterRequest
{
    [Required] public string FirstName { get; set; } = "";

    [Required] public string LastName { get; set; } = "";

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