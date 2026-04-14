using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dish;
using Microsoft.AspNetCore.Components.Forms;

namespace DeliverTableClient.Services
{
    public sealed class DishService(HttpClient httpClient) : IDishService
    {
        private static readonly long MaxUploadBytes = UploadLimits.ToBytes(UploadLimits.DefaultMaxSizeMb);
        private readonly HttpClient _httpClient = httpClient;

        public async Task<(PaginatedResult<DishDto>?, ErrorResponse?)> GetDishesByRestaurantId(int restaurantId, DishQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                string queryString = $"?PageNumber={query.PageNumber}&PageSize={query.PageSize}";
                if (!string.IsNullOrEmpty(query.Name)) queryString += $"&Name={Uri.EscapeDataString(query.Name)}";

                string url = $"{ApiRoutes.Dish.DishesByRestaurantId.Replace("{id:int}", restaurantId.ToString())}{queryString}";

                var result = await _httpClient.GetFromJsonAsync<PaginatedResult<DishDto>>(url, cancellationToken);
                return (result, null);
            }
            catch (Exception ex)
            {
                return (null, new ErrorResponse { Error = ex.Message });
            }
        }

        public async Task<(PaginatedResult<DishDto>?, ErrorResponse?)> GetAllDishes(DishQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                string queryString = $"?PageNumber={query.PageNumber}&PageSize={query.PageSize}";
                if (!string.IsNullOrEmpty(query.Name)) queryString += $"&Name={Uri.EscapeDataString(query.Name)}";
                if (!string.IsNullOrEmpty(query.IsDishOfTheDay.ToString())) queryString += $"&IsDishOfTheDay={Uri.EscapeDataString(query.IsDishOfTheDay.ToString() ?? "")}";
                if (!string.IsNullOrEmpty(query.IsVegetarian.ToString())) queryString += $"&IsVegetarian={Uri.EscapeDataString(query.IsVegetarian.ToString() ?? "")}";
                if (!string.IsNullOrEmpty(query.IsVegan.ToString())) queryString += $"&IsVegan={Uri.EscapeDataString(query.IsVegan.ToString() ?? "")}";
                if (!string.IsNullOrEmpty(query.IsGlutenFree.ToString())) queryString += $"&IsGlutenFree={Uri.EscapeDataString(query.IsGlutenFree.ToString() ?? "")}";

                string url = ApiRoutes.Dish.Base + queryString;
                var result = await _httpClient.GetFromJsonAsync<PaginatedResult<DishDto>>(url, cancellationToken);
                return (result, null);
            }
            catch (Exception ex)
            {
                return (null, new ErrorResponse { Error = ex.Message });
            }
        }

        public async Task<(DishDto?, ErrorResponse?)> CreateDish(int restaurantId, CreateDishDto createDto, IBrowserFile? image, CancellationToken cancellationToken = default)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                content.Add(new StringContent(createDto.Name ?? string.Empty), nameof(CreateDishDto.Name));
                content.Add(new StringContent(createDto.Description ?? string.Empty), nameof(CreateDishDto.Description));
                content.Add(new StringContent(createDto.BasePrice.ToString(System.Globalization.CultureInfo.InvariantCulture)), nameof(CreateDishDto.BasePrice));
                content.Add(new StringContent(createDto.IsVegetarian.ToString()), nameof(CreateDishDto.IsVegetarian));
                content.Add(new StringContent(createDto.IsVegan.ToString()), nameof(CreateDishDto.IsVegan));
                content.Add(new StringContent(createDto.IsGlutenFree.ToString()), nameof(CreateDishDto.IsGlutenFree));
                content.Add(new StringContent(createDto.IsAllergenHazard.ToString()), nameof(CreateDishDto.IsAllergenHazard));
                content.Add(new StringContent(createDto.IsDishOfTheDay.ToString()), nameof(CreateDishDto.IsDishOfTheDay));
                content.Add(new StringContent(((int)createDto.VatRate).ToString()), nameof(CreateDishDto.VatRate));

                if (image is not null)
                {
                    var fileContent = new StreamContent(image.OpenReadStream(maxAllowedSize: MaxUploadBytes));
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                    content.Add(fileContent, "image", image.Name);
                }

                string url = ApiRoutes.Dish.DishesByRestaurantId.Replace("{id:int}", restaurantId.ToString());
                var response = await _httpClient.PostAsync(url, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<DishDto>(cancellationToken: cancellationToken);
                    return (result, null);
                }
                else
                {
                    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
                    return (null, error ?? new ErrorResponse { Error = "Unknown error occurred" });
                }
            }
            catch (Exception ex)
            {
                return (null, new ErrorResponse { Error = ex.Message });
            }
        }

        public async Task<(DishDto?, ErrorResponse?)> UpdateDish(int dishId, CreateDishDto updateDto, IBrowserFile? image, CancellationToken cancellationToken = default)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                content.Add(new StringContent(updateDto.Name ?? string.Empty), nameof(CreateDishDto.Name));
                content.Add(new StringContent(updateDto.Description ?? string.Empty), nameof(CreateDishDto.Description));
                content.Add(new StringContent(updateDto.BasePrice.ToString(System.Globalization.CultureInfo.InvariantCulture)), nameof(CreateDishDto.BasePrice));
                content.Add(new StringContent(updateDto.IsVegetarian.ToString()), nameof(CreateDishDto.IsVegetarian));
                content.Add(new StringContent(updateDto.IsVegan.ToString()), nameof(CreateDishDto.IsVegan));
                content.Add(new StringContent(updateDto.IsGlutenFree.ToString()), nameof(CreateDishDto.IsGlutenFree));
                content.Add(new StringContent(updateDto.IsAllergenHazard.ToString()), nameof(CreateDishDto.IsAllergenHazard));
                content.Add(new StringContent(updateDto.IsDishOfTheDay.ToString()), nameof(CreateDishDto.IsDishOfTheDay));
                content.Add(new StringContent(((int)updateDto.VatRate).ToString()), nameof(CreateDishDto.VatRate));

                if (image is not null)
                {
                    var fileContent = new StreamContent(image.OpenReadStream(maxAllowedSize: MaxUploadBytes));
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                    content.Add(fileContent, "image", image.Name);
                }

                string url = $"{ApiRoutes.Dish.Base}/{dishId}";
                var response = await _httpClient.PutAsync(url, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<DishDto>(cancellationToken: cancellationToken);
                    return (result, null);
                }
                else
                {
                    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
                    return (null, error ?? new ErrorResponse { Error = "Unknown error occurred" });
                }
            }
            catch (Exception ex)
            {
                return (null, new ErrorResponse { Error = ex.Message });
            }
        }

        public async Task<ErrorResponse?> DeleteDish(int dishId, CancellationToken cancellationToken = default)
        {
            try
            {
                string url = $"{ApiRoutes.Dish.Base}/{dishId}";
                var response = await _httpClient.DeleteAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return null;
                }
                else
                {
                    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
                    return error ?? new ErrorResponse { Error = "Unknown error occurred" };
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse { Error = ex.Message };
            }
        }
    }
}
