using DeliverTableServer.Hubs.Interfaces;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.SignalR;

namespace DeliverTableServer.Hubs;

public class OrderHub : Hub<IOrderHub>
{
    public Task NewOrder(OrderDto order)
    {
        return Clients.All.NewOrder(order);
    }

    public Task UpdateStatus(OrderDto order)
    {
        return Clients.All.UpdateStatus(order);
    }

    public Task CancelOrder(OrderDto order)
    {
        return Clients.All.CancelOrder(order);
    }
}
