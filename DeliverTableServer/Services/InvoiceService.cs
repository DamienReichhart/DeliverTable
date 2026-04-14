using System.Text.Json;
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
    AppEnvironment env) : IInvoiceService
{
    public async Task<ServiceResult<List<InvoiceJobMessage>>> CreatePendingInvoicesForCapturedOrderAsync(
        int orderId,
        CancellationToken ct)
    {
        var order = await orderRepository.GetByIdWithFullDetailsAsync(orderId, ct);
        if (order is null)
            return new ServiceError(ErrorMessages.OrderNotFound);

        if (await invoiceRepository.ExistsForOrderAndKindAsync(orderId, InvoiceKind.OrderInvoiceToCustomer, ct))
            return ServiceResult<List<InvoiceJobMessage>>.Success(new List<InvoiceJobMessage>());

        int year = DateTime.UtcNow.Year;
        var restaurant = order.Restaurant;
        var customer = order.Customer;

        var customerNumber = await numbering.IssueNumberAsync(
            InvoiceIssuerType.Restaurant, restaurant.Id, year, false, ct);
        var customerInvoice = BuildCustomerInvoice(order, restaurant, customer, customerNumber);
        await invoiceRepository.CreateAsync(customerInvoice, ct);

        var platformNumber = await numbering.IssueNumberAsync(
            InvoiceIssuerType.Platform, null, year, false, ct);
        var commissionInvoice = BuildCommissionInvoice(order, restaurant, platformNumber);
        await invoiceRepository.CreateAsync(commissionInvoice, ct);

        var messages = new List<InvoiceJobMessage>
        {
            new(customerInvoice.Id),
            new(commissionInvoice.Id),
        };

        return ServiceResult<List<InvoiceJobMessage>>.Success(messages);
    }

    public async Task<ServiceResult<List<InvoiceJobMessage>>> CreateCreditNotesForRefundAsync(
        int refundId,
        CancellationToken ct)
    {
        var refund = await paymentRepository.GetRefundByIdAsync(refundId, ct);
        if (refund is null)
            return ServiceResult<List<InvoiceJobMessage>>.Success(new List<InvoiceJobMessage>());

        var payment = await paymentRepository.GetByIdAsync(refund.PaymentId, ct);
        if (payment is null)
            return ServiceResult<List<InvoiceJobMessage>>.Success(new List<InvoiceJobMessage>());

        var originals = await invoiceRepository.ListByOrderIdAsync(payment.OrderId, ct);
        var customerOriginal = originals.FirstOrDefault(i => i.Kind == InvoiceKind.OrderInvoiceToCustomer);
        var commissionOriginal = originals.FirstOrDefault(i =>
            i.Kind == InvoiceKind.CommissionInvoiceToRestaurant);

        if (customerOriginal is null && commissionOriginal is null)
            return ServiceResult<List<InvoiceJobMessage>>.Success(new List<InvoiceJobMessage>());

        if (customerOriginal is not null && customerOriginal.Lines.Count == 0)
            customerOriginal =
                await invoiceRepository.GetByIdWithLinesAsync(customerOriginal.Id, ct) ?? customerOriginal;

        if (commissionOriginal is not null && commissionOriginal.Lines.Count == 0)
            commissionOriginal =
                await invoiceRepository.GetByIdWithLinesAsync(commissionOriginal.Id, ct) ?? commissionOriginal;

        var messages = new List<InvoiceJobMessage>();

        if (customerOriginal is not null)
        {
            decimal ratio = customerOriginal.TotalTtc != 0m
                ? refund.Amount / customerOriginal.TotalTtc
                : 0m;
            var creditNote = await BuildCreditNoteAsync(
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

            var creditNote = await BuildCreditNoteAsync(
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
        var (items, total) = await invoiceRepository.ListForRecipientUserAsync(userId, page, pageSize, ct);
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
            var restaurant = await restaurantRepository.GetByIdAsync(restaurantId, ct);
            if (restaurant is null || restaurant.OwnerId != userId)
                return new ServiceError(ErrorMessages.InvoiceAccessDenied, 403);
        }

        var (items, total) = await invoiceRepository.ListForRecipientRestaurantAsync(restaurantId, page, pageSize, ct);
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
        var invoice = await invoiceRepository.GetByIdAsync(invoiceId, ct);
        if (invoice is null)
            return new ServiceError(ErrorMessages.InvoiceNotFound, 404);

        if (invoice.Status != InvoiceStatus.Generated || string.IsNullOrEmpty(invoice.StoragePath))
            return new ServiceError(ErrorMessages.InvoiceNotGeneratedYet, 409);

        if (!isAdmin)
        {
            bool authorized = false;

            if (invoice.RecipientUserId.HasValue && invoice.RecipientUserId == userId)
                authorized = true;

            if (!authorized && isRestaurantOwner && invoice.RecipientRestaurantId.HasValue)
            {
                var restaurant = await restaurantRepository.GetByIdAsync(invoice.RecipientRestaurantId.Value, ct);
                if (restaurant is not null && restaurant.OwnerId == userId)
                    authorized = true;
            }

            if (!authorized)
                return new ServiceError(ErrorMessages.InvoiceAccessDenied, 403);
        }

        var storageResult = await objectStorage.GetObjectAsync(invoice.StoragePath, ct);
        if (storageResult is null)
            return new ServiceError(ErrorMessages.InvoiceNotGeneratedYet, 409);

        var fileName = $"{invoice.Number}.pdf";
        return ServiceResult<InvoicePdfStreamResult>.Success(
            new InvoicePdfStreamResult(storageResult.Content, fileName, storageResult.ContentType));
    }

    public async Task<ServiceResult<PaginatedResult<AdminInvoiceRowDto>>> AdminListAsync(
        InvoiceAdminQuery query,
        CancellationToken ct)
    {
        var (items, total) = await invoiceRepository.AdminListAsync(
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
        var invoice = await invoiceRepository.GetByIdWithLinesAsync(id, ct);
        if (invoice is null)
            return new ServiceError(ErrorMessages.InvoiceNotFound, 404);

        var issuer = JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(invoice.IssuerLegalSnapshotJson)
                     ?? new InvoiceLegalSnapshotDto(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        var recipient = JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(invoice.RecipientSnapshotJson)
                        ?? new InvoiceLegalSnapshotDto(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        var lines = invoice.Lines.OrderBy(l => l.SortOrder).Select(l => new InvoiceLineDto(
            l.Description,
            l.Quantity,
            l.UnitPriceHt,
            l.UnitPriceTtc,
            l.VatRate,
            l.LineHt,
            l.LineVat,
            l.LineTtc)).ToList();

        var header = MapToListItemDto(invoice);
        return ServiceResult<AdminInvoiceDetailDto>.Success(
            new AdminInvoiceDetailDto(header, lines, issuer, recipient, invoice.RelatedInvoiceId));
    }

    public async Task<ServiceResult> AdminResendEmailAsync(int id, CancellationToken ct)
    {
        var invoice = await invoiceRepository.GetByIdAsync(id, ct);
        if (invoice is null)
            return new ServiceError(ErrorMessages.InvoiceNotFound, 404);

        if (invoice.Status != InvoiceStatus.Generated || string.IsNullOrEmpty(invoice.StoragePath))
            return new ServiceError(ErrorMessages.InvoiceNotGeneratedYet, 409);

        var recipient = JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(invoice.RecipientSnapshotJson);

        var isCustomerKind = invoice.Kind == InvoiceKind.OrderInvoiceToCustomer
                             || invoice.Kind == InvoiceKind.CreditNoteToCustomer;

        string? recipientEmail;
        string? recipientName;
        EmailJobType jobType;

        if (isCustomerKind)
        {
            recipientEmail = recipient?.Address;
            recipientName = recipient?.Name;
            jobType = EmailJobType.InvoiceReadyCustomer;
        }
        else
        {
            recipientEmail = recipient?.Address;
            recipientName = recipient?.Name;
            jobType = EmailJobType.InvoiceReadyRestaurant;
        }

        if (string.IsNullOrWhiteSpace(recipientEmail))
            return new ServiceError(ErrorMessages.InvoiceNotFound, 404);

        var fileName = $"{invoice.Number}.pdf";
        bool isCreditNote = invoice.Kind == InvoiceKind.CreditNoteToCustomer
                            || invoice.Kind == InvoiceKind.CommissionCreditNoteToRestaurant;
        var subject = isCreditNote
            ? $"Votre avoir {invoice.Number} est disponible"
            : $"Votre facture {invoice.Number} est disponible";

        var templateData = jobType == EmailJobType.InvoiceReadyCustomer
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

        var emailJob = new EmailJob
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
            await messagePublisher.PublishAsync("email", new EmailJobMessage(emailJob.Id), ct);
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
        var issuer = DeserializeSnapshot(invoice.IssuerLegalSnapshotJson);
        var recipient = DeserializeSnapshot(invoice.RecipientSnapshotJson);
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
        var number = await numbering.IssueNumberAsync(
            original.IssuerType,
            original.IssuerType == InvoiceIssuerType.Restaurant ? original.IssuerRestaurantId : null,
            year,
            isCreditNote: true,
            ct);

        var creditNote = new Invoice
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

        foreach (var origLine in original.Lines.OrderBy(l => l.SortOrder))
        {
            var qty = Math.Round(origLine.Quantity * -ratio, 3, MidpointRounding.AwayFromZero);
            var lineHt = Math.Round(origLine.LineHt * -ratio, 2, MidpointRounding.AwayFromZero);
            var lineVat = Math.Round(origLine.LineVat * -ratio, 2, MidpointRounding.AwayFromZero);
            var lineTtc = Math.Round(origLine.LineTtc * -ratio, 2, MidpointRounding.AwayFromZero);

            creditNote.Lines.Add(new InvoiceLine
            {
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
        var issuerSnapshot = new InvoiceLegalSnapshotDto(
            Name: restaurant.LegalName,
            LegalForm: restaurant.LegalForm,
            Siret: restaurant.Siret,
            VatNumber: string.Empty,
            Address: restaurant.LegalAddress);

        var recipientSnapshot = new InvoiceLegalSnapshotDto(
            Name: $"{customer.FirstName} {customer.LastName}".Trim(),
            LegalForm: string.Empty,
            Siret: string.Empty,
            VatNumber: string.Empty,
            Address: customer.Email ?? string.Empty);

        var invoice = new Invoice
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
        foreach (var item in order.Items)
        {
            var rate = restaurant.IsVatRegistered ? item.Dish.VatRate.ToDecimal() : 0m;
            var lineTtc = Math.Round(item.UnitPrice * item.Quantity, 2, MidpointRounding.AwayFromZero);
            var unitHt = Math.Round(item.UnitPrice / (1 + rate / 100m), 2, MidpointRounding.AwayFromZero);
            var lineHt = Math.Round(unitHt * item.Quantity, 2, MidpointRounding.AwayFromZero);
            var lineVat = Math.Round(lineTtc - lineHt, 2, MidpointRounding.AwayFromZero);

            invoice.Lines.Add(new InvoiceLine
            {
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

        invoice.TotalTtc = invoice.Lines.Sum(l => l.LineTtc);
        invoice.TotalHt = invoice.Lines.Sum(l => l.LineHt);
        invoice.TotalVat = invoice.Lines.Sum(l => l.LineVat);

        return invoice;
    }

    private Invoice BuildCommissionInvoice(Order order, Restaurant restaurant, string number)
    {
        var issuerSnapshot = new InvoiceLegalSnapshotDto(
            Name: env.PlatformLegalName,
            LegalForm: env.PlatformLegalForm,
            Siret: env.PlatformSiret,
            VatNumber: env.PlatformVatNumber,
            Address: env.PlatformAddress);

        var recipientSnapshot = new InvoiceLegalSnapshotDto(
            Name: restaurant.LegalName,
            LegalForm: restaurant.LegalForm,
            Siret: restaurant.Siret,
            VatNumber: string.Empty,
            Address: restaurant.LegalAddress);

        var commissionAmount = Math.Round(
            order.TotalAmount * env.PlatformCommissionRate, 2, MidpointRounding.AwayFromZero);
        decimal rate = env.PlatformVatApplicable ? 20m : 0m;

        var unitHt = commissionAmount;
        var unitTtc = Math.Round(unitHt * (1 + rate / 100m), 2, MidpointRounding.AwayFromZero);
        var lineVat = Math.Round(unitTtc - unitHt, 2, MidpointRounding.AwayFromZero);

        var invoice = new Invoice
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
