using DeliverTableClient.Configuration.Interfaces;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos.Order;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace DeliverTableClient.Services;

public sealed class OrderHubClientService : IOrderHubClientService
{
    private readonly HubConnection _hubConnection;
    private readonly IJSRuntime _jsRuntime;
    private bool _disposed;

    public event Action<OrderDto>? OnOrderCreated;
    public event Action<OrderDto>? OnOrderStatusUpdated;
    public event Action<OrderDto>? OnOrderCancelled;

    public OrderHubClientService(IAppConfiguration configuration, IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;

        string hubUrl = $"{configuration.ApiBaseUrl.TrimEnd('/')}/{ApiRoutes.LiveOrdersHub}";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");
                };
            })
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<OrderDto>("NewOrder", (order) => OnOrderCreated?.Invoke(order));
        _hubConnection.On<OrderDto>("UpdateStatus", (order) => OnOrderStatusUpdated?.Invoke(order));
        _hubConnection.On<OrderDto>("CancelOrder", (order) => OnOrderCancelled?.Invoke(order));
    }

    public async Task StartAsync()
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync();
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            await _hubConnection.StopAsync();
        }
    }

    public async Task JoinRestaurantGroup(int restaurantId)
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("JoinRestaurantGroup", restaurantId);
        }
    }

    public async Task LeaveRestaurantGroup(int restaurantId)
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("LeaveRestaurantGroup", restaurantId);
        }
    }

    public HubConnectionState State => _hubConnection.State;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await StopAsync();
        await _hubConnection.DisposeAsync();
        _disposed = true;
    }
}
