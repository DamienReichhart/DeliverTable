using System.Net.Http.Json;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.CommissionStatement;
using DeliverTableSharedLibrary.Enums;
using Microsoft.JSInterop;

namespace DeliverTableClient.Services.CommissionStatement;

public class CommissionStatementApiClient(HttpClient http, IJSRuntime js) : ICommissionStatementApiClient
{
    public async Task<CommissionStatementGenerationResultDto?> RunAsync(int? year, int? month)
    {
        var url = $"{ApiRoutes.Admin.Base}/{ApiRoutes.Admin.CommissionStatementsRunRoute}";
        var body = (year.HasValue || month.HasValue)
            ? (object?)new { year, month }
            : null;
        var response = await http.PostAsJsonAsync(url, body);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CommissionStatementGenerationResultDto>();
    }

    public async Task<PaginatedResult<AdminCommissionStatementRowDto>?> AdminListAsync(
        int? year, CommissionStatementKind? kind, int? restaurantId, int page, int pageSize)
    {
        var qs = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}",
        };
        if (year.HasValue) qs.Add($"year={year.Value}");
        if (kind.HasValue) qs.Add($"kind={kind.Value}");
        if (restaurantId.HasValue) qs.Add($"restaurantId={restaurantId.Value}");

        var url = $"{ApiRoutes.Admin.Base}/{ApiRoutes.Admin.CommissionStatementsRoute}?{string.Join("&", qs)}";
        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PaginatedResult<AdminCommissionStatementRowDto>>();
    }

    public async Task<AdminCommissionStatementDetailDto?> AdminGetAsync(int id)
    {
        var url = $"{ApiRoutes.Admin.Base}/commission-statements/{id}";
        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AdminCommissionStatementDetailDto>();
    }

    public async Task DownloadPdfAsync(int id)
    {
        var url = $"{ApiRoutes.Admin.Base}/commission-statements/{id}/pdf";
        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return;

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"releve-{id}.pdf";

        await js.InvokeVoidAsync("downloadFileFromBase64", fileName, contentType, Convert.ToBase64String(bytes));
    }

    public async Task<PaginatedResult<AdminCommissionStatementRowDto>?> GetForRestaurantAsync(int restaurantId, int page, int pageSize)
    {
        var url = $"{ApiRoutes.CommissionStatement.Base}/restaurant/{restaurantId}?page={page}&pageSize={pageSize}";
        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PaginatedResult<AdminCommissionStatementRowDto>>();
    }

    public async Task DownloadOwnerPdfAsync(int id)
    {
        var url = $"{ApiRoutes.CommissionStatement.Base}/{id}/pdf";
        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return;

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"releve-commissions-{id}.pdf";

        await js.InvokeVoidAsync("downloadFileFromBase64", fileName, contentType, Convert.ToBase64String(bytes));
    }
}
