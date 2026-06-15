using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Extensions;
using DeliverTableServer.Helpers;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Dish;
using DeliverTableSharedLibrary.Dtos.Event;

namespace DeliverTableServer.Services;

public sealed class RestaurantEventService(
    IEventRepository eventRepository,
    IRestaurantRepository restaurantRepository,
    IDishRepository dishRepository
) : IRestaurantEventService
{
    private readonly IEventRepository _eventRepository = eventRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;
    private readonly IDishRepository _dishRepository = dishRepository;

    public async Task<ServiceResult<List<RestaurantEventResponse>>> GetByRestaurantAsync(
        int restaurantId, int ownerId, CancellationToken ct = default)
    {
        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, restaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        var events = await _eventRepository.GetByRestaurantAsync(restaurantId, ct);
        return events.Select(e => e.ToRestaurantDto()).ToList();
    }

    public async Task<ServiceResult<RestaurantEventResponse>> GetByIdAsync(
        int eventId, int ownerId, CancellationToken ct = default)
    {
        var evt = await _eventRepository.GetByIdAsync(eventId, ct);
        if (evt is null || evt.RestaurantId is null)
            return ServiceError.NotFound(ErrorMessages.EventNotFound);

        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, evt.RestaurantId.Value, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        return evt.ToRestaurantDto();
    }

    public async Task<ServiceResult<RestaurantEventResponse>> CreateAsync(
        int restaurantId, int ownerId, CreateRestaurantEventRequest request, CancellationToken ct = default)
    {
        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, restaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        if (request.EndsAt <= request.StartsAt)
            return ServiceError.BadRequest(ErrorMessages.InvalidEventDates);

        var dishIds = request.MenuItems.Select(mi => mi.DishId).ToList();
        if (await ValidateDishesAsync(restaurantId, dishIds, ct) is { } dishError)
            return dishError;

        var evt = new Event
        {
            RestaurantId = restaurantId,
            CreatedByUserId = ownerId,
            Name = request.Name,
            Description = request.Description ?? "",
            StartsAt = request.StartsAt.ToUtc(),
            EndsAt = request.EndsAt.ToUtc(),
            MaxGuests = request.MaxGuests,
            IsActive = request.IsActive,
            EventMenuItems = request.MenuItems
                .Select(mi => new EventMenuItem { DishId = mi.DishId, OverridePrice = mi.OverridePrice })
                .ToList()
        };

        var created = await _eventRepository.CreateAsync(evt, ct);
        var reloaded = await _eventRepository.GetByIdAsync(created.Id, ct);
        return (reloaded ?? created).ToRestaurantDto();
    }

    public async Task<ServiceResult<RestaurantEventResponse>> UpdateAsync(
        int eventId, int ownerId, UpdateRestaurantEventRequest request, CancellationToken ct = default)
    {
        var evt = await _eventRepository.GetByIdAsync(eventId, ct);
        if (evt is null || evt.RestaurantId is null)
            return ServiceError.NotFound(ErrorMessages.EventNotFound);

        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, evt.RestaurantId.Value, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        if (request.EndsAt <= request.StartsAt)
            return ServiceError.BadRequest(ErrorMessages.InvalidEventDates);

        var dishIds = request.MenuItems.Select(mi => mi.DishId).ToList();
        if (await ValidateDishesAsync(evt.RestaurantId.Value, dishIds, ct) is { } dishError)
            return dishError;

        evt.Name = request.Name;
        evt.Description = request.Description ?? "";
        evt.StartsAt = request.StartsAt.ToUtc();
        evt.EndsAt = request.EndsAt.ToUtc();
        evt.MaxGuests = request.MaxGuests;
        evt.IsActive = request.IsActive;
        evt.UpdatedAt = DateTime.UtcNow;
        evt.EventMenuItems.Clear();
        evt.EventMenuItems.AddRange(request.MenuItems
            .Select(mi => new EventMenuItem { DishId = mi.DishId, OverridePrice = mi.OverridePrice }));

        var updated = await _eventRepository.UpdateAsync(evt, ct);
        var reloaded = await _eventRepository.GetByIdAsync(updated.Id, ct);
        return (reloaded ?? updated).ToRestaurantDto();
    }

    public async Task<ServiceResult> DeleteAsync(int eventId, int ownerId, CancellationToken ct = default)
    {
        var evt = await _eventRepository.GetByIdAsync(eventId, ct);
        if (evt is null || evt.RestaurantId is null)
            return ServiceError.NotFound(ErrorMessages.EventNotFound);

        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, evt.RestaurantId.Value, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        await _eventRepository.DeleteAsync(eventId, ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<List<RestaurantEventResponse>>> GetActiveByRestaurantAsync(
        int restaurantId, CancellationToken ct = default)
    {
        var events = await _eventRepository.GetActiveByRestaurantAsync(restaurantId, DateTime.UtcNow, ct);
        return events.Select(e => e.ToRestaurantDto()).ToList();
    }

    private async Task<ServiceError?> ValidateDishesAsync(
        int restaurantId, IReadOnlyList<int> dishIds, CancellationToken ct)
    {
        if (dishIds.Count == 0)
            return null;

        var (restaurantDishes, _) = await _dishRepository.GetByRestaurantIdAsync(new DishQuery(), restaurantId, ct);
        var restaurantDishIds = restaurantDishes.Select(d => d.Id).ToHashSet();
        if (dishIds.Any(id => !restaurantDishIds.Contains(id)))
            return new ServiceError(ErrorMessages.EventDishNotFromRestaurant);

        return null;
    }
}
