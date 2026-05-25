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
                if (query.IsDishOfTheDay is not null) queryString += $"&IsDishOfTheDay={Uri.EscapeDataString(query.IsDishOfTheDay.ToString() ?? string.Empty)}";
                if (query.IsVegetarian is not null) queryString += $"&IsVegetarian={Uri.EscapeDataString(query.IsVegetarian.ToString() ?? string.Empty)}";
                if (query.IsVegan is not null) queryString += $"&IsVegan={Uri.EscapeDataString(query.IsVegan.ToString() ?? string.Empty)}";
                if (query.IsGlutenFree is not null) queryString += $"&IsGlutenFree={Uri.EscapeDataString(query.IsGlutenFree.ToString() ?? string.Empty)}";
                if (query.IsAllergenHazard is not null) queryString += $"&IsAllergenHazard={Uri.EscapeDataString(query.IsAllergenHazard.ToString() ?? string.Empty)}";

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
                using var content = BuildDishFormContent(createDto, image);
                string url = ApiRoutes.Dish.DishesByRestaurantId.Replace("{id:int}", restaurantId.ToString());
                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                return await ReadDishResponse(response, cancellationToken);
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
                using var content = BuildDishFormContent(updateDto, image);
                string url = $"{ApiRoutes.Dish.Base}/{dishId}";
                var response = await _httpClient.PutAsync(url, content, cancellationToken);
                return await ReadDishResponse(response, cancellationToken);
            }
            catch (Exception ex)
            {
                return (null, new ErrorResponse { Error = ex.Message });
            }
        }

        private static MultipartFormDataContent BuildDishFormContent(CreateDishDto dto, IBrowserFile? image)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent(dto.Name ?? string.Empty), nameof(CreateDishDto.Name) },
                { new StringContent(dto.Description ?? string.Empty), nameof(CreateDishDto.Description) },
                { new StringContent(dto.BasePrice.ToString(System.Globalization.CultureInfo.InvariantCulture)), nameof(CreateDishDto.BasePrice) },
                { new StringContent(dto.IsVegetarian.ToString()), nameof(CreateDishDto.IsVegetarian) },
                { new StringContent(dto.IsVegan.ToString()), nameof(CreateDishDto.IsVegan) },
                { new StringContent(dto.IsGlutenFree.ToString()), nameof(CreateDishDto.IsGlutenFree) },
                { new StringContent(dto.IsAllergenHazard.ToString()), nameof(CreateDishDto.IsAllergenHazard) },
                { new StringContent(dto.IsDishOfTheDay.ToString()), nameof(CreateDishDto.IsDishOfTheDay) },
                { new StringContent(((int)dto.VatRate).ToString()), nameof(CreateDishDto.VatRate) },
            };

            if (image is not null)
            {
                var fileContent = new StreamContent(image.OpenReadStream(maxAllowedSize: MaxUploadBytes));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                content.Add(fileContent, "image", image.Name);
            }

            return content;
        }

        private static async Task<(DishDto?, ErrorResponse?)> ReadDishResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DishDto>(cancellationToken: cancellationToken);
                return (result, null);
            }

            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
            return (null, error ?? new ErrorResponse { Error = "Unknown error occurred" });
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
