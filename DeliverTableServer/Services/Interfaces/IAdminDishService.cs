using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services.Interfaces;

public interface IAdminDishService
{
    Task<ServiceResult<List<AdminDishResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResult<AdminDishResponse>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<AdminDishResponse>> CreateAsync(AdminCreateDishRequest request, CancellationToken ct = default);
    Task<ServiceResult<AdminDishResponse>> UpdateAsync(int id, AdminUpdateDishRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default);
}
