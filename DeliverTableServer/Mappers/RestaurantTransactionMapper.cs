using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;

namespace DeliverTableServer.Mappers;

public static class RestaurantTransactionMapper
{
    public static RestaurantTransactionDto ToDto(this RestaurantTransaction transaction)
    {
        return new RestaurantTransactionDto
        {
            Id = transaction.Id,
            Type = transaction.Type.ToString(),
            OrderId = transaction.OrderId,
            GrossAmount = transaction.GrossAmount,
            CommissionAmount = transaction.CommissionAmount,
            NetAmount = transaction.NetAmount,
            BalanceAfter = transaction.BalanceAfter,
            CreatedAt = transaction.CreatedAt
        };
    }
}
