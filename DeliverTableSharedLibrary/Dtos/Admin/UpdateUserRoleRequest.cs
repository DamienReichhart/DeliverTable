using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class UpdateUserRoleRequest
{
    [Required(ErrorMessage = "Le rôle est requis")]
    public string Role { get; set; } = "";
}
