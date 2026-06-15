using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos.Auth;
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

        if (string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);

        HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Auth.Me);

        if (!response.IsSuccessStatusCode)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        UserResponse? user = await response.Content.ReadFromJsonAsync<UserResponse>();

        if (user == null)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        List<Claim> claims = [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FirstName),
            new(ClaimTypes.Role, user.Role),
        ];

        ClaimsIdentity identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyUserAuthentication(string token, string role, string id, string firstname)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        List<Claim> claims = [
            new (ClaimTypes.NameIdentifier, id),
            new (ClaimTypes.Name, firstname),
            new (ClaimTypes.Role, role),
        ];

        ClaimsPrincipal authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
        Task<AuthenticationState> authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        NotifyAuthenticationStateChanged(authState);
    }

    public void NotifyUserLogout()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
        ClaimsPrincipal anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        Task<AuthenticationState> authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
    }
}