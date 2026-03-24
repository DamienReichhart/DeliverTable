using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services.Interfaces;

public interface IAdminRestaurantClientService
{
    Task<(List<AdminRestaurantResponse>? Restaurants, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default);

    Task<(AdminRestaurantResponse? Restaurant, ErrorResponse? Error)> GetByIdAsync(
        int id, CancellationToken ct = default);

    Task<(AdminRestaurantResponse? Restaurant, ErrorResponse? Error)> UpdateAsync(
        int id, AdminUpdateRestaurantRequest request, CancellationToken ct = default);

    Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int id, CancellationToken ct = default);
}
