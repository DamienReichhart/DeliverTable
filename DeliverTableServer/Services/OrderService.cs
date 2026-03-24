using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
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
    IRestaurantRepository restaurantRepository,
    IRestaurantTransactionRepository transactionRepository,
    IPromotionRepository promotionRepository,
    IDiscountCodeRepository discountCodeRepository,
    ILoyaltyRepository loyaltyRepository,
    AppEnvironment appEnvironment
) : IOrderService
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;
    private readonly IRestaurantTransactionRepository _transactionRepository = transactionRepository;
    private readonly IPromotionRepository _promotionRepository = promotionRepository;
    private readonly IDiscountCodeRepository _discountCodeRepository = discountCodeRepository;
    private readonly ILoyaltyRepository _loyaltyRepository = loyaltyRepository;
    private readonly decimal _commissionRate = appEnvironment.PlatformCommissionRate;

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

        // Step 1: Set OriginalAmount
        var originalAmount = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity);
        var orderDiscounts = new List<OrderDiscount>();

        // Step 2: Apply active promotions (all stack)
        var promotions = await _promotionRepository.GetActiveByRestaurantAsync(request.RestaurantId, ct);
        foreach (var promotion in promotions)
        {
            var discount = CalculatePromotionDiscount(promotion, originalAmount, cart.Items);
            if (discount > 0)
            {
                orderDiscounts.Add(new OrderDiscount
                {
                    Source = OrderDiscountSource.Promotion,
                    SourceId = promotion.Id,
                    Description = promotion.Name,
                    Amount = discount
                });
            }
        }

        // Step 3: Apply discount codes
        var appliedDiscountCodes = new List<Models.DiscountCode>();
        foreach (var codeStr in request.DiscountCodes.Where(c => !string.IsNullOrWhiteSpace(c)))
        {
            var discountCode = await _discountCodeRepository.GetByCodeAndRestaurantAsync(
                codeStr, request.RestaurantId, ct);

            var now = DateTime.UtcNow;
            if (discountCode is null || !discountCode.IsActive ||
                now < discountCode.ValidFrom || now > discountCode.ValidUntil)
                return new ServiceError(ErrorMessages.DiscountCodeInvalid);

            if (discountCode.MaxRedemptions.HasValue &&
                discountCode.CurrentRedemptions >= discountCode.MaxRedemptions.Value)
                return new ServiceError(ErrorMessages.DiscountCodeMaxRedemptions);

            var userRedemptions = await _discountCodeRepository.GetRedemptionCountByUserAsync(
                discountCode.Id, customerId, ct);
            if (userRedemptions >= discountCode.PerUserLimit)
                return new ServiceError(ErrorMessages.DiscountCodePerUserLimit);

            if (discountCode.MinOrderAmount.HasValue && originalAmount < discountCode.MinOrderAmount.Value)
                return new ServiceError(ErrorMessages.DiscountCodeMinOrderNotMet);

            var codeDiscount = discountCode.DiscountType == DiscountType.Percentage
                ? originalAmount * (discountCode.DiscountValue / 100)
                : discountCode.DiscountValue;

            orderDiscounts.Add(new OrderDiscount
            {
                Source = OrderDiscountSource.DiscountCode,
                SourceId = discountCode.Id,
                Description = $"{discountCode.Code} — {discountCode.Description}",
                Amount = codeDiscount
            });

            discountCode.CurrentRedemptions++;
            await _discountCodeRepository.UpdateAsync(discountCode, ct);
            appliedDiscountCodes.Add(discountCode);
        }

        // Step 4: Apply loyalty points
        var loyaltyPointsUsed = 0;
        if (request.LoyaltyPointsToRedeem > 0)
        {
            var program = await _loyaltyRepository.GetByRestaurantAsync(request.RestaurantId, ct);
            if (program is not null && program.IsActive)
            {
                var account = await _loyaltyRepository.GetAccountAsync(program.Id, customerId, ct);
                if (account is null || account.PointsBalance < request.LoyaltyPointsToRedeem)
                    return new ServiceError(ErrorMessages.InsufficientLoyaltyPoints);

                var pointsEuroValue = request.LoyaltyPointsToRedeem * program.EurosPerPoint;

                // Cap so total discount doesn't exceed originalAmount
                var currentDiscountTotal = orderDiscounts.Sum(d => d.Amount);
                var maxLoyaltyDiscount = originalAmount - currentDiscountTotal;
                if (maxLoyaltyDiscount < 0) maxLoyaltyDiscount = 0;
                pointsEuroValue = Math.Min(pointsEuroValue, maxLoyaltyDiscount);

                if (pointsEuroValue > 0)
                {
                    // Recalculate actual points used after capping
                    var actualPointsUsed = program.EurosPerPoint > 0
                        ? (int)Math.Ceiling(pointsEuroValue / program.EurosPerPoint)
                        : request.LoyaltyPointsToRedeem;
                    actualPointsUsed = Math.Min(actualPointsUsed, request.LoyaltyPointsToRedeem);
                    loyaltyPointsUsed = actualPointsUsed;

                    account.PointsBalance -= actualPointsUsed;
                    await _loyaltyRepository.UpdateAccountAsync(account, ct);

                    await _loyaltyRepository.CreateTransactionAsync(new LoyaltyTransaction
                    {
                        LoyaltyAccountId = account.Id,
                        Type = LoyaltyTransactionType.Redeem,
                        Points = -actualPointsUsed
                    }, ct);

                    orderDiscounts.Add(new OrderDiscount
                    {
                        Source = OrderDiscountSource.LoyaltyPoints,
                        SourceId = program.Id,
                        Description = $"Points fidélité ({actualPointsUsed} pts)",
                        Amount = pointsEuroValue
                    });
                }
            }
        }

        // Step 5: Calculate finals
        var discountAmount = orderDiscounts.Sum(d => d.Amount);
        discountAmount = Math.Min(discountAmount, originalAmount);
        var totalAmount = originalAmount - discountAmount;

        var order = new Order
        {
            CustomerId = customerId,
            RestaurantId = request.RestaurantId,
            OrderType = orderType,
            Status = OrderStatus.Confirmed,
            PaymentStatus = PaymentStatus.Completed,
            OriginalAmount = originalAmount,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            LoyaltyPointsUsed = loyaltyPointsUsed,
            DiscountCodeId = appliedDiscountCodes.Count > 0 ? appliedDiscountCodes[0].Id : null,
            GuestCount = request.GuestCount,
            DeliveryAddress = orderType == OrderType.Delivery ? request.DeliveryAddress : string.Empty,
            Notes = request.Notes,
            Source = BookingSource.CustomerApp,
            Items = orderItems,
            Discounts = orderDiscounts
        };

        var created = await _orderRepository.CreateAsync(order, ct);

        // Create discount code redemption records after order is created
        foreach (var dc in appliedDiscountCodes)
        {
            await _discountCodeRepository.CreateRedemptionAsync(new DiscountCodeRedemption
            {
                DiscountCodeId = dc.Id,
                CustomerId = customerId,
                OrderId = created.Id
            }, ct);
        }

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

        if (newStatus == OrderStatus.Delivered)
        {
            var restaurant = order.Restaurant;
            var commission = order.TotalAmount * _commissionRate;
            var netAmount = order.TotalAmount - commission;

            restaurant.Balance += netAmount;
            await _restaurantRepository.UpdateAsync(restaurant, ct);

            var transaction = new RestaurantTransaction
            {
                RestaurantId = restaurant.Id,
                OrderId = order.Id,
                Type = TransactionType.Credit,
                GrossAmount = order.TotalAmount,
                CommissionAmount = commission,
                NetAmount = netAmount,
                BalanceAfter = restaurant.Balance
            };

            await _transactionRepository.CreateAsync(transaction, ct);

            // Earn loyalty points
            var loyaltyProgram = await _loyaltyRepository.GetByRestaurantAsync(order.RestaurantId, ct);
            if (loyaltyProgram is not null && loyaltyProgram.IsActive)
            {
                var pointsEarned = (int)Math.Floor(order.OriginalAmount * loyaltyProgram.PointsPerEuro);
                if (pointsEarned > 0)
                {
                    var loyaltyAccount = await _loyaltyRepository.GetAccountAsync(loyaltyProgram.Id, order.CustomerId, ct);
                    if (loyaltyAccount is null)
                    {
                        loyaltyAccount = new LoyaltyAccount
                        {
                            LoyaltyProgramId = loyaltyProgram.Id,
                            CustomerId = order.CustomerId
                        };
                        await _loyaltyRepository.CreateAccountAsync(loyaltyAccount, ct);
                    }

                    loyaltyAccount.PointsBalance += pointsEarned;
                    await _loyaltyRepository.UpdateAccountAsync(loyaltyAccount, ct);

                    await _loyaltyRepository.CreateTransactionAsync(new LoyaltyTransaction
                    {
                        LoyaltyAccountId = loyaltyAccount.Id,
                        Type = LoyaltyTransactionType.Earn,
                        Points = pointsEarned,
                        OrderId = order.Id
                    }, ct);

                    order.LoyaltyPointsEarned = pointsEarned;
                    await _orderRepository.UpdateAsync(order, ct);
                }
            }
        }

        return updated.ToDto();
    }

    public async Task<ServiceResult<OrderDto>> CancelOrderAsync(int orderId, int customerId, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null)
            return new ServiceError(ErrorMessages.OrderNotFound, 404);

        if (order.CustomerId != customerId)
            return new ServiceError(ErrorMessages.OrderNotFound, 404);

        if (order.Status is OrderStatus.Delivering or OrderStatus.Delivered or OrderStatus.Cancelled or OrderStatus.Refused)
            return new ServiceError(ErrorMessages.OrderCannotBeCancelled);

        order.Status = OrderStatus.Cancelled;
        var updated = await _orderRepository.UpdateAsync(order, ct);
        return updated.ToDto();
    }

    private static decimal CalculatePromotionDiscount(
        Promotion promotion, decimal originalAmount, ICollection<CartItem> cartItems)
    {
        switch (promotion.PromotionType)
        {
            case PromotionType.Automatic:
                return promotion.DiscountType == DiscountType.Percentage
                    ? originalAmount * (promotion.DiscountValue / 100)
                    : promotion.DiscountValue;

            case PromotionType.Threshold:
                if (promotion.MinOrderAmount.HasValue && originalAmount < promotion.MinOrderAmount.Value)
                    return 0;
                return promotion.DiscountType == DiscountType.Percentage
                    ? originalAmount * (promotion.DiscountValue / 100)
                    : promotion.DiscountValue;

            case PromotionType.ItemBased:
                var promotionDishIds = promotion.PromotionDishes.Select(pd => pd.DishId).ToHashSet();
                var matchingSubtotal = cartItems
                    .Where(ci => promotionDishIds.Contains(ci.DishId))
                    .Sum(ci => ci.UnitPrice * ci.Quantity);
                if (matchingSubtotal == 0) return 0;
                return promotion.DiscountType == DiscountType.Percentage
                    ? matchingSubtotal * (promotion.DiscountValue / 100)
                    : Math.Min(promotion.DiscountValue, matchingSubtotal);

            default:
                return 0;
        }
    }
}
