using DeliverTableClient.Services;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Components;

namespace DeliverTableClient.Pages.Live;

public partial class Live : ComponentBase, IDisposable
{
    [Inject]
    private IRestaurantService RestaurantService { get; set; } = default!;

    [Inject]
    private IOrderService OrderService { get; set; } = default!;

    [Inject]
    private IOrderHubClientService OrderHub { get; set; } = default!;

    private int? SelectedRestaurant { get; set; } = null;

    private OrderDto[] orders = [];

    private List<OrderDto> finishedOrders = [];

    private RestaurantDto[] restaurants = [];

    protected override async Task OnInitializedAsync()
    {
        var (paginatedResult, error) = await RestaurantService.GetConnectedUserRestaurants();
        if (error is null)
        {
            restaurants = paginatedResult?.Items?.ToArray() ?? [];
            if (restaurants.Length != 0)
            {
                await OnRestaurantSelected(restaurants[0].Id);
            }
        }

        OrderHub.OnOrderCreated += HandleOrderCreated;
        OrderHub.OnOrderStatusUpdated += HandleOrderStatusUpdated;
        OrderHub.OnOrderCancelled += HandleOrderCancelled;
        await OrderHub.StartAsync();
    }

    private void HandleOrderCreated(OrderDto order)
    {
        if (SelectedRestaurant == order.RestaurantId)
        {
            _ = InvokeAsync(async () => await LoadOrders());
        }
    }

    private void HandleOrderStatusUpdated(OrderDto order)
    {
        if (SelectedRestaurant == order.RestaurantId)
        {
            _ = InvokeAsync(async () => await LoadOrders());
        }
    }

    private void HandleOrderCancelled(OrderDto order)
    {
        if (SelectedRestaurant == order.RestaurantId)
        {
            _ = InvokeAsync(async () => await LoadOrders());
        }
    }

    public async Task OnRestaurantSelected(int restaurantId)
    {
        SelectedRestaurant = restaurantId;
        finishedOrders.Clear();
        await LoadOrders();
        await LoadFinishedOrders();
    }

    public async Task MarkAsConfirmed(int orderId)
    {
        var (updatedOrder, error) = await OrderService.UpdateOrderStatusAsync(orderId, nameof(OrderStatus.Confirmed));
        if (error is null)
        {
            await LoadOrders();
        }
    }

    public async Task MarkAsPreparing(int orderId)
    {
        var (updatedOrder, error) = await OrderService.UpdateOrderStatusAsync(orderId, nameof(OrderStatus.Preparing));
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

        if (order.OrderType == nameof(OrderType.Delivery))
        {
            status = nameof(OrderStatus.Delivered);
        }

        var (updatedOrder, error) = await OrderService.UpdateOrderStatusAsync(orderId, status);
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
        SelectedRestaurant = null;
        StateHasChanged();
    }

    private async Task LoadOrders()
    {
        if (!SelectedRestaurant.HasValue) return;

        var (paginatedResult, error) = await OrderService.GetRestaurantOrdersAsync(SelectedRestaurant.Value, new OrderQuery()
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

        var (paginatedResult, error) = await OrderService.GetRestaurantOrdersAsync(SelectedRestaurant.Value, new OrderQuery()
        {
            Status = nameof(OrderStatus.Ready),
            CreatedAfter = DateTime.UtcNow.Date,
            PageNumber = 1,
            PageSize = 1000,
            SortBy = nameof(OrderDto.CreatedAt),
            SortDesc = true
        }, CancellationToken.None);

        if (error is null && paginatedResult?.Items != null)
        {
            finishedOrders = paginatedResult.Items.ToList();
        }

        var (deliveredResult, deliveredError) = await OrderService.GetRestaurantOrdersAsync(SelectedRestaurant.Value, new OrderQuery()
        {
            Status = nameof(OrderStatus.Delivered),
            CreatedAfter = DateTime.UtcNow.Date,
            PageNumber = 1,
            PageSize = 1000,
            SortBy = nameof(OrderDto.CreatedAt),
            SortDesc = true
        }, CancellationToken.None);

        if (deliveredError is null && deliveredResult?.Items != null)
        {
            finishedOrders.AddRange(deliveredResult.Items);
            // Re-sort by date descending
            finishedOrders = finishedOrders.OrderByDescending(o => o.CreatedAt).ToList();
        }

        StateHasChanged();
    }

    public void Dispose()
    {
        OrderHub.OnOrderCreated -= HandleOrderCreated;
        OrderHub.OnOrderStatusUpdated -= HandleOrderStatusUpdated;
        OrderHub.OnOrderCancelled -= HandleOrderCancelled;
    }
}
