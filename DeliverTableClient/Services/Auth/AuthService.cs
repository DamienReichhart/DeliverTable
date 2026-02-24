using System.Net.Http.Json;
using DeliverTableSharedLibrary.Constants;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Authorization;
using DeliverTableSharedLibrary.Dtos.Auth;

namespace DeliverTableClient.Services.Auth;

public class AuthService(HttpClient httpClient, ApiAuthStateProvider authStateProvider, IJSRuntime js)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ApiAuthStateProvider _authStateProvider = authStateProvider;
    private readonly IJSRuntime _js = js;
    
    private sealed class ApiErrorResponse
    {
        public string Error { get; set; } = "";
    }

    public async Task<AuthResponse> Login(LoginRequest loginRequest)
    {
        var response = await _httpClient.PostAsJsonAsync( ApiRoutes.Auth["Login"], loginRequest);
        return await HandleResponse(response);
    }

    public async Task<AuthResponse> Register(RegisterRequest registerRequest)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Auth["Register"], registerRequest);
        return await HandleResponse(response);
    }

    public async Task<AuthResponse> RegisterRestaurant(RestaurantRegister registerRequest)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Auth["RestaurantRegister"], registerRequest);
        return await HandleResponse(response);
    }

    private async Task<AuthResponse> HandleResponse(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            try
            {
                // On tente de lire le message d'erreur envoyé par l'API
                var errorContent = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

                // Si ton API renvoie juste du texte, utilise errorContent.
                // Si elle renvoie un objet JSON { "message": "..." }, il faudra le désérialiser.
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
        };

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
            // Stockage
            await _js.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
            await _js.InvokeVoidAsync("localStorage.setItem", "userRole", result.User.Role);
            await _js.InvokeVoidAsync("localStorage.setItem", "userId", result.User.Id.ToString());
            await _js.InvokeVoidAsync("localStorage.setItem", "userName", result.User.FirstName);

            // Notification du Provider
            _authStateProvider.NotifyUserAuthentication(
                result.Token,
                result.User.Role, 
                result.User.Id.ToString(), 
                result.User.FirstName);

            return new AuthResponse {Success = true};
        }

        return new AuthResponse {Success = false, Error = "Une erreur est survenue."};
    }

    public async Task Logout()
    {
        // Nettoyage local
        await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
        await _js.InvokeVoidAsync("localStorage.removeItem", "userRole");
        await _js.InvokeVoidAsync("localStorage.removeItem", "userId");
        await _js.InvokeVoidAsync("localStorage.removeItem", "userName");

        // Notification de l'état déconnecté
        _authStateProvider.NotifyUserLogout();
    }
}