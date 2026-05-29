using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableServer.Mappers;

public static class CommissionStatementMapper
{
    public static CommissionStatementDto ToDto(this CommissionStatement entity) => new()
    {
        Id = entity.Id,
        Number = entity.Number,
        Kind = entity.Kind,
        RecipientRestaurantId = entity.RecipientRestaurantId,
        PeriodYear = entity.PeriodYear,
        PeriodMonth = entity.PeriodMonth,
        IssuedAt = entity.IssuedAt,
        TotalHt = entity.TotalHt,
        TotalVat = entity.TotalVat,
        TotalTtc = entity.TotalTtc,
        Currency = entity.Currency,
        Status = entity.Status,
        RelatedStatementId = entity.RelatedStatementId,
        Lines = entity.Lines.Select(ToDto).ToList(),
    };

    public static CommissionStatementLineDto ToDto(this CommissionStatementLine entity) => new()
    {
        Id = entity.Id,
        OrderId = entity.OrderId,
        OrderNumber = entity.OrderNumber,
        OrderCompletedAt = entity.OrderCompletedAt,
        OrderTotalAmount = entity.OrderTotalAmount,
        CommissionRateSnapshot = entity.CommissionRateSnapshot,
        VatRate = entity.VatRate,
        LineHt = entity.LineHt,
        LineVat = entity.LineVat,
        LineTtc = entity.LineTtc,
        RefundEventId = entity.RefundEventId,
    };
}
