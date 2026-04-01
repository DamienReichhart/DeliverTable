using System.Net;
using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Reclamation;
using DeliverTableSharedLibrary.Interfaces;

namespace DeliverTableClient.Services;

public class ReclamationService(HttpClient httpClient) : IReclamationService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(ReclamationDto?, ErrorResponse?)> CreateReclamationAsync(
        CreateReclamationDto reclamation,
        List<Image> images,
        CancellationToken ct = default)
    {
        using MultipartFormDataContent content = new();

        content.Add(new StringContent(reclamation.OrderId.ToString()), "OrderId");
        content.Add(new StringContent(reclamation.Description ?? ""), "Description");
        content.Add(new StringContent(reclamation.Type ?? ""), "Type");

        for (int i = 0; i < reclamation.Items.Count; i++)
        {
            content.Add(new StringContent(reclamation.Items[i].OrderItemId.ToString()), $"Items[{i}].OrderItemId");
            content.Add(new StringContent(reclamation.Items[i].HasImage.ToString()), $"Items[{i}].HasImage");
        }
        foreach (Image image in images)
        {
            content.Add(image.Content, image.Name, image.Name);
        }

        var response = await _httpClient.PostAsync(ApiRoutes.Reclamation.Base, content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct)
                ?? new ErrorResponse { Error = "Une erreur est survenue lors de l'envoi de la réclamation." };
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<ReclamationDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(ReclamationDto?, ErrorResponse?)> GetByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"{ApiRoutes.Reclamation.Base}/order/{orderId}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return (null, null);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct)
                ?? new ErrorResponse { Error = "Impossible de charger la réclamation liée à cette commande." };
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<ReclamationDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(List<ReclamationDto>?, ErrorResponse?)> GetAllAsync(ReclamationQuery query, CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"PageNumber={query.PageNumber}",
            $"PageSize={query.PageSize}"
        };

        if (!string.IsNullOrWhiteSpace(query.ReclamationType))
            queryParams.Add($"ReclamationType={Uri.EscapeDataString(query.ReclamationType)}");
        if (!string.IsNullOrWhiteSpace(query.ReclamationStatus))
            queryParams.Add($"ReclamationStatus={Uri.EscapeDataString(query.ReclamationStatus)}");
        if (!string.IsNullOrWhiteSpace(query.Content))
            queryParams.Add($"Content={Uri.EscapeDataString(query.Content)}");

        var response = await _httpClient.GetAsync($"{ApiRoutes.Reclamation.Base}?{string.Join("&", queryParams)}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct)
                ?? new ErrorResponse { Error = "Impossible de charger les réclamations." };
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<List<ReclamationDto>>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(ReclamationDto?, ErrorResponse?)> UpdateAsync(
        int reclamationId,
        UpdateReclamationDto reclamation,
        CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Reclamation.Base}/{reclamationId}", reclamation, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct)
                ?? new ErrorResponse { Error = "Impossible de mettre à jour la réclamation." };
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<ReclamationDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int reclamationId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"{ApiRoutes.Reclamation.Base}/{reclamationId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct)
                ?? new ErrorResponse { Error = "Impossible de supprimer la réclamation." };
            return (false, error);
        }

        return (true, null);
    }

    public async Task<(List<ReclamationDto>?, ErrorResponse?)> GetByRestaurantOwnerAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(ApiRoutes.Reclamation.MyRestaurant, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct)
                ?? new ErrorResponse { Error = "Impossible de charger les réclamations de votre restaurant." };
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<List<ReclamationDto>>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(ReclamationDto?, ErrorResponse?)> RefundAsync(int reclamationId, RefundReclamationDto dto, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{ApiRoutes.Reclamation.Base}/{reclamationId}/refund", dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct)
                ?? new ErrorResponse { Error = "Impossible de traiter le remboursement." };
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<ReclamationDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(ReclamationDto?, ErrorResponse?)> ResolveAsync(int reclamationId, CancellationToken ct = default)
    {
        var response = await _httpClient.PatchAsync($"{ApiRoutes.Reclamation.Base}/{reclamationId}/resolve", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct)
                ?? new ErrorResponse { Error = "Impossible de résoudre la réclamation." };
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<ReclamationDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(ReclamationDto?, ErrorResponse?)> ContestAsync(int reclamationId, CancellationToken ct = default)
    {
        var response = await _httpClient.PatchAsync($"{ApiRoutes.Reclamation.Base}/{reclamationId}/contest", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct)
                ?? new ErrorResponse { Error = "Impossible de contester la réclamation." };
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<ReclamationDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(ReclamationDto?, ErrorResponse?)> CompleteAsync(int reclamationId, CancellationToken ct = default)
    {
        var response = await _httpClient.PatchAsync($"{ApiRoutes.Reclamation.Base}/{reclamationId}/complete", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct)
                ?? new ErrorResponse { Error = "Impossible de clôturer la réclamation." };
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<ReclamationDto>(cancellationToken: ct);
        return (result, null);
    }
}
