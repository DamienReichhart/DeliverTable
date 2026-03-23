using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Auth;

namespace DeliverTableServer.Services.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<ConnectionResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ServiceResult<ConnectionResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ServiceResult<ConnectionResponse>> RegisterRestaurantAsync(RestaurantRegister request, CancellationToken ct = default);
    Task<ServiceResult<UserResponse>> GetProfileAsync(int userId, CancellationToken ct = default);
    Task<ServiceResult<ConnectionResponse>> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default);
}
