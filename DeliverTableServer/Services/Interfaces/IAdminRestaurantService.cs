using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services.Interfaces;

public interface IAdminRestaurantService
{
    Task<ServiceResult<List<AdminRestaurantResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResult<AdminRestaurantResponse>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<AdminRestaurantResponse>> UpdateAsync(int id, AdminUpdateRestaurantRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default);
}
