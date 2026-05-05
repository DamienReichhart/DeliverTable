using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Components;
using DeliverTableSharedLibrary.Dtos.Order;

namespace DeliverTableClient.Components.Order;

public partial class OrderLiveCard : ComponentBase
{

    [Parameter] public OrderDto Order { get; set; } = default!;
    [Parameter] public EventCallback<int> OnOrderReady { get; set; }
    [Parameter] public EventCallback<int> OnOrderConfirmed { get; set; }
    [Parameter] public EventCallback<int> OnOrderPreparing { get; set; }

    private static string Translate(string status)
    {
        return Enum.TryParse(status, out OrderStatus orderStatus) ? orderStatus switch
        {
            OrderStatus.Pending => "En attente",
            OrderStatus.Confirmed => "Confirmée",
            OrderStatus.Preparing => "En préparation",
            OrderStatus.Ready => "Prête",
            OrderStatus.Cancelled => "Annulée",
            _ => status
        } : status;
    }

    private async Task MarkAsConfirmed()
    {
        if (!OnOrderConfirmed.HasDelegate) return;
        await OnOrderConfirmed.InvokeAsync(Order.Id);
    }

    private async Task MarkAsReady()
    {
        if (!OnOrderReady.HasDelegate) return;
        await OnOrderReady.InvokeAsync(Order.Id);
    }
}