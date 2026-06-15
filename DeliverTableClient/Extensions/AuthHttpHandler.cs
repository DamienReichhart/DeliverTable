using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeliverTableClient.Services.Auth;
using Microsoft.AspNetCore.Components;

namespace DeliverTableClient.Extensions
{
    public class AuthHttpHandler(
        NavigationManager navigation,
        AuthService authService
    ) : DelegatingHandler
    {
        private readonly AuthService _authService = authService;
        private NavigationManager _navigation = navigation;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await _authService.Logout();
                _navigation.NavigateTo("/login");
            }

            return response;
        }
    }
}