using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Payment;

public class AdminRefundRequest
{
    [Range(0.01, 100000)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
