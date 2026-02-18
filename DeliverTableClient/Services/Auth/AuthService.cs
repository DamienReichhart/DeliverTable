using System.Net.Http.Json;
using DeliverTableSharedLibrary.Constants;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Authorization;
using DeliverTableSharedLibrary.Dtos.Auth;

namespace DeliverTableClient.Services.Auth;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IJSRuntime _js;

    public AuthService(HttpClient httpClient, AuthenticationStateProvider authStateProvider, IJSRuntime js)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
        _js = js;
    }

    public async Task<bool> Login(LoginRequest loginRequest)
    {
        var response = await _httpClient.PostAsJsonAsync( ApiRoutes.Login, loginRequest);
        return await HandleResponse(response);
    }

    public async Task<bool> Register(RegisterRequest registerRequest)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Register, registerRequest);
        return await HandleResponse(response);
    }

    private async Task<bool> HandleResponse(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) return false;

        var result = await response.Content.ReadFromJsonAsync<ConnectionResponse>();

        if (result != null && !string.IsNullOrEmpty(result.Token))
        {
            // Stockage
            await _js.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
            await _js.InvokeVoidAsync("localStorage.setItem", "userRole", result.User.Role);
            await _js.InvokeVoidAsync("localStorage.setItem", "userId", result.User.Id.ToString());
            await _js.InvokeVoidAsync("localStorage.setItem", "userName", result.User.FirstName);

            // Notification du Provider
            ((ApiAuthStateProvider)_authStateProvider).NotifyUserAuthentication(
                result.User.Role, 
                result.User.Id.ToString(), 
                result.User.FirstName);

            return true;
        }

        return false;
    }

    public async Task Logout()
    {
        // Nettoyage local
        await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
        await _js.InvokeVoidAsync("localStorage.removeItem", "userRole");
        await _js.InvokeVoidAsync("localStorage.removeItem", "userId");
        await _js.InvokeVoidAsync("localStorage.removeItem", "userName");

        // Notification de l'état déconnecté
        ((ApiAuthStateProvider)_authStateProvider).NotifyUserLogout();
    }
}