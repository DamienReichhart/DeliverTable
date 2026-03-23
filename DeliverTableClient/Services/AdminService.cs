using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services;

public sealed class AdminService(HttpClient httpClient) : IAdminService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(List<AdminUserResponse>? Users, ErrorResponse? Error)> GetAllUsersAsync(
        CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync(ApiRoutes.Admin.Users, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var users = await response.Content.ReadFromJsonAsync<List<AdminUserResponse>>(cancellationToken: ct);
        return users is not null
            ? (users, null)
            : (null, new ErrorResponse { Error = "Impossible de lire la liste des utilisateurs", Status = (int)response.StatusCode });
    }

    public async Task<(AdminUserResponse? User, ErrorResponse? Error)> GetUserByIdAsync(
        int userId, CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync($"{ApiRoutes.Admin.Users}/{userId}", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var user = await response.Content.ReadFromJsonAsync<AdminUserResponse>(cancellationToken: ct);
        return (user, null);
    }

    public async Task<(AdminUserResponse? User, ErrorResponse? Error)> CreateUserAsync(
        AdminCreateUserRequest request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Admin.Users, request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var user = await response.Content.ReadFromJsonAsync<AdminUserResponse>(cancellationToken: ct);
        return (user, null);
    }

    public async Task<(AdminUserResponse? User, ErrorResponse? Error)> UpdateUserAsync(
        int userId, AdminUpdateUserRequest request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Admin.Users}/{userId}", request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var user = await response.Content.ReadFromJsonAsync<AdminUserResponse>(cancellationToken: ct);
        return (user, null);
    }

    public async Task<(bool Success, ErrorResponse? Error)> DeleteUserAsync(
        int userId, CancellationToken ct = default)
    {
        using var response = await _httpClient.DeleteAsync($"{ApiRoutes.Admin.Users}/{userId}", ct);
        if (!response.IsSuccessStatusCode)
            return (false, await ReadError(response, ct));

        return (true, null);
    }

    public async Task<(AdminUserResponse? User, ErrorResponse? Error)> UpdateUserRoleAsync(
        int userId, UpdateUserRoleRequest request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Admin.Users}/{userId}/role", request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var user = await response.Content.ReadFromJsonAsync<AdminUserResponse>(cancellationToken: ct);
        return (user, null);
    }

    public async Task<(AdminUserResponse? User, ErrorResponse? Error)> UpdateUserStatusAsync(
        int userId, UpdateUserStatusRequest request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Admin.Users}/{userId}/status", request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var user = await response.Content.ReadFromJsonAsync<AdminUserResponse>(cancellationToken: ct);
        return (user, null);
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
