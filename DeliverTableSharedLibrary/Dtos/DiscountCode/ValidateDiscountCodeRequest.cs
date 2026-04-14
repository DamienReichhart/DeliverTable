using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.DiscountCode;

public class ValidateDiscountCodeRequest
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
}
