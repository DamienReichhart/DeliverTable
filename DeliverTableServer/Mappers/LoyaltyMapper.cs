using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Loyalty;

namespace DeliverTableServer.Mappers;

public static class LoyaltyMapper
{
    public static LoyaltyProgramDto ToDto(this LoyaltyProgram program)
    {
        return new LoyaltyProgramDto
        {
            Id = program.Id,
            RestaurantId = program.RestaurantId,
            PointsPerEuro = program.PointsPerEuro,
            EurosPerPoint = program.EurosPerPoint,
            IsActive = program.IsActive,
            CreatedAt = program.CreatedAt
        };
    }

    public static LoyaltyAccountDto ToDto(this LoyaltyAccount account, LoyaltyProgram program)
    {
        return new LoyaltyAccountDto
        {
            Id = account.Id,
            PointsBalance = account.PointsBalance,
            EuroEquivalent = account.PointsBalance * program.EurosPerPoint,
            PointsPerEuro = program.PointsPerEuro,
            EurosPerPoint = program.EurosPerPoint
        };
    }
}
