using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Helpers;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Loyalty;

namespace DeliverTableServer.Services;

public sealed class LoyaltyService(
    ILoyaltyRepository loyaltyRepository,
    IRestaurantRepository restaurantRepository
) : ILoyaltyService
{
    private readonly ILoyaltyRepository _loyaltyRepository = loyaltyRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    public async Task<ServiceResult<LoyaltyProgramDto>> CreateOrUpdateProgramAsync(
        int restaurantId, int ownerId, CreateLoyaltyProgramRequest request, CancellationToken ct = default)
    {
        ServiceResult<Restaurant> ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, restaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        LoyaltyProgram? existing = await _loyaltyRepository.GetByRestaurantAsync(restaurantId, ct);

        if (existing is not null)
        {
            existing.PointsPerEuro = request.PointsPerEuro;
            existing.EurosPerPoint = request.EurosPerPoint;
            existing.UpdatedAt = DateTime.UtcNow;

            LoyaltyProgram updated = await _loyaltyRepository.UpdateAsync(existing, ct);
            return updated.ToDto();
        }

        LoyaltyProgram program = new LoyaltyProgram
        {
            RestaurantId = restaurantId,
            PointsPerEuro = request.PointsPerEuro,
            EurosPerPoint = request.EurosPerPoint
        };

        LoyaltyProgram created = await _loyaltyRepository.CreateAsync(program, ct);
        return created.ToDto();
    }

    public async Task<ServiceResult<LoyaltyProgramDto>> GetProgramAsync(
        int restaurantId, CancellationToken ct = default)
    {
        LoyaltyProgram? program = await _loyaltyRepository.GetByRestaurantAsync(restaurantId, ct);
        if (program is null)
            return ServiceError.NotFound(ErrorMessages.LoyaltyProgramNotFound);

        return program.ToDto();
    }

    public async Task<ServiceResult<LoyaltyAccountDto>> GetMyAccountAsync(
        int restaurantId, int customerId, CancellationToken ct = default)
    {
        LoyaltyProgram? program = await _loyaltyRepository.GetByRestaurantAsync(restaurantId, ct);
        if (program is null)
            return ServiceError.NotFound(ErrorMessages.LoyaltyProgramNotFound);

        LoyaltyAccount? account = await _loyaltyRepository.GetAccountAsync(program.Id, customerId, ct);

        if (account is null)
        {
            account = new LoyaltyAccount
            {
                LoyaltyProgramId = program.Id,
                CustomerId = customerId,
                PointsBalance = 0
            };

            account = await _loyaltyRepository.CreateAccountAsync(account, ct);
        }

        return account.ToDto(program);
    }
}
