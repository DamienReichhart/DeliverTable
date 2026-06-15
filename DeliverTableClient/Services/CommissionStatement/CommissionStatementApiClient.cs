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
        string url = $"{ApiRoutes.Admin.Base}/{ApiRoutes.Admin.CommissionStatementsRunRoute}";
        object? body = (year.HasValue || month.HasValue)
            ? (object?)new { year, month }
            : null;
        HttpResponseMessage response = await http.PostAsJsonAsync(url, body);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CommissionStatementGenerationResultDto>();
    }

    public async Task<PaginatedResult<AdminCommissionStatementRowDto>?> AdminListAsync(
        int? year, CommissionStatementKind? kind, int? restaurantId, int page, int pageSize)
    {
        List<string> qs = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}",
        };
        if (year.HasValue) qs.Add($"year={year.Value}");
        if (kind.HasValue) qs.Add($"kind={kind.Value}");
        if (restaurantId.HasValue) qs.Add($"restaurantId={restaurantId.Value}");

        string url = $"{ApiRoutes.Admin.Base}/{ApiRoutes.Admin.CommissionStatementsRoute}?{string.Join("&", qs)}";
        HttpResponseMessage response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PaginatedResult<AdminCommissionStatementRowDto>>();
    }

    public async Task<AdminCommissionStatementDetailDto?> AdminGetAsync(int id)
    {
        string url = $"{ApiRoutes.Admin.Base}/commission-statements/{id}";
        HttpResponseMessage response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AdminCommissionStatementDetailDto>();
    }

    public async Task DownloadPdfAsync(int id)
    {
        string url = $"{ApiRoutes.Admin.Base}/commission-statements/{id}/pdf";
        HttpResponseMessage response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return;

        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf";
        string fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"releve-{id}.pdf";

        await js.InvokeVoidAsync("downloadFileFromBase64", fileName, contentType, Convert.ToBase64String(bytes));
    }

    public async Task<PaginatedResult<AdminCommissionStatementRowDto>?> GetForRestaurantAsync(int restaurantId, int page, int pageSize)
    {
        string url = $"{ApiRoutes.CommissionStatement.Base}/restaurant/{restaurantId}?page={page}&pageSize={pageSize}";
        HttpResponseMessage response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PaginatedResult<AdminCommissionStatementRowDto>>();
    }

    public async Task DownloadOwnerPdfAsync(int id)
    {
        string url = $"{ApiRoutes.CommissionStatement.Base}/{id}/pdf";
        HttpResponseMessage response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return;

        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf";
        string fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"releve-commissions-{id}.pdf";

        await js.InvokeVoidAsync("downloadFileFromBase64", fileName, contentType, Convert.ToBase64String(bytes));
    }
}
