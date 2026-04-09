using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Mappers;

public static class AdminTransactionMapper
{
    public static AdminTransactionResponse ToAdminDto(this RestaurantTransaction transaction)
    {
        return new AdminTransactionResponse
        {
            Id = transaction.Id,
            Type = transaction.Type.ToString(),
            GrossAmount = transaction.GrossAmount,
            CommissionAmount = transaction.CommissionAmount,
            NetAmount = transaction.NetAmount,
            BalanceAfter = transaction.BalanceAfter,
            RestaurantId = transaction.RestaurantId,
            RestaurantName = transaction.Restaurant is not null
                ? transaction.Restaurant.Name
                : "",
            OrderId = transaction.OrderId,
            CreatedAt = transaction.CreatedAt
        };
    }
}
