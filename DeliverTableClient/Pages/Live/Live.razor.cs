using DeliverTableClient.Services;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Components;

namespace DeliverTableClient.Pages.Live;

public partial class Live : ComponentBase
{
    [Inject]
    private IRestaurantService RestaurantService { get; set; } = default!;

    [Inject]
    private IOrderService OrderService { get; set; } = default!;

    private int? SelectedRestaurant { get; set; } = null;

    private OrderDto[] orders = [];

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
    }

    public async Task OnRestaurantSelected(int restaurantId)
    {
        SelectedRestaurant = restaurantId;
        await LoadOrders();
    }

    public async Task MarkAsConfirmed(int orderId)
    {
        var (_, error) = await OrderService.UpdateOrderStatusAsync(orderId, nameof(OrderStatus.Preparing));
        if (error is null)
        {
            await LoadOrders();
        }
    }

    public async Task MarkAsReady(int orderId)
    {
        var (_, error) = await OrderService.UpdateOrderStatusAsync(orderId, nameof(OrderStatus.Ready));
        if (error is null)
        {
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
            PageSize = 100,
            SortBy = nameof(OrderDto.CreatedAt),
            SortDesc = false
        }, CancellationToken.None);
        if (error is null)
        {
            orders = paginatedResult?.Items?.ToArray() ?? [];
        }
        StateHasChanged();
    }

    private static string Translate(string status)
    {
        return status switch
        {
            nameof(OrderStatus.Pending) => "En attente",
            nameof(OrderStatus.Confirmed) => "Confirmée",
            nameof(OrderStatus.Preparing) => "En préparation",
            nameof(OrderStatus.Ready) => "Prête",
            nameof(OrderStatus.Cancelled) => "Annulée",
            _ => status
        };
    }
}
