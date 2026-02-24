using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace DeliverTableClient.Services.Auth;

public class ApiAuthStateProvider(IJSRuntime js, HttpClient httpClient) : AuthenticationStateProvider
{
    private readonly IJSRuntime _js = js;
    private readonly HttpClient _httpClient = httpClient;

    private readonly string _getItem = "localStorage.getItem";

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _js.InvokeAsync<string>(_getItem, "authToken");
        var role = await _js.InvokeAsync<string>(_getItem, "userRole");
        var userId = await _js.InvokeAsync<string>(_getItem, "userId");
        var userName = await _js.InvokeAsync<string>(_getItem, "userName");

        if (string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        // Configure le header Authorization pour tous les appels HttpClient
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);

        // On construit l'identité avec tous les claims nécessaires
        var claims = new List<Claim> 
        { 
            new (ClaimTypes.Role, role ?? "Customer"),
            new (ClaimTypes.NameIdentifier, userId ?? ""),
            new (ClaimTypes.Name, userName ?? "")
        };
        
        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyUserAuthentication(string token, string role, string userId, string userName)
    {
        
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var claims = new List<Claim> 
        { 
            new (ClaimTypes.Role, role),
            new (ClaimTypes.NameIdentifier, userId),
            new (ClaimTypes.Name, userName)
        };
        
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        NotifyAuthenticationStateChanged(authState);
    }

    public void NotifyUserLogout()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
    }
}