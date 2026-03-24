using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services;

public sealed class AdminModerationClientService(HttpClient httpClient) : IAdminModerationClientService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(List<AdminModerationActionResponse>? Actions, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync(ApiRoutes.Admin.Moderation, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var items = await response.Content.ReadFromJsonAsync<List<AdminModerationActionResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire la liste des actions de modération", Status = (int)response.StatusCode });
    }

    public async Task<(AdminModerationActionResponse? Action, ErrorResponse? Error)> GetByIdAsync(
        int id, CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync($"{ApiRoutes.Admin.Moderation}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var item = await response.Content.ReadFromJsonAsync<AdminModerationActionResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminModerationActionResponse? Action, ErrorResponse? Error)> CreateAsync(
        AdminCreateModerationActionRequest request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Admin.Moderation, request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var item = await response.Content.ReadFromJsonAsync<AdminModerationActionResponse>(cancellationToken: ct);
        return (item, null);
    }

    private static async Task<ErrorResponse> ReadError(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return error ?? new ErrorResponse { Error = "Une erreur est survenue", Status = (int)response.StatusCode };
        }
        catch
        {
            return new ErrorResponse { Error = "Une erreur est survenue", Status = (int)response.StatusCode };
        }
    }
}
