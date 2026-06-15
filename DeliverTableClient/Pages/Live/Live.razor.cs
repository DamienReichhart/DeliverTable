using DeliverTableClient.Services;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Components;

namespace DeliverTableClient.Pages.Live;

public partial class Live : ComponentBase, IDisposable
{
    public enum LiveViewMode
    {
        Restaurant,
        Client
    }

    [Inject]
    private IRestaurantService RestaurantService { get; set; } = default!;

    [Inject]
    private IOrderService OrderService { get; set; } = default!;

    [Inject]
    private IOrderHubClientService OrderHub { get; set; } = default!;

    private LiveViewMode CurrentMode { get; set; } = LiveViewMode.Restaurant;

    private int? SelectedRestaurant { get; set; } = null;

    private OrderDto[] orders = [];

    private List<OrderDto> finishedOrders = [];

    private RestaurantDto[] restaurants = [];

    private System.Timers.Timer? refreshTimer;

    protected override async Task OnInitializedAsync()
    {
        (DeliverTableSharedLibrary.Dtos.PaginatedResult<RestaurantDto>? paginatedResult, DeliverTableSharedLibrary.Dtos.ErrorResponse? error) = await RestaurantService.GetConnectedUserRestaurants();
        if (error is null)
        {
            restaurants = paginatedResult?.Items?.ToArray() ?? [];
        }

        OrderHub.OnOrderCreated += HandleOrderCreated;
        OrderHub.OnOrderStatusUpdated += HandleOrderStatusUpdated;
        OrderHub.OnOrderCancelled += HandleOrderCancelled;
        await OrderHub.StartAsync();

        if (restaurants.Length != 0)
        {
            await OnRestaurantSelected(restaurants[0].Id);
        }

        // Timer to refresh the UI every minute (for the 15min window)
        refreshTimer = new System.Timers.Timer(60000);
        refreshTimer.Elapsed += (s, e) => InvokeAsync(StateHasChanged);
        refreshTimer.Start();
    }

    private void HandleOrderCreated(OrderDto order)
    {
        _ = InvokeAsync(async () =>
        {
            await LoadOrders();
            await LoadFinishedOrders();
        });
    }

    private void HandleOrderStatusUpdated(OrderDto order)
    {
        _ = InvokeAsync(async () =>
        {
            await LoadOrders();
            await LoadFinishedOrders();
        });
    }

    private void HandleOrderCancelled(OrderDto order)
    {
        _ = InvokeAsync(async () =>
        {
            await LoadOrders();
            await LoadFinishedOrders();
        });
    }

    public async Task OnRestaurantSelected(int restaurantId)
    {
        if (SelectedRestaurant.HasValue)
        {
            await OrderHub.LeaveRestaurantGroup(SelectedRestaurant.Value);
        }

        SelectedRestaurant = restaurantId;
        await OrderHub.JoinRestaurantGroup(restaurantId);

        finishedOrders.Clear();
        await LoadOrders();
        await LoadFinishedOrders();
    }

    public async Task MarkAsConfirmed(int orderId)
    {
        (OrderDto? updatedOrder, DeliverTableSharedLibrary.Dtos.ErrorResponse? error) = await OrderService.UpdateOrderStatusAsync(orderId, nameof(OrderStatus.Confirmed));
        if (error is null)
        {
            await LoadOrders();
        }
    }

    public async Task MarkAsPreparing(int orderId)
    {
        (OrderDto? updatedOrder, DeliverTableSharedLibrary.Dtos.ErrorResponse? error) = await OrderService.UpdateOrderStatusAsync(orderId, nameof(OrderStatus.Preparing));
        if (error is null)
        {
            await LoadOrders();
        }
    }

    public async Task MarkAsReady(int orderId)
    {
        OrderDto? order = orders.First(o => o.Id == orderId);
        if (order is null) return;

        string status = nameof(OrderStatus.Ready);

        (OrderDto? updatedOrder, DeliverTableSharedLibrary.Dtos.ErrorResponse? error) = await OrderService.UpdateOrderStatusAsync(orderId, status);
        if (error is null)
        {
            if (updatedOrder != null)
            {
                finishedOrders.Insert(0, updatedOrder);
            }
            await LoadOrders();
        }
    }

    public void OnRestaurantDeselected()
    {
        if (SelectedRestaurant.HasValue)
        {
            _ = OrderHub.LeaveRestaurantGroup(SelectedRestaurant.Value);
        }
        SelectedRestaurant = null;
        StateHasChanged();
    }

    private async Task LoadOrders()
    {
        if (!SelectedRestaurant.HasValue) return;

        (DeliverTableSharedLibrary.Dtos.PaginatedResult<OrderDto>? paginatedResult, DeliverTableSharedLibrary.Dtos.ErrorResponse? error) = await OrderService.GetRestaurantOrdersAsync(SelectedRestaurant.Value, new OrderQuery()
        {
            ToPrepare = true,
            PageNumber = 1,
            PageSize = 1000,
            SortBy = nameof(OrderDto.CreatedAt),
            SortDesc = false
        }, CancellationToken.None);
        if (error is null)
        {
            orders = paginatedResult?.Items?.ToArray() ?? [];
        }
        StateHasChanged();
    }

    private async Task LoadFinishedOrders()
    {
        if (!SelectedRestaurant.HasValue) return;

        (DeliverTableSharedLibrary.Dtos.PaginatedResult<OrderDto>? readyResult, DeliverTableSharedLibrary.Dtos.ErrorResponse? readyError) = await OrderService.GetRestaurantOrdersAsync(SelectedRestaurant.Value, new OrderQuery()
        {
            Status = nameof(OrderStatus.Ready),
            PageNumber = 1,
            PageSize = 1000,
            SortBy = nameof(OrderDto.CreatedAt),
            SortDesc = true
        }, CancellationToken.None);

        if (readyError is null && readyResult?.Items != null)
        {
            finishedOrders = readyResult.Items;
        }

        (DeliverTableSharedLibrary.Dtos.PaginatedResult<OrderDto>? deliveredResult, DeliverTableSharedLibrary.Dtos.ErrorResponse? deliveredError) = await OrderService.GetRestaurantOrdersAsync(SelectedRestaurant.Value, new OrderQuery()
        {
            Status = nameof(OrderStatus.Delivered),
            PageNumber = 1,
            PageSize = 1000,
            SortBy = nameof(OrderDto.CreatedAt),
            SortDesc = true
        }, CancellationToken.None);

        if (deliveredError is null && deliveredResult?.Items != null)
        {
            finishedOrders.AddRange(deliveredResult.Items);
            finishedOrders = finishedOrders.OrderByDescending(o => o.CreatedAt).ToList();
        }

        StateHasChanged();
    }

    public void Dispose()
    {
        refreshTimer?.Stop();
        refreshTimer?.Dispose();

        OrderHub.OnOrderCreated -= HandleOrderCreated;
        OrderHub.OnOrderStatusUpdated -= HandleOrderStatusUpdated;
        OrderHub.OnOrderCancelled -= HandleOrderCancelled;

        if (SelectedRestaurant.HasValue)
        {
            _ = OrderHub.LeaveRestaurantGroup(SelectedRestaurant.Value);
        }
    }
}
