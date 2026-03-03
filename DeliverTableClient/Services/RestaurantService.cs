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

        public async Task<(List<RestaurantDto>?, ErrorResponse?)> GetConnectedUserRestaurants(CancellationToken cancellationToken = default)
        {
            try
            {
                List<RestaurantDto>? restaurants = await _httpClient
                    .GetFromJsonAsync<List<RestaurantDto>>(ApiRoutes.Restaurant.UserMe, cancellationToken);

                return (restaurants, null);
            }
            catch (Exception jsonEx)
            {
                return (null, new ErrorResponse { Error = jsonEx.Message });
            }
        }

        public async Task<bool> DeleteRestaurant(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{ApiRoutes.Restaurant.Base}/{id}");
                Console.WriteLine(response);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
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
    }
}