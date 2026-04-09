using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services;

public sealed class AdminModerationService(IModerationRepository moderationRepository)
    : IAdminModerationService
{
    private readonly IModerationRepository _moderationRepository = moderationRepository;

    public async Task<ServiceResult<List<AdminModerationActionResponse>>> GetAllAsync(
        CancellationToken ct = default)
    {
        var actions = await _moderationRepository.GetAllAsync(ct);
        var result = actions.Select(a => a.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminModerationActionResponse>> GetByIdAsync(
        int id, CancellationToken ct = default)
    {
        var action = await _moderationRepository.GetByIdAsync(id, ct);
        if (action is null)
            return new ServiceError(ErrorMessages.ModerationActionNotFound, 404);

        return action.ToAdminDto();
    }

    public async Task<ServiceResult<AdminModerationActionResponse>> CreateAsync(
        AdminCreateModerationActionRequest request, int adminUserId, CancellationToken ct = default)
    {
        var action = new ModerationAction
        {
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            ActionType = request.ActionType,
            Reason = request.Reason ?? "",
            AdminUserId = adminUserId
        };

        var created = await _moderationRepository.CreateAsync(action, ct);
        return created.ToAdminDto();
    }
}
