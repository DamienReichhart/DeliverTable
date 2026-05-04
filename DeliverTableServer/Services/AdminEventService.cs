using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services;

public sealed class AdminEventService(
    IEventRepository eventRepository,
    IUserRepository userRepository,
    IRestaurantRepository restaurantRepository)
    : IAdminEventService
{
    private readonly IEventRepository _eventRepository = eventRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    public async Task<ServiceResult<List<AdminEventResponse>>> GetAllAsync(CancellationToken ct = default)
    {
        var events = await _eventRepository.GetAllAsync(ct);
        var result = events.Select(e => e.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminEventResponse>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var evt = await _eventRepository.GetByIdAsync(id, ct);
        if (evt is null)
            return ServiceError.NotFound(ErrorMessages.EventNotFound);

        return evt.ToAdminDto();
    }

    public async Task<ServiceResult<AdminEventResponse>> CreateAsync(
        AdminCreateEventRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(request.CreatedByUserId, ct);
        if (user is null)
            return ServiceError.NotFound(ErrorMessages.UserNotFound);

        if (request.EndsAt <= request.StartsAt)
            return ServiceError.BadRequest(ErrorMessages.InvalidEventDates);

        if (request.RestaurantId is not null)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId.Value, ct);
            if (restaurant is null)
                return ServiceError.NotFound(ErrorMessages.RestaurantNotFound);
        }

        var evt = new Event
        {
            Name = request.Name,
            Description = request.Description ?? "",
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            MaxGuests = request.MaxGuests,
            Visibility = request.Visibility,
            IsActive = request.IsActive,
            RestaurantId = request.RestaurantId,
            CreatedByUserId = request.CreatedByUserId
        };

        var created = await _eventRepository.CreateAsync(evt, ct);
        return created.ToAdminDto();
    }

    public async Task<ServiceResult<AdminEventResponse>> UpdateAsync(
        int id, AdminUpdateEventRequest request, CancellationToken ct = default)
    {
        var evt = await _eventRepository.GetByIdAsync(id, ct);
        if (evt is null)
            return ServiceError.NotFound(ErrorMessages.EventNotFound);

        if (request.EndsAt <= request.StartsAt)
            return ServiceError.BadRequest(ErrorMessages.InvalidEventDates);

        if (request.RestaurantId is not null)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId.Value, ct);
            if (restaurant is null)
                return ServiceError.NotFound(ErrorMessages.RestaurantNotFound);
        }

        evt.Name = request.Name;
        evt.Description = request.Description ?? "";
        evt.StartsAt = request.StartsAt;
        evt.EndsAt = request.EndsAt;
        evt.MaxGuests = request.MaxGuests;
        evt.Visibility = request.Visibility;
        evt.IsActive = request.IsActive;
        evt.RestaurantId = request.RestaurantId;
        evt.UpdatedAt = DateTime.UtcNow;

        var updated = await _eventRepository.UpdateAsync(evt, ct);
        return updated.ToAdminDto();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var deleted = await _eventRepository.DeleteAsync(id, ct);
        if (!deleted)
            return ServiceError.NotFound(ErrorMessages.EventNotFound);

        return ServiceResult.Success();
    }
}
