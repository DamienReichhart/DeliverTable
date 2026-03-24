using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services.Interfaces;

public interface IAdminPromotionClientService
{
    Task<(List<AdminPromotionResponse>? Promotions, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default);

    Task<(AdminPromotionResponse? Promotion, ErrorResponse? Error)> GetByIdAsync(
        int id, CancellationToken ct = default);

    Task<(AdminPromotionResponse? Promotion, ErrorResponse? Error)> CreateAsync(
        AdminCreatePromotionRequest request, CancellationToken ct = default);

    Task<(AdminPromotionResponse? Promotion, ErrorResponse? Error)> UpdateAsync(
        int id, AdminUpdatePromotionRequest request, CancellationToken ct = default);

    Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int id, CancellationToken ct = default);
}
