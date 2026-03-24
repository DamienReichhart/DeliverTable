using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services;

public sealed class AdminLoyaltyClientService(HttpClient httpClient) : IAdminLoyaltyClientService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(List<AdminLoyaltyProgramResponse>? Programs, ErrorResponse? Error)> GetAllProgramsAsync(
        CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync(ApiRoutes.Admin.LoyaltyPrograms, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var items = await response.Content.ReadFromJsonAsync<List<AdminLoyaltyProgramResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire la liste des programmes de fidélité", Status = (int)response.StatusCode });
    }

    public async Task<(AdminLoyaltyProgramResponse? Program, ErrorResponse? Error)> GetProgramByIdAsync(
        int id, CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync($"{ApiRoutes.Admin.LoyaltyPrograms}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var item = await response.Content.ReadFromJsonAsync<AdminLoyaltyProgramResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminLoyaltyProgramResponse? Program, ErrorResponse? Error)> CreateProgramAsync(
        AdminCreateLoyaltyProgramRequest request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Admin.LoyaltyPrograms, request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var item = await response.Content.ReadFromJsonAsync<AdminLoyaltyProgramResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminLoyaltyProgramResponse? Program, ErrorResponse? Error)> UpdateProgramAsync(
        int id, AdminUpdateLoyaltyProgramRequest request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Admin.LoyaltyPrograms}/{id}", request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var item = await response.Content.ReadFromJsonAsync<AdminLoyaltyProgramResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(bool Success, ErrorResponse? Error)> DeleteProgramAsync(int id, CancellationToken ct = default)
    {
        using var response = await _httpClient.DeleteAsync($"{ApiRoutes.Admin.LoyaltyPrograms}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (false, await ReadError(response, ct));

        return (true, null);
    }

    public async Task<(List<AdminLoyaltyAccountResponse>? Accounts, ErrorResponse? Error)> GetAccountsAsync(
        int programId, CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync($"{ApiRoutes.Admin.LoyaltyPrograms}/{programId}/accounts", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var items = await response.Content.ReadFromJsonAsync<List<AdminLoyaltyAccountResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire la liste des comptes fidélité", Status = (int)response.StatusCode });
    }

    public async Task<(List<AdminLoyaltyTransactionResponse>? Transactions, ErrorResponse? Error)> GetTransactionsAsync(
        int programId, int accountId, CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync(
            $"{ApiRoutes.Admin.LoyaltyPrograms}/{programId}/accounts/{accountId}/transactions", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var items = await response.Content.ReadFromJsonAsync<List<AdminLoyaltyTransactionResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire la liste des transactions fidélité", Status = (int)response.StatusCode });
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
