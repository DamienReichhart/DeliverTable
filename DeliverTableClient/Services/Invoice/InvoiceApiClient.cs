using System.Net.Http.Json;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableClient.Services.Invoice;

public class InvoiceApiClient(HttpClient http) : IInvoiceApiClient
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
}
