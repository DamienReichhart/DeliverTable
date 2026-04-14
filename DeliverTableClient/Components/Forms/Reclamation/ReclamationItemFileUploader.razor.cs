using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Dtos.Reclamation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace DeliverTableClient.Components.Forms.Reclamation;

public partial class ReclamationItemFileUploader : ComponentBase
{
    private CreateReclamationItemDto? ItemDto { get; set; }

    [Parameter] public required OrderItemDto OrderItem { get; set; }
    [Parameter] public EventCallback<(CreateReclamationItemDto?, IBrowserFile?)> OnItemChanged { get; set; }

    protected override void OnParametersSet()
    {
        ItemDto = new CreateReclamationItemDto { OrderItemId = OrderItem.Id };
    }

    private async Task HandleImageSelected(InputFileChangeEventArgs e)
    {
        ItemDto?.HasImage = e.FileCount != 0;
        IBrowserFile file = e.File;
        await OnItemChanged.InvokeAsync((ItemDto, file));
    }
}