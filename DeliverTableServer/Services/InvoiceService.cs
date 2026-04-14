using System.Text.Json;
using DeliverTableInfrastructure.Invoicing;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;
using DeliverTableSharedLibrary.Extensions;

namespace DeliverTableServer.Services;

public class InvoiceService(
    IInvoiceRepository invoiceRepository,
    IOrderRepository orderRepository,
    IInvoiceNumberingService numbering,
    IPaymentRepository paymentRepository,
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
            decimal ratio = customerOriginal is not null && customerOriginal.TotalTtc != 0m
                ? refund.Amount / customerOriginal.TotalTtc
                : 0m;
            var creditNote = await BuildCreditNoteAsync(
                commissionOriginal, InvoiceKind.CommissionCreditNoteToRestaurant, ratio, ct);
            await invoiceRepository.CreateAsync(creditNote, ct);
            messages.Add(new InvoiceJobMessage(creditNote.Id));
        }

        return ServiceResult<List<InvoiceJobMessage>>.Success(messages);
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
