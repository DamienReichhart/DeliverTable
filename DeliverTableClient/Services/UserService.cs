using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Auth;

namespace DeliverTableClient.Services;

public sealed class UserService(HttpClient httpClient) : IUserService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(UserResponse? User, ErrorResponse? Error)> GetProfileAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ApiRoutes.Auth.Me, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadError(response, cancellationToken);
            return (null, error);
        }

        var user = await response.Content.ReadFromJsonAsync<UserResponse>(cancellationToken: cancellationToken);
        return (user, null);
    }

    public async Task<(ConnectionResponse? Connection, ErrorResponse? Error)> UpdateProfileAsync(
        UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(ApiRoutes.Auth.UpdateProfile, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadError(response, cancellationToken);
            return (null, error);
        }

        var connection = await response.Content.ReadFromJsonAsync<ConnectionResponse>(cancellationToken: cancellationToken);
        return (connection, null);
    }

    public async Task<(bool Success, ErrorResponse? Error)> ChangePasswordAsync(
        ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(ApiRoutes.Auth.ChangePassword, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadError(response, cancellationToken);
            return (false, error);
        }

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
