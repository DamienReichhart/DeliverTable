using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Restaurant;

namespace DeliverTableClient.Services
{
    public sealed class RestaurantService(HttpClient httpClient) : IRestaurantService
    {
        private readonly HttpClient _httpClient = httpClient;

        public async Task<bool> CreateRestaurant(CreateRestaurantDto creationDto, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage? response = await _httpClient.PostAsJsonAsync(ApiRoutes.Restaurant.Base, creationDto);
            if (response.IsSuccessStatusCode)
            {
                await response.Content.ReadFromJsonAsync<RestaurantDto>(cancellationToken: cancellationToken);
                return true;
            }
            else
            {
                ErrorResponse? errorContent = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
                if (errorContent != null)
                {
                    throw new ArgumentException(errorContent.Error);
                }
                return false;
            }
        }

        public async Task<(PaginatedResult<RestaurantDto>?, ErrorResponse?)> GetConnectedUserRestaurants(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _httpClient
                    .GetFromJsonAsync<PaginatedResult<RestaurantDto>>(ApiRoutes.Restaurant.UserMe, cancellationToken);

                return (result, null);
            }
            catch (Exception jsonEx)
            {
                return (null, new ErrorResponse { Error = jsonEx.Message });
            }
        }

        public async Task<bool> DeleteRestaurant(int id)
        {
            var response = await _httpClient.DeleteAsync($"{ApiRoutes.Restaurant.Base}/{id}");

            if (response.IsSuccessStatusCode)
                return true;

            ErrorResponse? errorContent = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            throw new HttpRequestException(
                errorContent?.Error ?? $"Delete failed for restaurant {id} (HTTP {(int)response.StatusCode})");
        }

        public async Task<(DetailedRestaurantDto?, ErrorResponse?)> GetRestaurantById(int id, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{ApiRoutes.Restaurant.Base}/{id}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                return (null, error);
            }

            var result = await response.Content.ReadFromJsonAsync<DetailedRestaurantDto>();
            return (result, null);
        }

        public async Task<(DetailedRestaurantDto? dto, ErrorResponse? error)> UpdateRestaurant(UpdateRestaurantDto updateDto, int id, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Restaurant.Base}/{id}", updateDto, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                return (null, error);
            }
            var result = await response.Content.ReadFromJsonAsync<DetailedRestaurantDto>();
            return (result, null);
        }

        public async Task<(PaginatedResult<RestaurantDto>?, ErrorResponse?)> GetAllRestaurants(RestaurantQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(query.Name)) queryParams.Add($"Name={Uri.EscapeDataString(query.Name)}");
                if (!string.IsNullOrWhiteSpace(query.City)) queryParams.Add($"City={Uri.EscapeDataString(query.City)}");
                if (!string.IsNullOrWhiteSpace(query.Type)) queryParams.Add($"Type={Uri.EscapeDataString(query.Type)}");
                if (query.Latitude.HasValue) queryParams.Add($"Latitude={query.Latitude.Value.ToString(CultureInfo.InvariantCulture)}");
                if (query.Longitude.HasValue) queryParams.Add($"Longitude={query.Longitude.Value.ToString(CultureInfo.InvariantCulture)}");
                if (query.RadiusKm.HasValue) queryParams.Add($"RadiusKm={query.RadiusKm.Value.ToString(CultureInfo.InvariantCulture)}");
                queryParams.Add($"PageNumber={query.PageNumber}");
                queryParams.Add($"PageSize={query.PageSize}");

                var url = $"{ApiRoutes.Restaurant.Base}?{string.Join("&", queryParams)}";
                var result = await _httpClient.GetFromJsonAsync<PaginatedResult<RestaurantDto>>(url, cancellationToken);
                return (result, null);
            }
            catch (Exception ex)
            {
                return (null, new ErrorResponse { Error = ex.Message });
            }
        }

        public async Task<(List<RestaurantMapDto>?, ErrorResponse?)> GetRestaurantsForMap(RestaurantQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(query.Name)) queryParams.Add($"Name={Uri.EscapeDataString(query.Name)}");
                if (!string.IsNullOrWhiteSpace(query.Type)) queryParams.Add($"Type={Uri.EscapeDataString(query.Type)}");
                if (query.Latitude.HasValue) queryParams.Add($"Latitude={query.Latitude.Value.ToString(CultureInfo.InvariantCulture)}");
                if (query.Longitude.HasValue) queryParams.Add($"Longitude={query.Longitude.Value.ToString(CultureInfo.InvariantCulture)}");
                if (query.RadiusKm.HasValue) queryParams.Add($"RadiusKm={query.RadiusKm.Value.ToString(CultureInfo.InvariantCulture)}");

                var url = $"{ApiRoutes.Restaurant.Map}?{string.Join("&", queryParams)}";
                var result = await _httpClient.GetFromJsonAsync<List<RestaurantMapDto>>(url, cancellationToken);
                return (result, null);
            }
            catch (Exception ex)
            {
                return (null, new ErrorResponse { Error = ex.Message });
            }
        }
    }
}