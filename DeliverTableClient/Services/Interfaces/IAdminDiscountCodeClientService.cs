using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services.Interfaces;

public interface IAdminDiscountCodeClientService
{
    Task<(List<AdminDiscountCodeResponse>? DiscountCodes, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default);

    Task<(AdminDiscountCodeResponse? DiscountCode, ErrorResponse? Error)> GetByIdAsync(
        int id, CancellationToken ct = default);

    Task<(AdminDiscountCodeResponse? DiscountCode, ErrorResponse? Error)> CreateAsync(
        AdminCreateDiscountCodeRequest request, CancellationToken ct = default);

    Task<(AdminDiscountCodeResponse? DiscountCode, ErrorResponse? Error)> UpdateAsync(
        int id, AdminUpdateDiscountCodeRequest request, CancellationToken ct = default);

    Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int id, CancellationToken ct = default);

    Task<(List<AdminRedemptionResponse>? Redemptions, ErrorResponse? Error)> GetRedemptionsAsync(
        int id, CancellationToken ct = default);
}
