using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services.Interfaces;

public interface IAdminRatingService
{
    Task<ServiceResult<List<AdminRestaurantRatingResponse>>> GetRestaurantRatingsAsync(CancellationToken ct = default);
    Task<ServiceResult<List<AdminCustomerRatingResponse>>> GetCustomerRatingsAsync(CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default);
}
