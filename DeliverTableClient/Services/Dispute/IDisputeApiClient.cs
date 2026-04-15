using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dispute;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableClient.Services.Dispute;

public interface IDisputeApiClient
{
    Task<PaginatedResult<DisputeRowDto>?> GetForRestaurantAsync(int restaurantId, int page, int pageSize);
    Task<PaginatedResult<AdminDisputeRowDto>?> AdminListAsync(
        DisputeState? state, int? year, int? restaurantId, int? orderId, int page, int pageSize);
    Task<AdminDisputeDetailDto?> AdminGetAsync(int id);
}
