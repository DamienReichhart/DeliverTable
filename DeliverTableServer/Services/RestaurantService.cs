using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Extensions;
using DeliverTableServer.Extensions;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using DeliverTableSharedLibrary.Enums;
using DeliverTableSharedLibrary.Validation;

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
        var data = await _restaurantRepository.GetAllAsync(query, ct);
        return data.ToPaginatedResult(r => r.ToDto(), query.PageNumber, query.PageSize);
    }

    public async Task<ServiceResult<List<RestaurantMapDto>>> GetForMapAsync(
        RestaurantQuery query, CancellationToken ct = default)
    {
        var restaurants = await _restaurantRepository.GetForMapAsync(query, ct);
        return restaurants.Select(r => r.ToMapDto()).ToList();
    }

    public async Task<ServiceResult<PaginatedResult<RestaurantDto>>> GetByOwnerAsync(
        int ownerId, RestaurantQuery query, CancellationToken ct = default)
    {
        var data = await _restaurantRepository.GetByOwnerAsync(ownerId, query, ct);
        return data.ToPaginatedResult(r => r.ToDto(), query.PageNumber, query.PageSize);
    }

    public async Task<ServiceResult<DetailedRestaurantDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id, ct);
        if (restaurant is null)
            return ServiceError.NotFound(ErrorMessages.RestaurantNotFound);

        return restaurant.ToDetailedDto();
    }

    public async Task<ServiceResult<RestaurantDto>> CreateAsync(
        CreateRestaurantDto dto, int ownerId, CancellationToken ct = default)
    {
        var legalAndCoords = await ValidateLegalAndLocateAsync(
            dto.Siret, dto.LegalName, dto.LegalAddress, dto.LegalForm,
            dto.AdressLine1, dto.City, dto.ZipCode);
        if (!legalAndCoords.IsSuccess) return legalAndCoords.Error!;
        var coords = legalAndCoords.Value;

        var restaurant = new Restaurant
        {
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            AdressLine1 = dto.AdressLine1,
            AdressLine2 = dto.AdressLine2 ?? string.Empty,
            City = dto.City,
            ZipCode = dto.ZipCode,
            Type = ParseRestaurantType(dto.Type),
            Country = char.ToUpper(dto.Country[0]) + dto.Country[1..],
            OwnerId = ownerId,
            Longitude = coords.lon,
            Latitude = coords.lat,
            Siret = dto.Siret,
            LegalName = dto.LegalName,
            LegalAddress = dto.LegalAddress,
            LegalForm = dto.LegalForm,
            IsVatRegistered = dto.IsVatRegistered
        };

        var created = await _restaurantRepository.CreateAsync(restaurant, ct);
        return created.ToDto();
    }

    public async Task<ServiceResult<DetailedRestaurantDto>> UpdateAsync(
        int id, UpdateRestaurantDto dto, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id, ct);
        if (restaurant is null)
            return ServiceError.NotFound(ErrorMessages.RestaurantNotFound);

        var legalAndCoords = await ValidateLegalAndLocateAsync(
            dto.Siret, dto.LegalName, dto.LegalAddress, dto.LegalForm,
            dto.AdressLine1, dto.City, dto.ZipCode);
        if (!legalAndCoords.IsSuccess) return legalAndCoords.Error!;
        var coords = legalAndCoords.Value;

        restaurant.Name = dto.Name;
        restaurant.Description = dto.Description;
        restaurant.Type = ParseRestaurantType(dto.Type);
        restaurant.AdressLine1 = dto.AdressLine1;
        restaurant.AdressLine2 = dto.AdressLine2;
        restaurant.City = dto.City;
        restaurant.ZipCode = dto.ZipCode;
        restaurant.Latitude = coords.lat;
        restaurant.Longitude = coords.lon;
        restaurant.UpdatedAt = DateTime.UtcNow;
        restaurant.Siret = dto.Siret;
        restaurant.LegalName = dto.LegalName;
        restaurant.LegalAddress = dto.LegalAddress;
        restaurant.LegalForm = dto.LegalForm;
        restaurant.IsVatRegistered = dto.IsVatRegistered;

        var updated = await _restaurantRepository.UpdateAsync(restaurant, ct);
        return updated.ToDetailedDto();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var deleted = await _restaurantRepository.DeleteAsync(id, ct);
        if (!deleted)
            return ServiceError.NotFound(ErrorMessages.RestaurantNotFound);

        return ServiceResult.Success();
    }

    private static RestaurantType ParseRestaurantType(string? value) =>
        Enum.TryParse<RestaurantType>(value, out var type) ? type : RestaurantType.Autre;

    private async Task<ServiceResult<(double lat, double lon)>> ValidateLegalAndLocateAsync(
        string siret, string? legalName, string? legalAddress, string? legalForm,
        string addressLine1, string city, string zipCode)
    {
        if (!SiretValidator.IsValid(siret))
            return new ServiceError(ErrorMessages.SiretInvalid);

        if (string.IsNullOrWhiteSpace(legalName)
            || string.IsNullOrWhiteSpace(legalAddress)
            || string.IsNullOrWhiteSpace(legalForm))
            return new ServiceError(ErrorMessages.LegalFieldsRequired);

        var coords = await _geoLocationService.GetCoordinatesAsync(addressLine1, city, zipCode);
        if (coords is null)
            return new ServiceError(ErrorMessages.AddressNotLocatable);

        return coords.Value;
    }
}
