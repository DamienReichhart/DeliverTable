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
        List<LoyaltyProgram> programs = await _loyaltyRepository.GetAllProgramsUnscopedAsync(ct);
        List<AdminLoyaltyProgramResponse> result = programs.Select(p => p.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminLoyaltyProgramResponse>> GetProgramByIdAsync(int id, CancellationToken ct = default)
    {
        LoyaltyProgram? program = await _loyaltyRepository.GetProgramByIdWithAccountsAsync(id, ct);
        if (program is null)
            return ServiceError.NotFound(ErrorMessages.LoyaltyProgramNotFound);

        return program.ToAdminDto();
    }

    public async Task<ServiceResult<AdminLoyaltyProgramResponse>> CreateProgramAsync(
        AdminCreateLoyaltyProgramRequest request, CancellationToken ct = default)
    {
        Restaurant? restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, ct);
        if (restaurant is null)
            return ServiceError.NotFound(ErrorMessages.RestaurantNotFound);

        LoyaltyProgram? existing = await _loyaltyRepository.GetByRestaurantAsync(request.RestaurantId, ct);
        if (existing is not null)
            return ServiceError.BadRequest(ErrorMessages.LoyaltyProgramAlreadyExists);

        LoyaltyProgram program = new LoyaltyProgram
        {
            PointsPerEuro = request.PointsPerEuro,
            EurosPerPoint = request.EurosPerPoint,
            RestaurantId = request.RestaurantId,
            IsActive = request.IsActive
        };

        LoyaltyProgram created = await _loyaltyRepository.CreateAsync(program, ct);
        return created.ToAdminDto();
    }

    public async Task<ServiceResult<AdminLoyaltyProgramResponse>> UpdateProgramAsync(
        int id, AdminUpdateLoyaltyProgramRequest request, CancellationToken ct = default)
    {
        LoyaltyProgram? program = await _loyaltyRepository.GetProgramByIdWithAccountsAsync(id, ct);
        if (program is null)
            return ServiceError.NotFound(ErrorMessages.LoyaltyProgramNotFound);

        program.PointsPerEuro = request.PointsPerEuro;
        program.EurosPerPoint = request.EurosPerPoint;
        program.IsActive = request.IsActive;

        LoyaltyProgram updated = await _loyaltyRepository.UpdateAsync(program, ct);
        return updated.ToAdminDto();
    }

    public async Task<ServiceResult> DeleteProgramAsync(int id, CancellationToken ct = default)
    {
        bool deleted = await _loyaltyRepository.DeleteProgramAsync(id, ct);
        if (!deleted)
            return ServiceError.NotFound(ErrorMessages.LoyaltyProgramNotFound);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<List<AdminLoyaltyAccountResponse>>> GetAccountsAsync(int programId, CancellationToken ct = default)
    {
        LoyaltyProgram? program = await _loyaltyRepository.GetProgramByIdWithAccountsAsync(programId, ct);
        if (program is null)
            return ServiceError.NotFound(ErrorMessages.LoyaltyProgramNotFound);

        List<LoyaltyAccount> accounts = await _loyaltyRepository.GetAccountsByProgramIdAsync(programId, ct);
        List<AdminLoyaltyAccountResponse> result = accounts.Select(a => a.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<List<AdminLoyaltyTransactionResponse>>> GetTransactionsAsync(int accountId, CancellationToken ct = default)
    {
        List<LoyaltyTransaction> transactions = await _loyaltyRepository.GetTransactionsByAccountIdAsync(accountId, ct);
        List<AdminLoyaltyTransactionResponse> result = transactions.Select(t => t.ToAdminDto()).ToList();
        return result;
    }
}
