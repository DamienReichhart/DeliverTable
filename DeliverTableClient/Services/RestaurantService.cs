using System.Net.Http.Json;
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
            var response = await _httpClient.PostAsJsonAsync(ApiRoutes.RestaurantEndpoints["Create"], creationDto);
            if (response.IsSuccessStatusCode)
            {
                await response.Content.ReadFromJsonAsync<RestaurantDto>(cancellationToken: cancellationToken);
                return true;
            }
            else
            {
                ErrorResponse? errorContent = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
                if(errorContent != null)
                {
                    throw new ArgumentException(errorContent.Error);
                }
                return false;
            }
        }
    }
}