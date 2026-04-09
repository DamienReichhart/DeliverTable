using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Order;

namespace DeliverTableServer.Mappers;

public static class OrderDiscountMapper
{
    public static OrderDiscountDto ToDto(this OrderDiscount discount)
    {
        return new OrderDiscountDto
        {
            Source = discount.Source.ToString(),
            Description = discount.Description,
            Amount = discount.Amount
        };
    }
}
