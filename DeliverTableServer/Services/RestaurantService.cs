using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Services;

public sealed class RestaurantService(
    IRestaurantRepository restaurantRepository,
    IGeoLocationService geoLocationService
) : IRestaurantService
{
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;
    private readonly IGeoLocationService _geoLocationService = geoLocationService;

    public async Task<ServiceResult<PaginatedResult<RestaurantDto>>> GetAllAsync(
        RestaurantQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _restaurantRepository.GetAllAsync(query, ct);
        return ToPaginatedResult(items, totalCount, query);
    }

    public async Task<ServiceResult<PaginatedResult<RestaurantDto>>> GetByOwnerAsync(
        int ownerId, RestaurantQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _restaurantRepository.GetByOwnerAsync(ownerId, query, ct);
        return ToPaginatedResult(items, totalCount, query);
    }

    public async Task<ServiceResult<DetailedRestaurantDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        return restaurant.ToDetailedDto();
    }

    public async Task<ServiceResult<RestaurantDto>> CreateAsync(
        CreateRestaurantDto dto, int ownerId, CancellationToken ct = default)
    {
        var coords = await _geoLocationService.GetCoordinatesAsync(dto.AdressLine1, dto.City, dto.ZipCode);
        if (coords is null)
            return new ServiceError(ErrorMessages.AddressNotLocatable);

        _ = Enum.TryParse<RestaurantType>(dto.Type, out var restaurantType);

        var restaurant = new Restaurant
        {
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            AdressLine1 = dto.AdressLine1,
            AdressLine2 = dto.AdressLine2 ?? string.Empty,
            City = dto.City,
            ZipCode = dto.ZipCode,
            Type = restaurantType,
            Country = char.ToUpper(dto.Country[0]) + dto.Country[1..],
            OwnerId = ownerId,
            Longitude = coords.Value.lon,
            Latitude = coords.Value.lat
        };

        var created = await _restaurantRepository.CreateAsync(restaurant, ct);
        return created.ToDto();
    }

    public async Task<ServiceResult<DetailedRestaurantDto>> UpdateAsync(
        int id, UpdateRestaurantDto dto, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        var coords = await _geoLocationService.GetCoordinatesAsync(dto.AdressLine1, dto.City, dto.ZipCode);
        if (coords is null)
            return new ServiceError(ErrorMessages.AddressNotLocatable);

        var isValid = Enum.TryParse<RestaurantType>(dto.Type, out var restaurantType);
        if (!isValid) restaurantType = RestaurantType.Autre;

        restaurant.Name = dto.Name;
        restaurant.Description = dto.Description;
        restaurant.Type = restaurantType;
        restaurant.AdressLine1 = dto.AdressLine1;
        restaurant.AdressLine2 = dto.AdressLine2;
        restaurant.City = dto.City;
        restaurant.ZipCode = dto.ZipCode;
        restaurant.Latitude = coords.Value.lat;
        restaurant.Longitude = coords.Value.lon;
        restaurant.UpdatedAt = DateTime.UtcNow;

        var updated = await _restaurantRepository.UpdateAsync(restaurant, ct);
        return updated.ToDetailedDto();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var deleted = await _restaurantRepository.DeleteAsync(id, ct);
        if (!deleted)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        return ServiceResult.Success();
    }

    private static PaginatedResult<RestaurantDto> ToPaginatedResult(
        List<Restaurant> items, int totalCount, RestaurantQuery query)
    {
        return new PaginatedResult<RestaurantDto>
        {
            Items = items.Select(r => r.ToDto()).ToList(),
            TotalCount = totalCount,
            Page = query.PageNumber > 0 ? query.PageNumber : 1,
            PageSize = query.PageSize
        };
    }
}
