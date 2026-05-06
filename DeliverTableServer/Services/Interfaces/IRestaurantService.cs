using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Restaurant;

namespace DeliverTableServer.Services.Interfaces;

public interface IRestaurantService
{
    Task<ServiceResult<PaginatedResult<RestaurantDto>>> GetAllAsync(RestaurantQuery query, CancellationToken ct = default);
    Task<ServiceResult<PaginatedResult<RestaurantDto>>> GetByOwnerAsync(int ownerId, RestaurantQuery query, CancellationToken ct = default);
    Task<ServiceResult<List<RestaurantMapDto>>> GetForMapAsync(RestaurantQuery query, CancellationToken ct = default);
    Task<ServiceResult<DetailedRestaurantDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<RestaurantDto>> CreateAsync(CreateRestaurantDto dto, int ownerId, CancellationToken ct = default);
    Task<ServiceResult<(double lat, double lon)>> ValidateLegalAndLocateAsync(
        string siret, string? legalName, string? legalAddress, string? legalForm,
        string addressLine1, string city, string zipCode);

    Task<ServiceResult<RestaurantDto>> CreateValidatedAsync(
        CreateRestaurantDto dto, int ownerId, (double lat, double lon) coords, CancellationToken ct = default);
    Task<ServiceResult<DetailedRestaurantDto>> UpdateAsync(int id, UpdateRestaurantDto dto, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default);
}
