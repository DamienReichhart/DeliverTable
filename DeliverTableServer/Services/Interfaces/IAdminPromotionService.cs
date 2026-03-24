using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services.Interfaces;

public interface IAdminPromotionService
{
    Task<ServiceResult<List<AdminPromotionResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResult<AdminPromotionResponse>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<AdminPromotionResponse>> CreateAsync(AdminCreatePromotionRequest request, CancellationToken ct = default);
    Task<ServiceResult<AdminPromotionResponse>> UpdateAsync(int id, AdminUpdatePromotionRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default);
}
