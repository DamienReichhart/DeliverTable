using DeliverTableSharedLibrary.Dtos.Order;
using Microsoft.AspNetCore.SignalR.Client;

namespace DeliverTableClient.Services.Interfaces;

public interface IOrderHubClientService : IAsyncDisposable
{
    event Action<OrderDto>? OnOrderCreated;
    event Action<OrderDto>? OnOrderStatusUpdated;
    event Action<OrderDto>? OnOrderCancelled;

    Task StartAsync();
    Task StopAsync();
    HubConnectionState State { get; }
}
