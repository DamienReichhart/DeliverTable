using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services.Interfaces;

public interface IAdminDashboardClientService
{
    Task<(AdminDashboardStatsResponse? Stats, ErrorResponse? Error)> GetStatsAsync(
        CancellationToken ct = default);

    Task<(AdminDashboardAnalyticsResponse? Analytics, ErrorResponse? Error)> GetAnalyticsAsync(
        CancellationToken ct = default);
}
