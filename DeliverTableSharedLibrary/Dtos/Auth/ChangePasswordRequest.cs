using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Auth;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Votre mot de passe actuel est nécessaire")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Votre nouveau mot de passe ne peut pas être vide")]
    [MinLength(12, ErrorMessage = "Le mot de passe doit contenir au moins 12 caractères")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Veuillez confirmer votre nouveau mot de passe")]
    [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmNewPassword { get; set; } = "";
}
