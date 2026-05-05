using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Hubs.Interfaces;

public interface IOrderHub
{
    Task NewOrder(OrderDto order);
    Task UpdateStatus(OrderDto order);
    Task CancelOrder(OrderDto order);
}