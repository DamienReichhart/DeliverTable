using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Mappers;

public static class AdminLoyaltyMapper
{
    public static AdminLoyaltyProgramResponse ToAdminDto(this LoyaltyProgram program)
    {
        return new AdminLoyaltyProgramResponse
        {
            Id = program.Id,
            PointsPerEuro = program.PointsPerEuro,
            EurosPerPoint = program.EurosPerPoint,
            IsActive = program.IsActive,
            RestaurantId = program.RestaurantId,
            RestaurantName = program.Restaurant is not null
                ? program.Restaurant.Name
                : "",
            AccountCount = program.Accounts.Count,
            CreatedAt = program.CreatedAt,
            UpdatedAt = program.UpdatedAt
        };
    }

    public static AdminLoyaltyAccountResponse ToAdminDto(this LoyaltyAccount account)
    {
        return new AdminLoyaltyAccountResponse
        {
            Id = account.Id,
            PointsBalance = account.PointsBalance,
            CustomerName = account.Customer is not null
                ? $"{account.Customer.FirstName} {account.Customer.LastName}"
                : "",
            ProgramId = account.LoyaltyProgramId,
            CreatedAt = account.CreatedAt
        };
    }

    public static AdminLoyaltyTransactionResponse ToAdminDto(this LoyaltyTransaction transaction)
    {
        return new AdminLoyaltyTransactionResponse
        {
            Id = transaction.Id,
            Type = transaction.Type,
            Points = transaction.Points,
            OrderId = transaction.OrderId,
            CreatedAt = transaction.CreatedAt
        };
    }
}
