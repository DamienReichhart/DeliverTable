using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableSharedLibrary.Dtos.RestaurantAccount;

public class RestaurantAccountDto
{
    public decimal Balance { get; set; }
    public PaginatedResult<RestaurantTransactionDto> Transactions { get; set; } = null!;
}
