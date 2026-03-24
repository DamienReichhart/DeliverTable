using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services.Interfaces;

public interface IAdminEventClientService
{
    Task<(List<AdminEventResponse>? Events, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default);

    Task<(AdminEventResponse? Event, ErrorResponse? Error)> GetByIdAsync(
        int id, CancellationToken ct = default);

    Task<(AdminEventResponse? Event, ErrorResponse? Error)> CreateAsync(
        AdminCreateEventRequest request, CancellationToken ct = default);

    Task<(AdminEventResponse? Event, ErrorResponse? Error)> UpdateAsync(
        int id, AdminUpdateEventRequest request, CancellationToken ct = default);

    Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int id, CancellationToken ct = default);
}
