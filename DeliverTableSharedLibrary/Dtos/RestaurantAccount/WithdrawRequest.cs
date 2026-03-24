using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.RestaurantAccount;

public class WithdrawRequest
{
    [Required]
    [Range(0.01, 999999.99)]
    public decimal Amount { get; set; }
}
