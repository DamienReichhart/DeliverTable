using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminUpdateOrderStatusRequest
{
    [Required(ErrorMessage = "Le statut est obligatoire")]
    public string Status { get; set; } = "";
}
