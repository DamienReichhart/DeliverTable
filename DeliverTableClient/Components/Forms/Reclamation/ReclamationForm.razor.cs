using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Dtos.Reclamation;
using DeliverTableSharedLibrary.Enums;
using DeliverTableSharedLibrary.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace DeliverTableClient.Components.Forms.Reclamation;

public partial class ReclamationForm(IReclamationService reclamationService, NavigationManager navigationManager)
    : ComponentBase
{
    private string? _errorMessage;

    private readonly List<Image> _images = new();

    private bool _isReclamationSend;

    private bool _isSubmitting;

    private readonly List<(string Name, string Value)> _reclamationTypes = Enum.GetValues<ReclamationType>()
        .Select(e => (Name: TranslateType(e.ToString()), Value: e.ToString()))
        .ToList();

    [Parameter] public required OrderDto Order { get; set; }

    public CreateReclamationDto Reclamation { get; set; } = new();

    private static string TranslateType(string type)
    {
        return type switch
        {
            nameof(ReclamationType.FoodQuality) => "Qualité du repas",
            nameof(ReclamationType.ServiceIssue) => "Problème lors du service",
            nameof(ReclamationType.WrongOrder) => "Mauvaise commande",
            nameof(ReclamationType.Other) => "Autre motif",
            _ => type
        };
    }

    protected override void OnParametersSet()
    {
        Reclamation = new CreateReclamationDto
        {
            OrderId = Order.Id,
            Items = new List<CreateReclamationItemDto>()
        };
    }

    private async Task HandleItemChanges((CreateReclamationItemDto? ItemDto, IBrowserFile? file) data)
    {
        (CreateReclamationItemDto? itemDto, IBrowserFile? file) = data;
        if (itemDto is null)
            return;

        string entryPrefix = $"Item_{itemDto.OrderItemId}_image";
        if (file != null)
        {
            string fileExtension = Path.GetExtension(file.Name);
            string entryName = $"{entryPrefix}{fileExtension}";

            using MemoryStream stream = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(stream);
            byte[] buffer = stream.ToArray();

            ByteArrayContent byteArrayContent = new(buffer);
            _images.RemoveAll(i => i.Name.StartsWith(entryPrefix, StringComparison.Ordinal));
            _images.Add(new Image
            {
                Content = byteArrayContent,
                Name = entryName
            });

            if (Reclamation.Items.All(i => i.OrderItemId != itemDto.OrderItemId))
                Reclamation.Items.Add(itemDto);
        }
        else
        {
            Reclamation.Items.RemoveAll(i => i.OrderItemId == itemDto.OrderItemId);
            _images.RemoveAll(i => i.Name.StartsWith(entryPrefix, StringComparison.Ordinal));
        }
    }

    private async Task HandleSubmit()
    {
        _isSubmitting = true;
        _errorMessage = null;
        try
        {
            (ReclamationDto _, DeliverTableSharedLibrary.Dtos.ErrorResponse? error) = await reclamationService.CreateReclamationAsync(Reclamation, _images);
            if (error is null)
                _isReclamationSend = true;
            else
                _errorMessage = error.Error;
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void GoToOrders() => navigationManager.NavigateTo("/mes-commandes");
}
