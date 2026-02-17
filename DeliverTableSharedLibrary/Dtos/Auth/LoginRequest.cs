using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Auth;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress(ErrorMessage = "Invalid Credentials")]
    public string Email { get; set; } = "";

    [Required]
    [MinLength(12, ErrorMessage = "Invalid Credentials")]
    public string Password { get; set; } = "";
}