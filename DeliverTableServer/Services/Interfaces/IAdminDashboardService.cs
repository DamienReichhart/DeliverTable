using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services.Interfaces;

public interface IAdminDashboardService
{
    Task<ServiceResult<AdminDashboardStatsResponse>> GetStatsAsync(CancellationToken ct = default);
    Task<ServiceResult<AdminDashboardAnalyticsResponse>> GetAnalyticsAsync(CancellationToken ct = default);
}
