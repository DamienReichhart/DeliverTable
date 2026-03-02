using System.IdentityModel.Tokens.Jwt;
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
        string token = await _js.InvokeAsync<string>(_getItem, "authToken");
        string role = await _js.InvokeAsync<string>(_getItem, "userRole");
        string userId = await _js.InvokeAsync<string>(_getItem, "userId");
        string userName = await _js.InvokeAsync<string>(_getItem, "userName");

        if (string.IsNullOrWhiteSpace(token) || IsTokenExpired(token))
        {
            await CleanUpStorage();
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);

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

    private async Task CleanUpStorage()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
        await _js.InvokeVoidAsync("localStorage.removeItem", "userRole");
        await _js.InvokeVoidAsync("localStorage.removeItem", "userId");
        await _js.InvokeVoidAsync("localStorage.removeItem", "userName");
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    private static bool IsTokenExpired(string token)
    {
        try
        {
            JwtSecurityTokenHandler handler = new();
            JwtSecurityToken jwtToken = handler.ReadJwtToken(token);

            return jwtToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }
}