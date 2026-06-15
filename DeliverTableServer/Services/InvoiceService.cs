using System.Text.Json;
using DeliverTableInfrastructure.Extensions;
using DeliverTableInfrastructure.Invoicing;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableInfrastructure.Services.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;
using DeliverTableSharedLibrary.Extensions;

namespace DeliverTableServer.Services;

public class InvoiceService(
    IInvoiceRepository invoiceRepository,
    IOrderRepository orderRepository,
    IInvoiceNumberingService numbering,
    IPaymentRepository paymentRepository,
    IRestaurantRepository restaurantRepository,
    IObjectStorageService objectStorage,
    IEmailJobRepository emailJobRepository,
    IMessagePublisher messagePublisher,
    AppEnvironment env,
    ISystemClock clock) : IInvoiceService
{
    public async Task<ServiceResult<List<InvoiceJobMessage>>> CreatePendingInvoicesForCapturedOrderAsync(
        int orderId,
        CancellationToken ct)
    {
        Order? order = await orderRepository.GetByIdWithFullDetailsAsync(orderId, ct);
        if (order is null)
            return new ServiceError(ErrorMessages.OrderNotFound);

        if (await invoiceRepository.ExistsForOrderAndKindAsync(orderId, InvoiceKind.OrderInvoiceToCustomer, ct))
            return ServiceResult<List<InvoiceJobMessage>>.Success(new List<InvoiceJobMessage>());

        int year = DateTime.UtcNow.Year;
        Restaurant restaurant = order.Restaurant;
        User customer = order.Customer;

        string customerNumber = await numbering.IssueNumberAsync(
            InvoiceIssuerType.Restaurant, restaurant.Id, year, false, ct);
        Invoice customerInvoice = BuildCustomerInvoice(order, restaurant, customer, customerNumber);

        List<Invoice> invoicesToCreate = new List<Invoice> { customerInvoice };

        if (clock.UtcNow < CommissionInvoicingCutover.MonthlyStartUtc)
        {
            string platformNumber = await numbering.IssueNumberAsync(
                InvoiceIssuerType.Platform, null, year, false, ct);
            Invoice commissionInvoice = BuildCommissionInvoice(order, restaurant, platformNumber);
            invoicesToCreate.Add(commissionInvoice);
        }

        // Batch invoices into a single SaveChanges so they are created atomically.
        // If the process crashes between two separate CreateAsync calls the idempotency
        // guard on retry would skip the commission invoice — batching prevents that gap.
        await invoiceRepository.CreateBatchAsync(invoicesToCreate, ct);

        List<InvoiceJobMessage> messages = invoicesToCreate.Select(i => new InvoiceJobMessage(i.Id)).ToList();

        return ServiceResult<List<InvoiceJobMessage>>.Success(messages);
    }

    public async Task<ServiceResult<List<InvoiceJobMessage>>> CreateCreditNotesForRefundAsync(
        int refundId,
        CancellationToken ct)
    {
        Refund? refund = await paymentRepository.GetRefundByIdAsync(refundId, ct);
        if (refund is null)
            return ServiceResult<List<InvoiceJobMessage>>.Success(new List<InvoiceJobMessage>());

        Payment? payment = await paymentRepository.GetByIdAsync(refund.PaymentId, ct);
        if (payment is null)
            return ServiceResult<List<InvoiceJobMessage>>.Success(new List<InvoiceJobMessage>());

        List<Invoice> originals = await invoiceRepository.ListOriginalsByOrderIdAsync(payment.OrderId, ct);
        Invoice? customerOriginal = originals.FirstOrDefault(i => i.Kind == InvoiceKind.OrderInvoiceToCustomer);
        Invoice? commissionOriginal = originals.FirstOrDefault(i =>
            i.Kind == InvoiceKind.CommissionInvoiceToRestaurant);

        if (customerOriginal is null && commissionOriginal is null)
            return ServiceResult<List<InvoiceJobMessage>>.Success(new List<InvoiceJobMessage>());

        if (customerOriginal is not null && customerOriginal.Lines.Count == 0)
            customerOriginal =
                await invoiceRepository.GetByIdWithLinesAsync(customerOriginal.Id, ct) ?? customerOriginal;

        if (commissionOriginal is not null && commissionOriginal.Lines.Count == 0)
            commissionOriginal =
                await invoiceRepository.GetByIdWithLinesAsync(commissionOriginal.Id, ct) ?? commissionOriginal;

        List<InvoiceJobMessage> messages = new List<InvoiceJobMessage>();

        if (customerOriginal is not null)
        {
            decimal ratio = customerOriginal.TotalTtc != 0m
                ? refund.Amount / customerOriginal.TotalTtc
                : 0m;
            Invoice creditNote = await BuildCreditNoteAsync(
                customerOriginal, InvoiceKind.CreditNoteToCustomer, ratio, ct);
            await invoiceRepository.CreateAsync(creditNote, ct);
            messages.Add(new InvoiceJobMessage(creditNote.Id));
        }

        if (commissionOriginal is not null)
        {
            decimal commissionRatio;
            if (customerOriginal is not null && customerOriginal.TotalTtc != 0m)
            {
                commissionRatio = refund.Amount / customerOriginal.TotalTtc;
            }
            else if (payment.Amount != 0m)
            {
                commissionRatio = refund.Amount / payment.Amount;
            }
            else
            {
                commissionRatio = 0m;
            }

            Invoice creditNote = await BuildCreditNoteAsync(
                commissionOriginal, InvoiceKind.CommissionCreditNoteToRestaurant, commissionRatio, ct);
            await invoiceRepository.CreateAsync(creditNote, ct);
            messages.Add(new InvoiceJobMessage(creditNote.Id));
        }

        return ServiceResult<List<InvoiceJobMessage>>.Success(messages);
    }

    public async Task<ServiceResult<PaginatedResult<InvoiceListItemDto>>> ListForMeAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        (List<Invoice>? items, int total) = await invoiceRepository.ListForRecipientUserAsync(userId, page, pageSize, ct);
        return ServiceResult<PaginatedResult<InvoiceListItemDto>>.Success(new PaginatedResult<InvoiceListItemDto>
        {
            Items = items.Select(MapToListItemDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<ServiceResult<PaginatedResult<InvoiceListItemDto>>> ListForRestaurantAsync(
        int restaurantId,
        int userId,
        bool isAdmin,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        if (!isAdmin)
        {
            Restaurant? restaurant = await restaurantRepository.GetByIdAsync(restaurantId, ct);
            if (restaurant is null || restaurant.OwnerId != userId)
                return ServiceError.Forbidden(ErrorMessages.InvoiceAccessDenied);
        }

        (List<Invoice>? items, int total) = await invoiceRepository.ListForRecipientRestaurantAsync(restaurantId, page, pageSize, ct);
        return ServiceResult<PaginatedResult<InvoiceListItemDto>>.Success(new PaginatedResult<InvoiceListItemDto>
        {
            Items = items.Select(MapToListItemDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<ServiceResult<InvoicePdfStreamResult>> GetPdfStreamAsync(
        int invoiceId,
        int userId,
        bool isAdmin,
        bool isRestaurantOwner,
        CancellationToken ct)
    {
        Invoice? invoice = await invoiceRepository.GetByIdAsync(invoiceId, ct);
        if (invoice is null)
            return ServiceError.NotFound(ErrorMessages.InvoiceNotFound);

        if (invoice.Status != InvoiceStatus.Generated || string.IsNullOrEmpty(invoice.StoragePath))
            return ServiceError.Conflict(ErrorMessages.InvoiceNotGeneratedYet);

        if (!isAdmin)
        {
            bool authorized = false;

            if (invoice.RecipientUserId.HasValue && invoice.RecipientUserId == userId)
                authorized = true;

            if (!authorized && isRestaurantOwner && invoice.RecipientRestaurantId.HasValue)
            {
                Restaurant? restaurant = await restaurantRepository.GetByIdAsync(invoice.RecipientRestaurantId.Value, ct);
                if (restaurant is not null && restaurant.OwnerId == userId)
                    authorized = true;
            }

            if (!authorized)
                return ServiceError.Forbidden(ErrorMessages.InvoiceAccessDenied);
        }

        ObjectStorageResult? storageResult = await objectStorage.GetObjectAsync(invoice.StoragePath, ct);
        if (storageResult is null)
            return ServiceError.Conflict(ErrorMessages.InvoiceNotGeneratedYet);

        string fileName = $"{invoice.Number}.pdf";
        return ServiceResult<InvoicePdfStreamResult>.Success(
            new InvoicePdfStreamResult(storageResult.Content, fileName, storageResult.ContentType));
    }

    public async Task<ServiceResult<PaginatedResult<AdminInvoiceRowDto>>> AdminListAsync(
        InvoiceAdminQuery query,
        CancellationToken ct)
    {
        (List<Invoice>? items, int total) = await invoiceRepository.AdminListAsync(
            query.Year,
            query.Kind,
            query.IssuerType,
            query.RestaurantId,
            query.CustomerEmail,
            query.Page,
            query.PageSize,
            ct);

        return ServiceResult<PaginatedResult<AdminInvoiceRowDto>>.Success(
            new PaginatedResult<AdminInvoiceRowDto>
            {
                Items = items.Select(MapToAdminRowDto).ToList(),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize,
            });
    }

    public async Task<ServiceResult<AdminInvoiceDetailDto>> AdminGetDetailAsync(int id, CancellationToken ct)
    {
        Invoice? invoice = await invoiceRepository.GetByIdWithLinesAsync(id, ct);
        if (invoice is null)
            return ServiceError.NotFound(ErrorMessages.InvoiceNotFound);

        InvoiceLegalSnapshotDto issuer = JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(invoice.IssuerLegalSnapshotJson)
                     ?? new InvoiceLegalSnapshotDto(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        InvoiceLegalSnapshotDto recipient = JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(invoice.RecipientSnapshotJson)
                        ?? new InvoiceLegalSnapshotDto(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        List<InvoiceLineDto> lines = invoice.Lines.OrderBy(l => l.SortOrder).Select(l => new InvoiceLineDto(
            l.Description,
            l.Quantity,
            l.UnitPriceHt,
            l.UnitPriceTtc,
            l.VatRate,
            l.LineHt,
            l.LineVat,
            l.LineTtc,
            l.Kind)).ToList();

        InvoiceListItemDto header = MapToListItemDto(invoice);
        return ServiceResult<AdminInvoiceDetailDto>.Success(
            new AdminInvoiceDetailDto(header, lines, issuer, recipient, invoice.RelatedInvoiceId));
    }

    public async Task<ServiceResult> AdminResendEmailAsync(int id, CancellationToken ct)
    {
        Invoice? invoice = await invoiceRepository.GetByIdAsync(id, ct);
        if (invoice is null)
            return ServiceError.NotFound(ErrorMessages.InvoiceNotFound);

        if (invoice.Status != InvoiceStatus.Generated || string.IsNullOrEmpty(invoice.StoragePath))
            return ServiceError.Conflict(ErrorMessages.InvoiceNotGeneratedYet);

        InvoiceLegalSnapshotDto? recipient = JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(invoice.RecipientSnapshotJson);

        bool isCustomerKind = invoice.Kind == InvoiceKind.OrderInvoiceToCustomer
                             || invoice.Kind == InvoiceKind.CreditNoteToCustomer;

        string? recipientEmail;
        string? recipientName;
        EmailJobType jobType;

        if (isCustomerKind)
        {
            // Prefer the dedicated Email field; fall back to Address for pre-existing snapshots.
            recipientEmail = !string.IsNullOrWhiteSpace(recipient?.Email)
                ? recipient.Email
                : recipient?.Address;
            recipientName = recipient?.Name;
            jobType = EmailJobType.InvoiceReadyCustomer;
        }
        else
        {
            recipientEmail = !string.IsNullOrWhiteSpace(recipient?.Email)
                ? recipient.Email
                : recipient?.Address;
            recipientName = recipient?.Name;
            jobType = EmailJobType.InvoiceReadyRestaurant;
        }

        if (string.IsNullOrWhiteSpace(recipientEmail))
            return ServiceError.NotFound(ErrorMessages.InvoiceNotFound);

        string fileName = $"{invoice.Number}.pdf";
        bool isCreditNote = invoice.Kind == InvoiceKind.CreditNoteToCustomer
                            || invoice.Kind == InvoiceKind.CommissionCreditNoteToRestaurant;
        string subject = isCreditNote
            ? $"Votre avoir {invoice.Number} est disponible"
            : $"Votre facture {invoice.Number} est disponible";

        object templateData = jobType == EmailJobType.InvoiceReadyCustomer
            ? (object)new DeliverTableInfrastructure.TemplateData.InvoiceReadyCustomerData(
                invoice.Number,
                invoice.OrderId.ToString(),
                invoice.IssuedAt.ToString("dd/MM/yyyy"),
                invoice.TotalTtc.ToString("0.00"),
                invoice.Currency)
            : new DeliverTableInfrastructure.TemplateData.InvoiceReadyRestaurantData(
                invoice.Number,
                invoice.OrderId.ToString(),
                invoice.IssuedAt.ToString("dd/MM/yyyy"),
                invoice.TotalTtc.ToString("0.00"),
                invoice.Currency);

        EmailJob emailJob = new EmailJob
        {
            Type = jobType,
            Status = EmailJobStatus.Pending,
            RecipientEmail = recipientEmail,
            RecipientName = recipientName,
            Subject = subject,
            TemplateData = JsonSerializer.Serialize(templateData),
            MaxRetries = 3,
            AttachmentStoragePath = invoice.StoragePath,
            AttachmentFilename = fileName,
        };

        await emailJobRepository.CreateAsync(emailJob, ct);

        try
        {
            await messagePublisher.PublishAsync(MessagingExchanges.Email, new EmailJobMessage(emailJob.Id), ct);
        }
        catch
        {
            // best-effort: sweep will retry
        }

        return ServiceResult.Success();
    }

    private static InvoiceListItemDto MapToListItemDto(Invoice invoice) =>
        new(
            invoice.Id,
            invoice.Number,
            invoice.Kind,
            invoice.OrderId,
            invoice.IssuedAt,
            invoice.TotalTtc,
            invoice.Currency,
            invoice.Status);

    private static AdminInvoiceRowDto MapToAdminRowDto(Invoice invoice)
    {
        InvoiceLegalSnapshotDto? issuer = DeserializeSnapshot(invoice.IssuerLegalSnapshotJson);
        InvoiceLegalSnapshotDto? recipient = DeserializeSnapshot(invoice.RecipientSnapshotJson);
        return new AdminInvoiceRowDto(
            invoice.Id,
            invoice.Number,
            invoice.Kind,
            invoice.IssuerType,
            issuer?.Name ?? string.Empty,
            recipient?.Name ?? string.Empty,
            invoice.IssuedAt,
            invoice.TotalTtc,
            invoice.Status);
    }

    private static InvoiceLegalSnapshotDto? DeserializeSnapshot(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(json);
        }
        catch
        {
            return null;
        }
    }

    private async Task<Invoice> BuildCreditNoteAsync(
        Invoice original,
        InvoiceKind creditKind,
        decimal ratio,
        CancellationToken ct)
    {
        int year = DateTime.UtcNow.Year;
        string number = await numbering.IssueNumberAsync(
            original.IssuerType,
            original.IssuerType == InvoiceIssuerType.Restaurant ? original.IssuerRestaurantId : null,
            year,
            isCreditNote: true,
            ct);

        Invoice creditNote = new Invoice
        {
            Number = number,
            Kind = creditKind,
            OrderId = original.OrderId,
            IssuerType = original.IssuerType,
            IssuerRestaurantId = original.IssuerRestaurantId,
            RecipientUserId = original.RecipientUserId,
            RecipientRestaurantId = original.RecipientRestaurantId,
            RelatedInvoiceId = original.Id,
            IssuedAt = DateTime.UtcNow,
            Currency = original.Currency,
            Status = InvoiceStatus.Queued,
            IssuerLegalSnapshotJson = original.IssuerLegalSnapshotJson,
            RecipientSnapshotJson = original.RecipientSnapshotJson,
        };

        foreach (InvoiceLine? origLine in original.Lines.OrderBy(l => l.SortOrder))
        {
            decimal qty = Math.Round(origLine.Quantity * -ratio, 3, MidpointRounding.AwayFromZero);
            decimal lineHt = Math.Round(origLine.LineHt * -ratio, 2, MidpointRounding.AwayFromZero);
            decimal lineVat = Math.Round(origLine.LineVat * -ratio, 2, MidpointRounding.AwayFromZero);
            decimal lineTtc = Math.Round(origLine.LineTtc * -ratio, 2, MidpointRounding.AwayFromZero);

            creditNote.Lines.Add(new InvoiceLine
            {
                Kind = origLine.Kind,
                Description = origLine.Description,
                Quantity = qty,
                UnitPriceTtc = origLine.UnitPriceTtc,
                UnitPriceHt = origLine.UnitPriceHt,
                VatRate = origLine.VatRate,
                LineHt = lineHt,
                LineVat = lineVat,
                LineTtc = lineTtc,
                SortOrder = origLine.SortOrder,
            });
        }

        creditNote.TotalHt = creditNote.Lines.Sum(l => l.LineHt);
        creditNote.TotalVat = creditNote.Lines.Sum(l => l.LineVat);
        creditNote.TotalTtc = creditNote.Lines.Sum(l => l.LineTtc);
        return creditNote;
    }

    private Invoice BuildCustomerInvoice(Order order, Restaurant restaurant, User customer, string number)
    {
        InvoiceLegalSnapshotDto issuerSnapshot = new InvoiceLegalSnapshotDto(
            Name: restaurant.LegalName,
            LegalForm: restaurant.LegalForm,
            Siret: restaurant.Siret,
            VatNumber: string.Empty,
            Address: restaurant.LegalAddress);

        InvoiceLegalSnapshotDto recipientSnapshot = new InvoiceLegalSnapshotDto(
            Name: customer.GetFullName(),
            LegalForm: string.Empty,
            Siret: string.Empty,
            VatNumber: string.Empty,
            Address: BillingAddressHelper.FormatBillingAddressForSnapshot(customer),
            Email: customer.Email ?? string.Empty);

        Invoice invoice = new Invoice
        {
            Number = number,
            Kind = InvoiceKind.OrderInvoiceToCustomer,
            OrderId = order.Id,
            IssuerType = InvoiceIssuerType.Restaurant,
            IssuerRestaurantId = restaurant.Id,
            RecipientUserId = customer.Id,
            IssuedAt = DateTime.UtcNow,
            Currency = "EUR",
            Status = InvoiceStatus.Queued,
            IssuerLegalSnapshotJson = JsonSerializer.Serialize(issuerSnapshot),
            RecipientSnapshotJson = JsonSerializer.Serialize(recipientSnapshot),
        };

        int sort = 0;
        foreach (OrderItem item in order.Items)
        {
            decimal rate = restaurant.IsVatRegistered ? item.Dish.VatRate.ToDecimal() : 0m;
            decimal lineTtc = Math.Round(item.UnitPrice * item.Quantity, 2, MidpointRounding.AwayFromZero);
            decimal unitHt = Math.Round(item.UnitPrice / (1 + rate / 100m), 2, MidpointRounding.AwayFromZero);
            decimal lineHt = Math.Round(unitHt * item.Quantity, 2, MidpointRounding.AwayFromZero);
            decimal lineVat = Math.Round(lineTtc - lineHt, 2, MidpointRounding.AwayFromZero);

            invoice.Lines.Add(new InvoiceLine
            {
                Kind = InvoiceLineKind.Item,
                Description = item.DishName,
                Quantity = item.Quantity,
                UnitPriceTtc = item.UnitPrice,
                UnitPriceHt = unitHt,
                VatRate = rate,
                LineHt = lineHt,
                LineVat = lineVat,
                LineTtc = lineTtc,
                SortOrder = sort++,
            });
        }

        foreach (InvoiceLine discountLine in BuildDiscountLines(invoice.Lines.ToList(), order.Discounts, sort))
        {
            invoice.Lines.Add(discountLine);
            sort++;
        }

        invoice.TotalTtc = invoice.Lines.Sum(l => l.LineTtc);
        invoice.TotalHt = invoice.Lines.Sum(l => l.LineHt);
        invoice.TotalVat = invoice.Lines.Sum(l => l.LineVat);

        return invoice;
    }

    private static List<InvoiceLine> BuildDiscountLines(
        IReadOnlyList<InvoiceLine> itemLines,
        IReadOnlyList<OrderDiscount> discounts,
        int startSortOrder)
    {
        List<InvoiceLine> result = new List<InvoiceLine>();
        if (discounts.Count == 0) return result;

        Dictionary<decimal, decimal> subtotalByRate = itemLines
            .GroupBy(l => l.VatRate)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.LineTtc));
        decimal subtotalTtc = subtotalByRate.Values.Sum();
        if (subtotalTtc <= 0m) return result;

        int sort = startSortOrder;
        foreach (OrderDiscount d in discounts)
        {
            List<(decimal Rate, decimal Slice)> slices = new List<(decimal Rate, decimal Slice)>();
            foreach ((decimal rate, decimal rateSubtotal) in subtotalByRate)
            {
                if (rateSubtotal <= 0m) continue;
                decimal share = rateSubtotal / subtotalTtc;
                decimal slice = Math.Round(d.Amount * share, 2, MidpointRounding.AwayFromZero);
                if (slice > 0m)
                    slices.Add((rate, slice));
            }
            if (slices.Count == 0) continue;

            decimal drift = d.Amount - slices.Sum(s => s.Slice);
            if (drift != 0m)
            {
                int idx = slices
                    .Select((s, i) => (s, i))
                    .OrderByDescending(t => Math.Abs(t.s.Slice))
                    .ThenByDescending(t => t.s.Rate)
                    .First().i;
                (decimal r, decimal sl) = slices[idx];
                slices[idx] = (r, sl + drift);
            }

            bool multiRate = slices.Count > 1;
            foreach ((decimal rate, decimal slice) in slices)
            {
                decimal lineTtc = -slice;
                decimal lineHt = Math.Round(lineTtc / (1 + rate / 100m), 2, MidpointRounding.AwayFromZero);
                decimal lineVat = lineTtc - lineHt;
                string description = multiRate ? $"{d.Description} (TVA {rate:0.#}%)" : d.Description;
                result.Add(new InvoiceLine
                {
                    Kind = InvoiceLineKind.Discount,
                    Description = description,
                    Quantity = 1m,
                    UnitPriceTtc = lineTtc,
                    UnitPriceHt = lineHt,
                    VatRate = rate,
                    LineHt = lineHt,
                    LineVat = lineVat,
                    LineTtc = lineTtc,
                    SortOrder = sort++,
                });
            }
        }

        return result;
    }

    private Invoice BuildCommissionInvoice(Order order, Restaurant restaurant, string number)
    {
        InvoiceLegalSnapshotDto issuerSnapshot = new InvoiceLegalSnapshotDto(
            Name: env.PlatformLegalName,
            LegalForm: env.PlatformLegalForm,
            Siret: env.PlatformSiret,
            VatNumber: env.PlatformVatNumber,
            Address: env.PlatformAddress);

        InvoiceLegalSnapshotDto recipientSnapshot = new InvoiceLegalSnapshotDto(
            Name: restaurant.LegalName,
            LegalForm: restaurant.LegalForm,
            Siret: restaurant.Siret,
            VatNumber: string.Empty,
            Address: restaurant.LegalAddress);

        decimal commissionAmount = Math.Round(
            order.TotalAmount * env.PlatformCommissionRate, 2, MidpointRounding.AwayFromZero);
        decimal rate = env.PlatformVatApplicable ? 20m : 0m;

        decimal unitHt = commissionAmount;
        decimal unitTtc = Math.Round(unitHt * (1 + rate / 100m), 2, MidpointRounding.AwayFromZero);
        decimal lineVat = Math.Round(unitTtc - unitHt, 2, MidpointRounding.AwayFromZero);

        Invoice invoice = new Invoice
        {
            Number = number,
            Kind = InvoiceKind.CommissionInvoiceToRestaurant,
            OrderId = order.Id,
            IssuerType = InvoiceIssuerType.Platform,
            RecipientRestaurantId = restaurant.Id,
            IssuedAt = DateTime.UtcNow,
            Currency = "EUR",
            Status = InvoiceStatus.Queued,
            IssuerLegalSnapshotJson = JsonSerializer.Serialize(issuerSnapshot),
            RecipientSnapshotJson = JsonSerializer.Serialize(recipientSnapshot),
        };

        invoice.Lines.Add(new InvoiceLine
        {
            Description = $"Commission plateforme sur commande #{order.Id}",
            Quantity = 1m,
            UnitPriceHt = unitHt,
            UnitPriceTtc = unitTtc,
            VatRate = rate,
            LineHt = unitHt,
            LineVat = lineVat,
            LineTtc = unitTtc,
            SortOrder = 0,
        });

        invoice.TotalTtc = unitTtc;
        invoice.TotalHt = unitHt;
        invoice.TotalVat = lineVat;

        return invoice;
    }
}
