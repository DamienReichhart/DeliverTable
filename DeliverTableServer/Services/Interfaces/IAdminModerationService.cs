using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services.Interfaces;

public interface IAdminModerationService
{
    Task<ServiceResult<List<AdminModerationActionResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResult<AdminModerationActionResponse>> GetByIdAsync(int id, CancellationToken ct = default);

    Task<ServiceResult<AdminModerationActionResponse>> CreateAsync(
        AdminCreateModerationActionRequest request, int adminUserId, CancellationToken ct = default);
}
