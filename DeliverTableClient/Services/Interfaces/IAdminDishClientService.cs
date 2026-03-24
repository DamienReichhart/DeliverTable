using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services.Interfaces;

public interface IAdminDishClientService
{
    Task<(List<AdminDishResponse>? Dishes, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default);

    Task<(AdminDishResponse? Dish, ErrorResponse? Error)> GetByIdAsync(
        int id, CancellationToken ct = default);

    Task<(AdminDishResponse? Dish, ErrorResponse? Error)> CreateAsync(
        AdminCreateDishRequest request, CancellationToken ct = default);

    Task<(AdminDishResponse? Dish, ErrorResponse? Error)> UpdateAsync(
        int id, AdminUpdateDishRequest request, CancellationToken ct = default);

    Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int id, CancellationToken ct = default);
}
