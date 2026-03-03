using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Auth;

namespace DeliverTableClient.Services.Interfaces;

public interface IUserService
{
    /// <summary>Fetches the current user profile from <c>GET /api/v1/auth/me</c>.</summary>
    Task<(UserResponse? User, ErrorResponse? Error)> GetProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>Updates the current user's profile info (name, email) and returns a fresh token + user.</summary>
    Task<(ConnectionResponse? Connection, ErrorResponse? Error)> UpdateProfileAsync(
        UpdateProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>Changes the current user's password.</summary>
    Task<(bool Success, ErrorResponse? Error)> ChangePasswordAsync(
        ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
