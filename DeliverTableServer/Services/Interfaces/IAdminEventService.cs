using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services.Interfaces;

public interface IAdminEventService
{
    Task<ServiceResult<List<AdminEventResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResult<AdminEventResponse>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<AdminEventResponse>> CreateAsync(AdminCreateEventRequest request, CancellationToken ct = default);
    Task<ServiceResult<AdminEventResponse>> UpdateAsync(int id, AdminUpdateEventRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default);
}
