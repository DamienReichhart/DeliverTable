using System.Net.Http.Headers;
using Microsoft.JSInterop;

public class JwtInterceptor : DelegatingHandler
{
    private readonly IJSRuntime _js;

    public JwtInterceptor(IJSRuntime js) => _js = js;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string token = await _js.InvokeAsync<string>("localStorage.getItem", "authToken");

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}