using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services.Interfaces;

public interface IAdminRatingClientService
{
    Task<(List<AdminRestaurantRatingResponse>? Ratings, ErrorResponse? Error)> GetRestaurantRatingsAsync(
        CancellationToken ct = default);

    Task<(List<AdminCustomerRatingResponse>? Ratings, ErrorResponse? Error)> GetCustomerRatingsAsync(
        CancellationToken ct = default);

    Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int id, CancellationToken ct = default);
}
