using DeliverTableSharedLibrary.Dtos.Order;
using Microsoft.AspNetCore.Components;

namespace DeliverTableClient.Components.Order;

public partial class OrderSessionSummary : ComponentBase
{
    [Parameter]
    public List<OrderDto> Orders { get; set; } = [];

    private string TranslateType(string orderType)
    {
        return orderType switch
        {
            "DineIn" => "Sur place",
            "TakeAway" => "À emporter",
            "Delivery" => "Livraison",
            _ => orderType
        };
    }
}
