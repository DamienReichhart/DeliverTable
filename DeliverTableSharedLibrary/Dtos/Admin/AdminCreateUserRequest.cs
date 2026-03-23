using System.ComponentModel.DataAnnotations;
using DeliverTableSharedLibrary.Constants.Enums;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminCreateUserRequest
{
    [Required(ErrorMessage = "Le prénom est requis")]
    [MaxLength(50, ErrorMessage = "Le prénom ne peut pas dépasser 50 caractères")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Le nom est requis")]
    [MaxLength(50, ErrorMessage = "Le nom ne peut pas dépasser 50 caractères")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "L'adresse email n'est pas valide")]
    [MaxLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(12, ErrorMessage = "Le mot de passe doit contenir au moins 12 caractères")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Le rôle est requis")]
    public string Role { get; set; } = nameof(UserRole.Customer);
}
