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
        List<ModerationAction> actions = await _moderationRepository.GetAllAsync(ct);
        List<AdminModerationActionResponse> result = actions.Select(a => a.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminModerationActionResponse>> GetByIdAsync(
        int id, CancellationToken ct = default)
    {
        ModerationAction? action = await _moderationRepository.GetByIdAsync(id, ct);
        if (action is null)
            return ServiceError.NotFound(ErrorMessages.ModerationActionNotFound);

        return action.ToAdminDto();
    }

    public async Task<ServiceResult<AdminModerationActionResponse>> CreateAsync(
        AdminCreateModerationActionRequest request, int adminUserId, CancellationToken ct = default)
    {
        ModerationAction action = new ModerationAction
        {
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            ActionType = request.ActionType,
            Reason = request.Reason ?? "",
            AdminUserId = adminUserId
        };

        ModerationAction created = await _moderationRepository.CreateAsync(action, ct);
        return created.ToAdminDto();
    }
}
