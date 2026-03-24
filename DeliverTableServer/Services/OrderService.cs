using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Extensions;
using DeliverTableServer.Helpers;
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

        var originalAmount = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity);
        var orderDiscounts = new List<OrderDiscount>();

        await ApplyPromotionsAsync(request.RestaurantId, originalAmount, cart.Items, orderDiscounts, ct);

        var discountCodesResult = await ApplyDiscountCodesAsync(
            request.DiscountCodes, request.RestaurantId, customerId, originalAmount, orderDiscounts, ct);
        if (!discountCodesResult.IsSuccess)
            return discountCodesResult.Error!;
        var appliedDiscountCodes = discountCodesResult.Value!;

        var loyaltyResult = await ApplyLoyaltyPointsAsync(
            request.RestaurantId, customerId, request.LoyaltyPointsToRedeem, originalAmount, orderDiscounts, ct);
        if (!loyaltyResult.IsSuccess)
            return loyaltyResult.Error!;
        var loyaltyPointsUsed = loyaltyResult.Value!;

        var discountAmount = Math.Min(orderDiscounts.Sum(d => d.Amount), originalAmount);
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
        await TrackDiscountCodeRedemptionsAsync(appliedDiscountCodes, customerId, created.Id, ct);
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
        var data = await _orderRepository.GetByCustomerAsync(customerId, query, ct);
        return data.ToPaginatedResult(o => o.ToDto(), query.PageNumber, query.PageSize);
    }

    public async Task<ServiceResult<PaginatedResult<OrderDto>>> GetRestaurantOrdersAsync(
        int restaurantId, int ownerId, OrderQuery query, CancellationToken ct = default)
    {
        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, restaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        var data = await _orderRepository.GetByRestaurantAsync(restaurantId, query, ct);
        return data.ToPaginatedResult(o => o.ToDto(), query.PageNumber, query.PageSize);
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
            await ProcessDeliveryAsync(order, ct);

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

    private async Task ApplyPromotionsAsync(
        int restaurantId, decimal originalAmount, ICollection<CartItem> cartItems,
        List<OrderDiscount> orderDiscounts, CancellationToken ct)
    {
        var promotions = await _promotionRepository.GetActiveByRestaurantAsync(restaurantId, ct);
        foreach (var promotion in promotions)
        {
            var discount = CalculatePromotionDiscount(promotion, originalAmount, cartItems);
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
    }

    private async Task<ServiceResult<List<Models.DiscountCode>>> ApplyDiscountCodesAsync(
        List<string> codes, int restaurantId, int customerId, decimal originalAmount,
        List<OrderDiscount> orderDiscounts, CancellationToken ct)
    {
        var appliedCodes = new List<Models.DiscountCode>();
        foreach (var codeStr in codes.Where(c => !string.IsNullOrWhiteSpace(c)))
        {
            var discountCode = await _discountCodeRepository.GetByCodeAndRestaurantAsync(
                codeStr, restaurantId, ct);

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

            appliedCodes.Add(discountCode);
        }

        return appliedCodes;
    }

    private async Task<ServiceResult<int>> ApplyLoyaltyPointsAsync(
        int restaurantId, int customerId, int pointsToRedeem, decimal originalAmount,
        List<OrderDiscount> orderDiscounts, CancellationToken ct)
    {
        if (pointsToRedeem <= 0)
            return 0;

        var program = await _loyaltyRepository.GetByRestaurantAsync(restaurantId, ct);
        if (program is null || !program.IsActive)
            return 0;

        var account = await _loyaltyRepository.GetAccountAsync(program.Id, customerId, ct);
        if (account is null || account.PointsBalance < pointsToRedeem)
            return new ServiceError(ErrorMessages.InsufficientLoyaltyPoints);

        var pointsEuroValue = pointsToRedeem * program.EurosPerPoint;

        var currentDiscountTotal = orderDiscounts.Sum(d => d.Amount);
        var maxLoyaltyDiscount = Math.Max(0, originalAmount - currentDiscountTotal);
        pointsEuroValue = Math.Min(pointsEuroValue, maxLoyaltyDiscount);

        if (pointsEuroValue <= 0)
            return 0;

        var actualPointsUsed = program.EurosPerPoint > 0
            ? (int)Math.Ceiling(pointsEuroValue / program.EurosPerPoint)
            : pointsToRedeem;
        actualPointsUsed = Math.Min(actualPointsUsed, pointsToRedeem);

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

        return actualPointsUsed;
    }

    private async Task TrackDiscountCodeRedemptionsAsync(
        List<Models.DiscountCode> appliedCodes, int customerId, int orderId, CancellationToken ct)
    {
        foreach (var dc in appliedCodes)
        {
            dc.CurrentRedemptions++;
            await _discountCodeRepository.UpdateAsync(dc, ct);
            await _discountCodeRepository.CreateRedemptionAsync(new DiscountCodeRedemption
            {
                DiscountCodeId = dc.Id,
                CustomerId = customerId,
                OrderId = orderId
            }, ct);
        }
    }

    private async Task ProcessDeliveryAsync(Order order, CancellationToken ct)
    {
        var restaurant = order.Restaurant;
        var commission = order.TotalAmount * _commissionRate;
        var netAmount = order.TotalAmount - commission;

        restaurant.Balance += netAmount;
        await _restaurantRepository.UpdateAsync(restaurant, ct);

        await _transactionRepository.CreateAsync(new RestaurantTransaction
        {
            RestaurantId = restaurant.Id,
            OrderId = order.Id,
            Type = TransactionType.Credit,
            GrossAmount = order.TotalAmount,
            CommissionAmount = commission,
            NetAmount = netAmount,
            BalanceAfter = restaurant.Balance
        }, ct);

        await EarnLoyaltyPointsAsync(order, ct);
    }

    private async Task EarnLoyaltyPointsAsync(Order order, CancellationToken ct)
    {
        var loyaltyProgram = await _loyaltyRepository.GetByRestaurantAsync(order.RestaurantId, ct);
        if (loyaltyProgram is null || !loyaltyProgram.IsActive)
            return;

        var pointsEarned = (int)Math.Floor(order.OriginalAmount * loyaltyProgram.PointsPerEuro);
        if (pointsEarned <= 0)
            return;

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
