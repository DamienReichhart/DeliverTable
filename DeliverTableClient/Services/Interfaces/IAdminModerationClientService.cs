using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services.Interfaces;

public interface IAdminModerationClientService
{
    Task<(List<AdminModerationActionResponse>? Actions, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default);

    Task<(AdminModerationActionResponse? Action, ErrorResponse? Error)> GetByIdAsync(
        int id, CancellationToken ct = default);

    Task<(AdminModerationActionResponse? Action, ErrorResponse? Error)> CreateAsync(
        AdminCreateModerationActionRequest request, CancellationToken ct = default);
}
