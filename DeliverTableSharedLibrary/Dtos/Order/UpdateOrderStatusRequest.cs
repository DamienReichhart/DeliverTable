using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Order;

public class UpdateOrderStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
