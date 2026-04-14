using System.Net.Http.Json;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;
using Microsoft.JSInterop;

namespace DeliverTableClient.Services.Invoice;

public class InvoiceApiClient(HttpClient http, IJSRuntime js) : IInvoiceApiClient
{
    public async Task<PaginatedResult<InvoiceListItemDto>?> GetMineAsync(int page, int pageSize)
    {
        var url = $"{ApiRoutes.Invoice.Base}/{ApiRoutes.Invoice.MyListRoute}?page={page}&pageSize={pageSize}";
        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PaginatedResult<InvoiceListItemDto>>();
    }

    public async Task<PaginatedResult<InvoiceListItemDto>?> GetForRestaurantAsync(int restaurantId, int page, int pageSize)
    {
        var url = $"{ApiRoutes.Invoice.Base}/restaurant/{restaurantId}?page={page}&pageSize={pageSize}";
        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PaginatedResult<InvoiceListItemDto>>();
    }

    public async Task<PaginatedResult<AdminInvoiceRowDto>?> AdminListAsync(
        int? year,
        InvoiceKind? kind,
        InvoiceIssuerType? issuerType,
        int? restaurantId,
        string? customerEmail,
        int page,
        int pageSize)
    {
        var qs = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}",
        };
        if (year.HasValue) qs.Add($"year={year.Value}");
        if (kind.HasValue) qs.Add($"kind={kind.Value}");
        if (issuerType.HasValue) qs.Add($"issuerType={issuerType.Value}");
        if (restaurantId.HasValue) qs.Add($"restaurantId={restaurantId.Value}");
        if (!string.IsNullOrWhiteSpace(customerEmail)) qs.Add($"customerEmail={Uri.EscapeDataString(customerEmail)}");

        var url = $"{ApiRoutes.Admin.Base}/{ApiRoutes.Admin.InvoicesRoute}?{string.Join("&", qs)}";
        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PaginatedResult<AdminInvoiceRowDto>>();
    }

    public async Task<AdminInvoiceDetailDto?> AdminGetAsync(int id)
    {
        var url = $"{ApiRoutes.Admin.Base}/invoices/{id}";
        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AdminInvoiceDetailDto>();
    }

    public async Task AdminResendEmailAsync(int id)
    {
        await http.PostAsync($"{ApiRoutes.Admin.Base}/invoices/{id}/resend-email", null);
    }

    public async Task DownloadPdfAsync(int id)
    {
        var url = $"{ApiRoutes.Invoice.Base}/{id}/pdf";
        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return;

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"facture-{id}.pdf";

        await js.InvokeVoidAsync("downloadFileFromBase64", fileName, contentType, Convert.ToBase64String(bytes));
    }
}
