using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services;

public sealed class AdminOrderConfigClientService(HttpClient httpClient) : IAdminOrderConfigClientService
{
    private readonly HttpClient _httpClient = httpClient;

    // ── Order Rules ──

    public async Task<(List<AdminOrderRuleResponse>? Rules, ErrorResponse? Error)> GetAllRulesAsync(
        CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync(ApiRoutes.Admin.OrderRules, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var items = await response.Content.ReadFromJsonAsync<List<AdminOrderRuleResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire la liste des règles de commande", Status = (int)response.StatusCode });
    }

    public async Task<(AdminOrderRuleResponse? Rule, ErrorResponse? Error)> GetRuleByIdAsync(
        int id, CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync($"{ApiRoutes.Admin.OrderRules}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var item = await response.Content.ReadFromJsonAsync<AdminOrderRuleResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminOrderRuleResponse? Rule, ErrorResponse? Error)> CreateRuleAsync(
        AdminCreateOrderRuleRequest request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Admin.OrderRules, request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var item = await response.Content.ReadFromJsonAsync<AdminOrderRuleResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminOrderRuleResponse? Rule, ErrorResponse? Error)> UpdateRuleAsync(
        int id, AdminUpdateOrderRuleRequest request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Admin.OrderRules}/{id}", request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var item = await response.Content.ReadFromJsonAsync<AdminOrderRuleResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(bool Success, ErrorResponse? Error)> DeleteRuleAsync(int id, CancellationToken ct = default)
    {
        using var response = await _httpClient.DeleteAsync($"{ApiRoutes.Admin.OrderRules}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (false, await ReadError(response, ct));

        return (true, null);
    }

    // ── Blocked Slots ──

    public async Task<(List<AdminBlockedSlotResponse>? Slots, ErrorResponse? Error)> GetAllBlockedSlotsAsync(
        CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync(ApiRoutes.Admin.BlockedSlots, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var items = await response.Content.ReadFromJsonAsync<List<AdminBlockedSlotResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire la liste des créneaux bloqués", Status = (int)response.StatusCode });
    }

    public async Task<(AdminBlockedSlotResponse? Slot, ErrorResponse? Error)> GetBlockedSlotByIdAsync(
        int id, CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync($"{ApiRoutes.Admin.BlockedSlots}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var item = await response.Content.ReadFromJsonAsync<AdminBlockedSlotResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminBlockedSlotResponse? Slot, ErrorResponse? Error)> CreateBlockedSlotAsync(
        AdminCreateBlockedSlotRequest request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Admin.BlockedSlots, request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var item = await response.Content.ReadFromJsonAsync<AdminBlockedSlotResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(bool Success, ErrorResponse? Error)> DeleteBlockedSlotAsync(int id, CancellationToken ct = default)
    {
        using var response = await _httpClient.DeleteAsync($"{ApiRoutes.Admin.BlockedSlots}/{id}", ct);
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
