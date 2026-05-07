using DeliverTableServer.Hubs.Interfaces;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DeliverTableServer.Hubs;

[Authorize]
public class OrderHub : Hub<IOrderHub>
{
    public async Task JoinRestaurantGroup(int restaurantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"restaurant_{restaurantId}");
    }

    public async Task LeaveRestaurantGroup(int restaurantId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"restaurant_{restaurantId}");
    }

    public Task NewOrder(OrderDto order)
    {
        return Clients.Group($"restaurant_{order.RestaurantId}").NewOrder(order);
    }

    public Task UpdateStatus(OrderDto order)
    {
        return Clients.Group($"restaurant_{order.RestaurantId}").UpdateStatus(order);
    }

    public Task CancelOrder(OrderDto order)
    {
        return Clients.Group($"restaurant_{order.RestaurantId}").CancelOrder(order);
    }
}
