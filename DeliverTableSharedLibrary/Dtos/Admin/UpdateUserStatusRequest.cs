using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class UpdateUserStatusRequest
{
    [Required(ErrorMessage = "Le statut est requis")]
    public string Status { get; set; } = "";
}
