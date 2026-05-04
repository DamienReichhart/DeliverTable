using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services;

public sealed class AdminLoyaltyService(ILoyaltyRepository loyaltyRepository, IRestaurantRepository restaurantRepository)
    : IAdminLoyaltyService
{
    private readonly ILoyaltyRepository _loyaltyRepository = loyaltyRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    public async Task<ServiceResult<List<AdminLoyaltyProgramResponse>>> GetAllProgramsAsync(CancellationToken ct = default)
    {
        var programs = await _loyaltyRepository.GetAllProgramsUnscopedAsync(ct);
        var result = programs.Select(p => p.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminLoyaltyProgramResponse>> GetProgramByIdAsync(int id, CancellationToken ct = default)
    {
        var program = await _loyaltyRepository.GetProgramByIdWithAccountsAsync(id, ct);
        if (program is null)
            return ServiceError.NotFound(ErrorMessages.LoyaltyProgramNotFound);

        return program.ToAdminDto();
    }

    public async Task<ServiceResult<AdminLoyaltyProgramResponse>> CreateProgramAsync(
        AdminCreateLoyaltyProgramRequest request, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, ct);
        if (restaurant is null)
            return ServiceError.NotFound(ErrorMessages.RestaurantNotFound);

        var existing = await _loyaltyRepository.GetByRestaurantAsync(request.RestaurantId, ct);
        if (existing is not null)
            return ServiceError.BadRequest(ErrorMessages.LoyaltyProgramAlreadyExists);

        var program = new LoyaltyProgram
        {
            PointsPerEuro = request.PointsPerEuro,
            EurosPerPoint = request.EurosPerPoint,
            RestaurantId = request.RestaurantId,
            IsActive = request.IsActive
        };

        var created = await _loyaltyRepository.CreateAsync(program, ct);
        return created.ToAdminDto();
    }

    public async Task<ServiceResult<AdminLoyaltyProgramResponse>> UpdateProgramAsync(
        int id, AdminUpdateLoyaltyProgramRequest request, CancellationToken ct = default)
    {
        var program = await _loyaltyRepository.GetProgramByIdWithAccountsAsync(id, ct);
        if (program is null)
            return ServiceError.NotFound(ErrorMessages.LoyaltyProgramNotFound);

        program.PointsPerEuro = request.PointsPerEuro;
        program.EurosPerPoint = request.EurosPerPoint;
        program.IsActive = request.IsActive;

        var updated = await _loyaltyRepository.UpdateAsync(program, ct);
        return updated.ToAdminDto();
    }

    public async Task<ServiceResult> DeleteProgramAsync(int id, CancellationToken ct = default)
    {
        var deleted = await _loyaltyRepository.DeleteProgramAsync(id, ct);
        if (!deleted)
            return ServiceError.NotFound(ErrorMessages.LoyaltyProgramNotFound);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<List<AdminLoyaltyAccountResponse>>> GetAccountsAsync(int programId, CancellationToken ct = default)
    {
        var program = await _loyaltyRepository.GetProgramByIdWithAccountsAsync(programId, ct);
        if (program is null)
            return ServiceError.NotFound(ErrorMessages.LoyaltyProgramNotFound);

        var accounts = await _loyaltyRepository.GetAccountsByProgramIdAsync(programId, ct);
        var result = accounts.Select(a => a.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<List<AdminLoyaltyTransactionResponse>>> GetTransactionsAsync(int accountId, CancellationToken ct = default)
    {
        var transactions = await _loyaltyRepository.GetTransactionsByAccountIdAsync(accountId, ct);
        var result = transactions.Select(t => t.ToAdminDto()).ToList();
        return result;
    }
}
