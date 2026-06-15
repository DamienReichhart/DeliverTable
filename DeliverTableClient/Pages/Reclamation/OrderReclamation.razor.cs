using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Dtos.Reclamation;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableClient.Pages.Reclamation;

using Microsoft.AspNetCore.Components;

public partial class OrderReclamation : ComponentBase
{
    [Parameter]
    public required int OrderId { get; set; }
    private OrderDto? _orderDto;
    private ReclamationDto? _existingReclamation;
    private string? _errorMessage;
    private string? _contestSuccess;
    private string? _contestError;
    private bool isLoading = true;
    private bool _isContesting;

    [Inject]
    private IOrderService OrderService { get; set; } = null!;

    [Inject]
    private IReclamationService ReclamationService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        (OrderDto? order, DeliverTableSharedLibrary.Dtos.ErrorResponse? orderError) = await OrderService.GetOrderByIdAsync(OrderId);
        if (orderError is not null)
        {
            _errorMessage = orderError.Error;
            isLoading = false;
            return;
        }

        _orderDto = order;

        (ReclamationDto? reclamation, DeliverTableSharedLibrary.Dtos.ErrorResponse? reclamationError) = await ReclamationService.GetByOrderIdAsync(OrderId);
        if (reclamationError is not null)
        {
            _errorMessage = reclamationError.Error;
            isLoading = false;
            return;
        }

        _existingReclamation = reclamation;
        isLoading = false;
    }

    private async Task HandleContest()
    {
        if (_existingReclamation is null) return;
        _isContesting = true;
        _contestSuccess = null;
        _contestError = null;

        (ReclamationDto? updated, DeliverTableSharedLibrary.Dtos.ErrorResponse? error) = await ReclamationService.ContestAsync(_existingReclamation.ReclamationId);
        if (updated is not null)
        {
            _existingReclamation = updated;
            _contestSuccess = "Votre contestation a bien été enregistrée.";
        }
        else
        {
            _contestError = error?.Error ?? "Impossible de contester la réclamation.";
        }

        _isContesting = false;
    }

    private static string TranslateType(ReclamationType type) => type switch
    {
        ReclamationType.FoodQuality => "Qualité du repas",
        ReclamationType.ServiceIssue => "Problème de service",
        ReclamationType.WrongOrder => "Mauvaise commande",
        _ => "Autre motif"
    };

    private static string TranslateStatus(ReclamationStatus status) => status switch
    {
        ReclamationStatus.Pending => "En attente",
        ReclamationStatus.InProgress => "En cours de traitement",
        ReclamationStatus.Resolved => "Résolue",
        ReclamationStatus.Contested => "Contestée",
        ReclamationStatus.Completed => "Terminée",
        _ => status.ToString()
    };

    private static string TranslateStatusClass(ReclamationStatus status) => status switch
    {
        ReclamationStatus.Pending => "pending",
        ReclamationStatus.InProgress => "preparing",
        ReclamationStatus.Resolved => "confirmed",
        ReclamationStatus.Contested => "cancelled",
        ReclamationStatus.Completed => "delivered",
        _ => "pending"
    };
}
