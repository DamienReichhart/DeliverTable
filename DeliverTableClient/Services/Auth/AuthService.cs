using System.Net.Http.Json;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Auth;
using Microsoft.JSInterop;

namespace DeliverTableClient.Services.Auth;

public class AuthService(HttpClient httpClient, ApiAuthStateProvider authStateProvider, IJSRuntime js)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ApiAuthStateProvider _authStateProvider = authStateProvider;
    private readonly IJSRuntime _js = js;

    public async Task<AuthResponse> Login(LoginRequest loginRequest)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Auth.Login, loginRequest);
        return await HandleResponse(response);
    }

    public async Task<AuthResponse> Register(RegisterRequest registerRequest)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Auth.Register, registerRequest);
        return await HandleResponse(response);
    }

    public async Task<AuthResponse> RegisterRestaurant(RestaurantRegister registerRequest)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Auth.RestaurantRegister, registerRequest);
        return await HandleResponse(response);
    }

    private async Task<AuthResponse> HandleResponse(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var errorContent = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                return new AuthResponse
                {
                    Success = false,
                    Error = errorContent?.Error ?? "Une erreur est survenue"
                };
            }
            catch
            {
                return new AuthResponse
                {
                    Success = false,
                    Error = "Une erreur est survenue"
                };
            }
        }

        ConnectionResponse? result;
        try
        {
            result = await response.Content.ReadFromJsonAsync<ConnectionResponse>();
        }
        catch
        {
            return new AuthResponse { Success = false, Error = "Une erreur est survenue." };
        }

        if (result?.User != null && !string.IsNullOrEmpty(result.Token))
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);

            _authStateProvider.NotifyUserAuthentication(
                result.Token,
                result.User.Role,
                result.User.Id.ToString(),
                result.User.FirstName
            );

            return new AuthResponse { Success = true };
        }

        return new AuthResponse { Success = false, Error = "Une erreur est survenue." };
    }

    public async Task Logout()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
        _authStateProvider.NotifyUserLogout();
    }
}
