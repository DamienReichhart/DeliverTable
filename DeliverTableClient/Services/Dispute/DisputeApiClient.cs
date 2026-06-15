using System.Net.Http.Json;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dispute;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableClient.Services.Dispute;

public class DisputeApiClient(HttpClient http) : IDisputeApiClient
{
    public async Task<PaginatedResult<DisputeRowDto>?> GetForRestaurantAsync(
        int restaurantId, int page, int pageSize)
    {
        string url = $"{ApiRoutes.Dispute.Base}/restaurant/{restaurantId}?page={page}&pageSize={pageSize}";
        HttpResponseMessage response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PaginatedResult<DisputeRowDto>>();
    }

    public async Task<PaginatedResult<AdminDisputeRowDto>?> AdminListAsync(
        DisputeState? state, int? year, int? restaurantId, int? orderId, int page, int pageSize)
    {
        List<string> qs = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}",
        };
        if (state.HasValue) qs.Add($"state={state.Value}");
        if (year.HasValue) qs.Add($"year={year.Value}");
        if (restaurantId.HasValue) qs.Add($"restaurantId={restaurantId.Value}");
        if (orderId.HasValue) qs.Add($"orderId={orderId.Value}");

        string url = $"{ApiRoutes.Admin.Base}/{ApiRoutes.Admin.DisputesRoute}?{string.Join("&", qs)}";
        HttpResponseMessage response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PaginatedResult<AdminDisputeRowDto>>();
    }

    public async Task<AdminDisputeDetailDto?> AdminGetAsync(int id)
    {
        string url = $"{ApiRoutes.Admin.Base}/disputes/{id}";
        HttpResponseMessage response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AdminDisputeDetailDto>();
    }
}
