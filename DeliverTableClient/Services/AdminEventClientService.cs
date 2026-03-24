using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services;

public sealed class AdminEventClientService(HttpClient httpClient) : IAdminEventClientService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(List<AdminEventResponse>? Events, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync(ApiRoutes.Admin.Events, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var items = await response.Content.ReadFromJsonAsync<List<AdminEventResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire la liste des événements", Status = (int)response.StatusCode });
    }

    public async Task<(AdminEventResponse? Event, ErrorResponse? Error)> GetByIdAsync(
        int id, CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync($"{ApiRoutes.Admin.Events}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var item = await response.Content.ReadFromJsonAsync<AdminEventResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminEventResponse? Event, ErrorResponse? Error)> CreateAsync(
        AdminCreateEventRequest request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Admin.Events, request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var item = await response.Content.ReadFromJsonAsync<AdminEventResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminEventResponse? Event, ErrorResponse? Error)> UpdateAsync(
        int id, AdminUpdateEventRequest request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Admin.Events}/{id}", request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var item = await response.Content.ReadFromJsonAsync<AdminEventResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int id, CancellationToken ct = default)
    {
        using var response = await _httpClient.DeleteAsync($"{ApiRoutes.Admin.Events}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (false, await ReadError(response, ct));

        return (true, null);
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
