using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services.Interfaces;

public interface IAdminDiscountCodeService
{
    Task<ServiceResult<List<AdminDiscountCodeResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResult<AdminDiscountCodeResponse>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<AdminDiscountCodeResponse>> CreateAsync(AdminCreateDiscountCodeRequest request, CancellationToken ct = default);
    Task<ServiceResult<AdminDiscountCodeResponse>> UpdateAsync(int id, AdminUpdateDiscountCodeRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<List<AdminRedemptionResponse>>> GetRedemptionsAsync(int discountCodeId, CancellationToken ct = default);
}
