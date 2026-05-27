using System.Security.Claims;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace DeliverTableClient.Pages.Home;

public partial class Home(
    IRestaurantService restaurantService,
    AuthenticationStateProvider authStateProvider,
    NavigationManager nav
)
{
    private NavigationManager Nav { get; } = nav;

    private List<RestaurantDto>? _restaurants;
    private bool _loading = true;
    private string _greeting = "Bonjour";
    private string? _userName;
    private string? _activeType;

    protected override async Task OnInitializedAsync()
    {
        _greeting = DateTime.Now.Hour switch
        {
            >= 18 => "Bonsoir",
            >= 12 => "Bon après-midi",
            _ => "Bonjour"
        };

        var authState = await authStateProvider.GetAuthenticationStateAsync();
        var name = authState.User.FindFirst(ClaimTypes.Name)?.Value;
        _userName = !string.IsNullOrWhiteSpace(name) ? name : null;

        await LoadRestaurants();
    }

    private async Task SetTypeFilter(string? type)
    {
        _activeType = type;
        await LoadRestaurants();
    }

    private async Task LoadRestaurants()
    {
        _loading = true;
        StateHasChanged();

        var query = new RestaurantQuery { PageSize = 8, Type = _activeType };
        var (result, _) = await restaurantService.GetAllRestaurants(query);
        _restaurants = result?.Items;

        _loading = false;
        StateHasChanged();
    }

    private void NavigateToRestaurant(int id) => Nav.NavigateTo($"/restaurant/{id}");

    private static string GetCardColorClass(string type) => type switch
    {
        nameof(RestaurantType.Italien) => "discover-card--orange",
        nameof(RestaurantType.Asiatique) => "discover-card--green",
        nameof(RestaurantType.Français) => "discover-card--purple",
        nameof(RestaurantType.FastFood) => "discover-card--yellow",
        nameof(RestaurantType.Traditionnel) => "discover-card--blue",
        nameof(RestaurantType.Oriental) => "discover-card--red",
        _ => "discover-card--teal"
    };
}
