using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Mappers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Rating;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Services;

public sealed class RatingService(IRatingRepository ratingRepository, IOrderRepository orderRepository) : IRatingService
{
    private readonly IRatingRepository _ratingRepository = ratingRepository;
    private readonly IOrderRepository _orderRepository = orderRepository;

    public async Task<ServiceResult<RatingDto>> CreateAsync(int orderId, int customerId, CreateRatingRequest request, CancellationToken ct = default)
    {
        if (request.Rating < 1 || request.Rating > 5)
            return ServiceError.BadRequest(ErrorMessages.RatingOutOfRange);

        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null || order.CustomerId != customerId)
            return ServiceError.NotFound(ErrorMessages.OrderNotFound);

        if (order.Status != OrderStatus.Delivered)
            return ServiceError.BadRequest(ErrorMessages.OrderNotDelivered);

        var existing = await _ratingRepository.GetByOrderAndCustomerAsync(orderId, customerId, ct);
        if (existing is not null)
            return ServiceError.Conflict(ErrorMessages.RatingAlreadyExists);

        var rating = new RestaurantRating
        {
            OrderId = orderId,
            RestaurantId = order.RestaurantId,
            CustomerUserId = customerId,
            Rating = request.Rating,
            Comment = request.Comment ?? string.Empty
        };

        var created = await _ratingRepository.CreateAsync(rating, ct);
        return created.ToDto();
    }

    public async Task<ServiceResult<RatingDto>> GetByOrderAsync(int orderId, int customerId, CancellationToken ct = default)
    {
        var rating = await _ratingRepository.GetByOrderAndCustomerAsync(orderId, customerId, ct);
        if (rating is null)
            return ServiceError.NotFound(ErrorMessages.RatingNotFound);

        return rating.ToDto();
    }

    public async Task<ServiceResult<RatingDto>> UpdateAsync(int orderId, int customerId, UpdateRatingRequest request, CancellationToken ct = default)
    {
        if (request.Rating < 1 || request.Rating > 5)
            return ServiceError.BadRequest(ErrorMessages.RatingOutOfRange);

        var rating = await _ratingRepository.GetByOrderAndCustomerAsync(orderId, customerId, ct);
        if (rating is null)
            return ServiceError.NotFound(ErrorMessages.RatingNotFound);

        rating.Rating = request.Rating;
        rating.Comment = request.Comment ?? string.Empty;

        var updated = await _ratingRepository.UpdateAsync(rating, ct);
        return updated.ToDto();
    }

    public async Task<ServiceResult> DeleteAsync(int orderId, int customerId, CancellationToken ct = default)
    {
        var rating = await _ratingRepository.GetByOrderAndCustomerAsync(orderId, customerId, ct);
        if (rating is null)
            return ServiceError.NotFound(ErrorMessages.RatingNotFound);

        await _ratingRepository.DeleteAsync(rating, ct);
        return ServiceResult.Success();
    }
}
