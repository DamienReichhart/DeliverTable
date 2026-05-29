using System.Text.Json;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;
using Microsoft.Extensions.Logging;

namespace DeliverTableServer.Services;

public class CommissionStatementService(
    ICommissionStatementRepository repo,
    IRestaurantRepository restaurantRepo,
    IOrderRepository orderRepo,
    IMessagePublisher publisher,
    AppEnvironment env,
    ILogger<CommissionStatementService> logger) : ICommissionStatementService
{
    private static readonly TimeZoneInfo ParisTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    public async Task<ServiceResult<CommissionStatementGenerationResultDto>> GenerateForPeriodAsync(
        int year, int month, CancellationToken ct)
    {
        if (month is < 1 or > 12 || year is < 2000 or > 9999)
            return ServiceError.BadRequest(ErrorMessages.CommissionStatementInvalidPeriod);

        var (startUtc, endUtc) = ComputePeriodBoundsUtc(year, month);
        var result = new CommissionStatementGenerationResultDto
        {
            PeriodYear = year,
            PeriodMonth = month,
        };

        var restaurantIds = await repo.ListRestaurantIdsWithEligibleOrdersAsync(startUtc, endUtc, ct);
        result.RestaurantsProcessed = restaurantIds.Count;

        foreach (var restaurantId in restaurantIds)
        {
            try
            {
                if (await repo.InvoiceExistsForPeriodAsync(restaurantId, year, month, ct))
                {
                    result.RestaurantsSkipped++;
                    continue;
                }

                var restaurant = await restaurantRepo.GetByIdWithOwnerAsync(restaurantId, ct);
                if (restaurant is null) continue;

                var orders = await repo.ListEligibleOrdersForRestaurantAsync(restaurantId, startUtc, endUtc, ct);
                if (orders.Count == 0)
                {
                    result.RestaurantsSkipped++;
                    continue;
                }

                var statement = BuildStatement(restaurant, year, month, orders);
                statement.Number = await FormatInvoiceNumberAsync(year, month, ct);

                foreach (var o in orders) o.CommissionStatementId = statement.Id;
                await repo.CreateAsync(statement, ct);

                await publisher.PublishAsync(
                    MessagingExchanges.CommissionStatement,
                    new CommissionStatementJobMessage(statement.Id),
                    ct);

                result.StatementsCreated++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate commission statement for restaurant {RestaurantId}", restaurantId);
                result.Failures.Add(new GenerationFailureDto
                {
                    RestaurantId = restaurantId,
                    Reason = ex.Message,
                });
            }
        }

        return result;
    }

    public async Task<ServiceResult> HandleRefundForPriorPeriodAsync(
        int orderId, int refundId, string stripeRefundId, decimal refundedAmount, CancellationToken ct)
    {
        var order = await orderRepo.GetByIdAsync(orderId, ct);
        if (order is null) return ServiceResult.Success();
        if (order.CommissionStatementId is null) return ServiceResult.Success();

        var existingLine = await repo.FindLineByRefundEventIdAsync(stripeRefundId, ct);
        if (existingLine is not null) return ServiceResult.Success();

        var original = await repo.GetByIdWithLinesAndRecipientAsync(order.CommissionStatementId.Value, ct);
        if (original is null) return ServiceError.NotFound(ErrorMessages.CommissionStatementNotFound);

        var originalLine = original.Lines.FirstOrDefault(l => l.OrderId == orderId);
        if (originalLine is null) return ServiceResult.Success();

        var restaurant = await restaurantRepo.GetByIdWithOwnerAsync(order.RestaurantId, ct);
        if (restaurant is null) return ServiceError.NotFound(ErrorMessages.CommissionStatementNotFound);

        var rate = originalLine.CommissionRateSnapshot;
        var vat = originalLine.VatRate;
        var ht = Math.Round(refundedAmount * rate, 2, MidpointRounding.AwayFromZero);
        var ttc = Math.Round(ht * (1 + vat / 100m), 2, MidpointRounding.AwayFromZero);
        var vatAmount = Math.Round(ttc - ht, 2, MidpointRounding.AwayFromZero);

        var creditNote = new CommissionStatement
        {
            Kind = CommissionStatementKind.CreditNote,
            RecipientRestaurantId = restaurant.Id,
            PeriodYear = original.PeriodYear,
            PeriodMonth = original.PeriodMonth,
            IssuedAt = DateTime.UtcNow,
            Currency = "EUR",
            Status = CommissionStatementStatus.Queued,
            RelatedStatementId = original.Id,
            IssuerLegalSnapshotJson = original.IssuerLegalSnapshotJson,
            RecipientSnapshotJson = original.RecipientSnapshotJson,
            RecipientEmailSnapshot = restaurant.Owner?.Email,
            TotalHt = -ht,
            TotalVat = -vatAmount,
            TotalTtc = -ttc,
        };
        creditNote.Lines.Add(new CommissionStatementLine
        {
            OrderId = order.Id,
            OrderNumber = order.Id.ToString(),
            OrderCompletedAt = order.DeliveredAt ?? order.UpdatedAt,
            OrderTotalAmount = refundedAmount,
            CommissionRateSnapshot = rate,
            VatRate = vat,
            LineHt = -ht,
            LineVat = -vatAmount,
            LineTtc = -ttc,
            RefundEventId = stripeRefundId,
            SortOrder = 0,
        });

        var seq = await repo.AllocateNextNumberAsync(ct);
        creditNote.Number = $"AVOIR-COMM-{original.PeriodYear:D4}-{original.PeriodMonth:D2}-{seq:D6}";

        order.CommissionRefundStatementId = creditNote.Id;
        await repo.CreateAsync(creditNote, ct);
        await orderRepo.UpdateAsync(order, ct);

        await publisher.PublishAsync(MessagingExchanges.CommissionStatement,
            new CommissionStatementJobMessage(creditNote.Id), ct);

        return ServiceResult.Success();
    }

    internal static (DateTime startUtc, DateTime endUtc) ComputePeriodBoundsUtc(int year, int month)
    {
        var startLocal = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var endLocal = startLocal.AddMonths(1);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, ParisTz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, ParisTz);
        return (startUtc, endUtc);
    }

    private CommissionStatement BuildStatement(Restaurant restaurant, int year, int month, List<Order> orders)
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

        var statement = new CommissionStatement
        {
            Kind = CommissionStatementKind.Invoice,
            RecipientRestaurantId = restaurant.Id,
            PeriodYear = year,
            PeriodMonth = month,
            IssuedAt = DateTime.UtcNow,
            Currency = "EUR",
            Status = CommissionStatementStatus.Queued,
            IssuerLegalSnapshotJson = JsonSerializer.Serialize(issuerSnapshot),
            RecipientSnapshotJson = JsonSerializer.Serialize(recipientSnapshot),
            RecipientEmailSnapshot = restaurant.Owner?.Email,
        };

        var rateVat = env.PlatformVatApplicable ? 20m : 0m;
        int sort = 0;
        foreach (var order in orders)
        {
            var net = order.TotalAmount - TotalRefundedFor(order);
            if (net <= 0) continue;
            var commissionHt = Math.Round(net * env.PlatformCommissionRate, 2, MidpointRounding.AwayFromZero);
            var commissionTtc = Math.Round(commissionHt * (1 + rateVat / 100m), 2, MidpointRounding.AwayFromZero);
            var commissionVat = Math.Round(commissionTtc - commissionHt, 2, MidpointRounding.AwayFromZero);

            statement.Lines.Add(new CommissionStatementLine
            {
                OrderId = order.Id,
                OrderNumber = order.Id.ToString(),
                OrderCompletedAt = order.DeliveredAt ?? order.UpdatedAt,
                OrderTotalAmount = net,
                CommissionRateSnapshot = env.PlatformCommissionRate,
                VatRate = rateVat,
                LineHt = commissionHt,
                LineVat = commissionVat,
                LineTtc = commissionTtc,
                SortOrder = sort++,
            });
        }

        statement.TotalHt = statement.Lines.Sum(l => l.LineHt);
        statement.TotalVat = statement.Lines.Sum(l => l.LineVat);
        statement.TotalTtc = statement.Lines.Sum(l => l.LineTtc);
        return statement;
    }

    private static decimal TotalRefundedFor(Order order)
        => order.Payments
            .SelectMany(p => p.Refunds ?? Enumerable.Empty<Refund>())
            .Sum(r => r.Amount);

    private async Task<string> FormatInvoiceNumberAsync(int year, int month, CancellationToken ct)
    {
        var seq = await repo.AllocateNextNumberAsync(ct);
        return $"COMM-{year:D4}-{month:D2}-{seq:D6}";
    }
}
