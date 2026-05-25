using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Helpers;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using System.Text.Json;

namespace DeliverTableServer.Services;

public sealed class RestaurantOrderConfigService(
    IOrderConfigRepository orderConfigRepository,
    IRestaurantRepository restaurantRepository,
    IOrderRepository orderRepository)
    : IRestaurantOrderConfigService
{
    private readonly IOrderConfigRepository _orderConfigRepository = orderConfigRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;
    private readonly IOrderRepository _orderRepository = orderRepository;

    public async Task<ServiceResult<List<AdminBlockedSlotResponse>>> GetBlockedSlotsAsync(
        int restaurantId,
        int ownerId,
        CancellationToken ct = default)
    {
        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, restaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        var blockedSlots = await _orderConfigRepository.GetBlockedSlotsByRestaurantAsync(restaurantId, ct);
        var response = blockedSlots.Select(slot => slot.ToAdminDto()).ToList();

        return response;
    }

    public async Task<ServiceResult<AdminBlockedSlotResponse>> CreateBlockedSlotAsync(
        int restaurantId,
        int ownerId,
        AdminCreateBlockedSlotRequest request,
        CancellationToken ct = default)
    {
        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, restaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        if (request.EndsAt <= request.StartsAt)
            return new ServiceError(ErrorMessages.InvalidBlockedSlotDates, 400);

        var overlapExists = await _orderConfigRepository.ExistsBlockedSlotOverlapAsync(
            restaurantId,
            request.RestaurantTableId,
            request.StartsAt,
            request.EndsAt,
            ct);

        if (overlapExists)
            return new ServiceError(ErrorMessages.BlockedSlotOverlapExists, 400);

        var blockedSlot = new OrderBlockedSlot
        {
            RestaurantId = restaurantId,
            RestaurantTableId = request.RestaurantTableId,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Reason = request.Reason
        };

        var createdBlockedSlot = await _orderConfigRepository.CreateBlockedSlotAsync(blockedSlot, ct);
        return createdBlockedSlot.ToAdminDto();
    }

    public async Task<ServiceResult> DeleteBlockedSlotAsync(
        int restaurantId,
        int slotId,
        int ownerId,
        CancellationToken ct = default)
    {
        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, restaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        var blockedSlot = await _orderConfigRepository.GetBlockedSlotByIdAsync(slotId, ct);
        if (blockedSlot is null || blockedSlot.RestaurantId != restaurantId)
            return new ServiceError(ErrorMessages.BlockedSlotNotFound, 404);

        var isDeleted = await _orderConfigRepository.DeleteBlockedSlotAsync(slotId, ct);
        if (!isDeleted)
            return new ServiceError(ErrorMessages.BlockedSlotNotFound, 404);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<TablesCapacityResponse>> GetTablesCapacityAsync(
        int restaurantId,
        int ownerId,
        CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId, ct);
        if (restaurant is null || !restaurant.IsActive)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        var rule = await _orderConfigRepository.GetRuleByRestaurantIdAsync(restaurantId, ct);
        var fallbackCount = await _restaurantRepository.CountActiveTablesByMaxCapacityAsync(restaurantId, 2, ct);

        return new TablesCapacityResponse
        {
            RestaurantId = restaurantId,
            CapacityPerSlot = rule?.TablesCapacityPerSlot ?? fallbackCount,
            ActiveTablesFallback = fallbackCount
        };
    }

    public async Task<ServiceResult<TablesCapacityResponse>> UpdateTablesCapacityAsync(
        int restaurantId,
        int ownerId,
        UpdateTablesCapacityRequest request,
        CancellationToken ct = default)
    {
        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, restaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        var rule = await _orderConfigRepository.GetRuleByRestaurantIdAsync(restaurantId, ct);
        if (rule is null)
        {
            rule = new OrderRule
            {
                RestaurantId = restaurantId,
                TablesCapacityPerSlot = request.CapacityPerSlot,
                AvailabilityRanges = string.Empty,
                AllowDelivery = true,
                AllowPreorder = true
            };

            await _orderConfigRepository.CreateRuleAsync(rule, ct);
        }
        else
        {
            rule.TablesCapacityPerSlot = request.CapacityPerSlot;
            rule.UpdatedAt = DateTime.UtcNow;
            await _orderConfigRepository.UpdateRuleAsync(rule, ct);
        }

        var fallbackCount = await _restaurantRepository.CountActiveTablesByMaxCapacityAsync(restaurantId, 2, ct);

        return new TablesCapacityResponse
        {
            RestaurantId = restaurantId,
            CapacityPerSlot = request.CapacityPerSlot,
            ActiveTablesFallback = fallbackCount
        };
    }

    public async Task<ServiceResult<RestaurantOpeningHoursResponse>> GetOpeningHoursAsync(
        int restaurantId,
        int ownerId,
        CancellationToken ct = default)
    {
        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, restaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        var rule = await _orderConfigRepository.GetRuleByRestaurantIdAsync(restaurantId, ct);
        var slotDuration = rule?.SlotDurationMinutes is > 0 ? rule.SlotDurationMinutes.Value : 60;
        var days = ParseOpeningHours(rule?.AvailabilityRanges);

        return new RestaurantOpeningHoursResponse
        {
            RestaurantId = restaurantId,
            SlotDurationMinutes = slotDuration,
            Days = days
        };
    }

    public async Task<ServiceResult<RestaurantOpeningHoursResponse>> UpdateOpeningHoursAsync(
        int restaurantId,
        int ownerId,
        UpdateRestaurantOpeningHoursRequest request,
        CancellationToken ct = default)
    {
        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, restaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        if (!ValidateOpeningHours(request.Days))
            return new ServiceError(ErrorMessages.InvalidOpeningHours, 400);

        var serializedDays = JsonSerializer.Serialize(request.Days);

        var rule = await _orderConfigRepository.GetRuleByRestaurantIdAsync(restaurantId, ct);
        if (rule is null)
        {
            rule = new OrderRule
            {
                RestaurantId = restaurantId,
                SlotDurationMinutes = request.SlotDurationMinutes,
                AvailabilityRanges = serializedDays,
                AllowDelivery = true,
                AllowPreorder = true
            };

            await _orderConfigRepository.CreateRuleAsync(rule, ct);
        }
        else
        {
            rule.SlotDurationMinutes = request.SlotDurationMinutes;
            rule.AvailabilityRanges = serializedDays;
            rule.UpdatedAt = DateTime.UtcNow;
            await _orderConfigRepository.UpdateRuleAsync(rule, ct);
        }

        return new RestaurantOpeningHoursResponse
        {
            RestaurantId = restaurantId,
            SlotDurationMinutes = request.SlotDurationMinutes,
            Days = request.Days
        };
    }

    public async Task<ServiceResult<RestaurantAvailableSlotsResponse>> GetAvailableSlotsAsync(
        int restaurantId,
        RestaurantAvailableSlotsQuery query,
        CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId, ct);
        if (restaurant is null || !restaurant.IsActive)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        var rule = await _orderConfigRepository.GetRuleByRestaurantIdAsync(restaurantId, ct);
        var slotDurationMinutes = rule?.SlotDurationMinutes is > 0 ? rule.SlotDurationMinutes.Value : 60;
        var days = ParseOpeningHours(rule?.AvailabilityRanges);

        var date = query.Date.Date;
        var daySchedule = days.FirstOrDefault(d => d.DayOfWeek == (int)date.DayOfWeek);
        var availableSlots = new List<AvailableSlotDto>();

        if (daySchedule is null || daySchedule.Slots.Count == 0)
        {
            return new RestaurantAvailableSlotsResponse
            {
                RestaurantId = restaurantId,
                Date = date,
                SlotDurationMinutes = slotDurationMinutes,
                Slots = availableSlots
            };
        }

        var tablesCapacity = await GetTablesCapacityPerSlotAsync(restaurantId, rule, ct);
        var requiredTableUnits = GetRequiredTableUnits(query.GuestCount);

        if (tablesCapacity <= 0 || requiredTableUnits > tablesCapacity)
        {
            return new RestaurantAvailableSlotsResponse
            {
                RestaurantId = restaurantId,
                Date = date,
                SlotDurationMinutes = slotDurationMinutes,
                Slots = availableSlots
            };
        }

        foreach (var range in daySchedule.Slots)
        {
            if (!TimeOnly.TryParse(range.StartTime, out var startTime)
                || !TimeOnly.TryParse(range.EndTime, out var endTime)
                || endTime <= startTime)
            {
                continue;
            }

            var rangeStart = DateTime.SpecifyKind(date.Add(startTime.ToTimeSpan()), DateTimeKind.Unspecified);
            var rangeEnd = DateTime.SpecifyKind(date.Add(endTime.ToTimeSpan()), DateTimeKind.Unspecified);

            for (var slotStart = rangeStart;
                 slotStart.AddMinutes(slotDurationMinutes) <= rangeEnd;
                 slotStart = slotStart.AddMinutes(slotDurationMinutes))
            {
                var slotEnd = slotStart.AddMinutes(slotDurationMinutes);

                var slotStartUtc = TimeZoneInfo.ConvertTimeToUtc(slotStart, TimeZoneInfo.Local);
                var slotEndUtc = TimeZoneInfo.ConvertTimeToUtc(slotEnd, TimeZoneInfo.Local);

                var isBlocked = await _orderConfigRepository.IsRestaurantLevelSlotBlockedAsync(
                    restaurantId,
                    slotStartUtc,
                    slotEndUtc,
                    ct);

                if (isBlocked)
                    continue;

                var reservedTableUnits = await _orderRepository.GetScheduledDineInReservedTableUnitsOverlappingAsync(
                    restaurantId,
                    slotStartUtc,
                    slotEndUtc,
                    ct);

                if (reservedTableUnits + requiredTableUnits > tablesCapacity)
                    continue;

                availableSlots.Add(new AvailableSlotDto
                {
                    StartsAt = slotStart,
                    EndsAt = slotEnd
                });
            }
        }

        return new RestaurantAvailableSlotsResponse
        {
            RestaurantId = restaurantId,
            Date = date,
            SlotDurationMinutes = slotDurationMinutes,
            Slots = availableSlots
        };
    }

    private static List<OpeningDayScheduleDto> ParseOpeningHours(string? rawAvailabilityRanges)
    {
        if (string.IsNullOrWhiteSpace(rawAvailabilityRanges))
            return BuildDefaultDays();

        try
        {
            var parsed = JsonSerializer.Deserialize<List<OpeningDayScheduleDto>>(rawAvailabilityRanges);
            if (parsed is null || parsed.Count == 0)
                return BuildDefaultDays();

            return NormalizeDays(parsed);
        }
        catch
        {
            return BuildDefaultDays();
        }
    }

    private static List<OpeningDayScheduleDto> BuildDefaultDays()
    {
        var result = new List<OpeningDayScheduleDto>(7);
        for (var day = 0; day <= 6; day++)
        {
            result.Add(new OpeningDayScheduleDto
            {
                DayOfWeek = day,
                Slots = []
            });
        }

        return result;
    }

    private static List<OpeningDayScheduleDto> NormalizeDays(List<OpeningDayScheduleDto> days)
    {
        var byDay = days
            .Where(d => d.DayOfWeek is >= 0 and <= 6)
            .GroupBy(d => d.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new List<OpeningDayScheduleDto>(7);
        for (var day = 0; day <= 6; day++)
        {
            if (byDay.TryGetValue(day, out var value))
            {
                value.Slots ??= [];
                result.Add(value);
            }
            else
            {
                result.Add(new OpeningDayScheduleDto { DayOfWeek = day, Slots = [] });
            }
        }

        return result;
    }

    private static bool ValidateOpeningHours(List<OpeningDayScheduleDto> days)
    {
        if (days.Count == 0)
            return true;

        var groupedDays = days.GroupBy(d => d.DayOfWeek);
        if (groupedDays.Any(g => g.Key < 0 || g.Key > 6 || g.Count() > 1))
            return false;

        foreach (var day in days)
        {
            var parsedRanges = new List<(TimeOnly Start, TimeOnly End)>();

            foreach (var slot in day.Slots)
            {
                if (!TimeOnly.TryParse(slot.StartTime, out var start)
                    || !TimeOnly.TryParse(slot.EndTime, out var end)
                    || end <= start)
                {
                    return false;
                }

                parsedRanges.Add((start, end));
            }

            parsedRanges = parsedRanges.OrderBy(r => r.Start).ToList();
            for (var i = 1; i < parsedRanges.Count; i++)
            {
                if (parsedRanges[i].Start < parsedRanges[i - 1].End)
                    return false;
            }
        }

        return true;
    }

    private async Task<int> GetTablesCapacityPerSlotAsync(
        int restaurantId,
        OrderRule? rule,
        CancellationToken ct)
    {
        if (rule?.TablesCapacityPerSlot is > -1)
            return rule.TablesCapacityPerSlot.Value;

        return await _restaurantRepository.CountActiveTablesByMaxCapacityAsync(restaurantId, 2, ct);
    }

    private static int GetRequiredTableUnits(int guestCount)
    {
        return Math.Max(1, (int)Math.Ceiling(guestCount / 2m));
    }
}
