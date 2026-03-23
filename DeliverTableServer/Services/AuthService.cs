using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Models;
using DeliverTableServer.Mappers;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Enums;
using DeliverTableSharedLibrary.Dtos.Auth;

namespace DeliverTableServer.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    ITokenService tokenService
) : IAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ITokenService _tokenService = tokenService;
    private readonly string _defaultRole = nameof(UserRole.Customer);

    public async Task<ServiceResult<ConnectionResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null || !await _userRepository.CheckPasswordAsync(user, request.Password))
            return new ServiceError(ErrorMessages.InvalidCredentials, 401);

        if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Banned)
            return new ServiceError(ErrorMessages.AccountSuspendedOrBanned, 401);

        return await BuildConnectionResponse(user);
    }

    public async Task<ServiceResult<ConnectionResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email?.ToUpperInvariant();
        if (await _userRepository.EmailExistsAsync(normalizedEmail!, ct))
            return new ServiceError(ErrorMessages.EmailAlreadyUsed);

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Customer = new Customer()
        };

        var (created, errors) = await _userRepository.CreateAsync(user, request.Password);
        if (!created)
            return new ServiceError(string.Join(", ", errors));

        var (roleOk, _) = await _userRepository.AddToRoleAsync(user, _defaultRole);
        if (!roleOk)
            return new ServiceError(ErrorMessages.InternalError, 500);

        return await BuildConnectionResponse(user);
    }

    public async Task<ServiceResult<ConnectionResponse>> RegisterRestaurantAsync(RestaurantRegister request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email?.ToUpperInvariant();
        if (await _userRepository.EmailExistsAsync(normalizedEmail!, ct))
            return new ServiceError(ErrorMessages.EmailAlreadyUsed);

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            RestaurantOwner = new RestaurantOwner
            {
                CompanyName = request.CompanyName,
                VatNumber = request.VatNumber,
                ContactPhoneNumber = request.ContactPhoneNumber
            },
            Customer = new Customer()
        };

        var (created, errors) = await _userRepository.CreateAsync(user, request.Password);
        if (!created)
            return new ServiceError(string.Join(", ", errors));

        var (roleOk, _) = await _userRepository.AddToRoleAsync(user, nameof(UserRole.RestaurantOwner));
        if (!roleOk)
            return new ServiceError(ErrorMessages.InternalError, 500);

        return await BuildConnectionResponse(user);
    }

    public async Task<ServiceResult<UserResponse>> GetProfileAsync(int userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return new ServiceError(ErrorMessages.UserNotFound, 404);

        var role = await _userRepository.GetPrimaryRoleAsync(user) ?? _defaultRole;
        return user.ToDto(role);
    }

    public async Task<ServiceResult<ConnectionResponse>> UpdateProfileAsync(
        int userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return new ServiceError(ErrorMessages.UserNotFound, 404);

        var normalizedEmail = request.Email?.ToUpperInvariant();
        if (user.NormalizedEmail != normalizedEmail
            && await _userRepository.EmailExistsAsync(normalizedEmail!, ct))
        {
            return new ServiceError(ErrorMessages.EmailAlreadyUsed);
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.UserName = request.Email;
        user.NormalizedEmail = normalizedEmail;
        user.NormalizedUserName = normalizedEmail;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.SaveChangesAsync(ct);
        return await BuildConnectionResponse(user);
    }

    public async Task<ServiceResult> ChangePasswordAsync(
        int userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return new ServiceError(ErrorMessages.UserNotFound, 404);

        if (!await _userRepository.CheckPasswordAsync(user, request.CurrentPassword))
            return new ServiceError(ErrorMessages.CurrentPasswordIncorrect);

        var (succeeded, errors) = await _userRepository.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!succeeded)
            return new ServiceError(string.Join(", ", errors));

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    private async Task<ConnectionResponse> BuildConnectionResponse(User user)
    {
        var role = await _userRepository.GetPrimaryRoleAsync(user) ?? _defaultRole;
        var token = await _tokenService.CreateToken(user);
        return new ConnectionResponse { Token = token, User = user.ToDto(role) };
    }
}
