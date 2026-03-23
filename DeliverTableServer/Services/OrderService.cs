using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Services;

public sealed class OrderService(
    IOrderRepository orderRepository,
    ICartRepository cartRepository,
    IRestaurantRepository restaurantRepository
) : IOrderService
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    public async Task<ServiceResult<OrderDto>> CreateFromCartAsync(
        int customerId, CreateOrderRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<OrderType>(request.OrderType, out var orderType))
        {
            var validValues = string.Join(", ", Enum.GetNames<OrderType>());
            return new ServiceError(ErrorMessages.InvalidOrderType(validValues));
        }

        if (request.GuestCount < 1 || request.GuestCount > 50)
            return new ServiceError(ErrorMessages.GuestCountRequired);

        if (orderType == OrderType.Delivery && string.IsNullOrWhiteSpace(request.DeliveryAddress))
            return new ServiceError(ErrorMessages.DeliveryAddressRequired);

        var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (!restaurant.IsActive)
            return new ServiceError(ErrorMessages.RestaurantNotActive);

        var cart = await _cartRepository.GetByCustomerAndRestaurantAsync(customerId, request.RestaurantId, ct);
        if (cart is null || cart.Items.Count == 0)
            return new ServiceError(ErrorMessages.CartEmpty);

        var orderItems = cart.Items.Select(ci => new OrderItem
        {
            DishId = ci.DishId,
            DishName = ci.Dish?.Name ?? string.Empty,
            Quantity = ci.Quantity,
            UnitPrice = ci.UnitPrice,
            SpecialInstructions = ci.SpecialInstructions
        }).ToList();

        var totalAmount = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity);

        var order = new Order
        {
            CustomerId = customerId,
            RestaurantId = request.RestaurantId,
            OrderType = orderType,
            Status = OrderStatus.Confirmed,
            PaymentStatus = PaymentStatus.Completed,
            TotalAmount = totalAmount,
            GuestCount = request.GuestCount,
            DeliveryAddress = orderType == OrderType.Delivery ? request.DeliveryAddress : string.Empty,
            Notes = request.Notes,
            Source = BookingSource.CustomerApp,
            Items = orderItems
        };

        var created = await _orderRepository.CreateAsync(order, ct);

        await _cartRepository.DeleteAsync(cart.Id, ct);

        var fullOrder = await _orderRepository.GetByIdAsync(created.Id, ct);
        return fullOrder!.ToDto();
    }

    public async Task<ServiceResult<OrderDto>> GetByIdAsync(int orderId, int userId, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null)
            return new ServiceError(ErrorMessages.OrderNotFound, 404);

        if (order.CustomerId != userId && order.Restaurant.OwnerId != userId)
            return new ServiceError(ErrorMessages.OrderNotFound, 404);

        return order.ToDto();
    }

    public async Task<ServiceResult<PaginatedResult<OrderDto>>> GetCustomerOrdersAsync(
        int customerId, OrderQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _orderRepository.GetByCustomerAsync(customerId, query, ct);

        return new PaginatedResult<OrderDto>
        {
            Items = items.Select(o => o.ToDto()).ToList(),
            TotalCount = totalCount,
            Page = query.PageNumber > 0 ? query.PageNumber : 1,
            PageSize = query.PageSize
        };
    }

    public async Task<ServiceResult<PaginatedResult<OrderDto>>> GetRestaurantOrdersAsync(
        int restaurantId, int ownerId, OrderQuery query, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (restaurant.OwnerId != ownerId)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        var (items, totalCount) = await _orderRepository.GetByRestaurantAsync(restaurantId, query, ct);

        return new PaginatedResult<OrderDto>
        {
            Items = items.Select(o => o.ToDto()).ToList(),
            TotalCount = totalCount,
            Page = query.PageNumber > 0 ? query.PageNumber : 1,
            PageSize = query.PageSize
        };
    }

    public async Task<ServiceResult<OrderDto>> UpdateStatusAsync(
        int orderId, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null)
            return new ServiceError(ErrorMessages.OrderNotFound, 404);

        if (!Enum.TryParse<OrderStatus>(request.Status, out var newStatus))
        {
            var validValues = string.Join(", ", Enum.GetNames<OrderStatus>());
            return new ServiceError(ErrorMessages.InvalidOrderStatus(validValues));
        }

        order.Status = newStatus;
        var updated = await _orderRepository.UpdateAsync(order, ct);
        return updated.ToDto();
    }

    public async Task<ServiceResult<OrderDto>> CancelOrderAsync(int orderId, int customerId, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null)
            return new ServiceError(ErrorMessages.OrderNotFound, 404);

        if (order.CustomerId != customerId)
            return new ServiceError(ErrorMessages.OrderNotFound, 404);

        if (order.Status is OrderStatus.Delivering or OrderStatus.Delivered or OrderStatus.Cancelled)
            return new ServiceError(ErrorMessages.OrderCannotBeCancelled);

        order.Status = OrderStatus.Cancelled;
        var updated = await _orderRepository.UpdateAsync(order, ct);
        return updated.ToDto();
    }
}
