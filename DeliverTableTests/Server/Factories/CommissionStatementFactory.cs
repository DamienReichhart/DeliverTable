using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableTests.Server.Factories;

public static class CommissionStatementFactory
{
    public static CommissionStatement CreateInvoice(
        int restaurantId,
        int year,
        int month,
        string number = "COMM-2026-05-000001") => new()
        {
            Number = number,
            Kind = CommissionStatementKind.Invoice,
            RecipientRestaurantId = restaurantId,
            PeriodYear = year,
            PeriodMonth = month,
            IssuedAt = DateTime.UtcNow,
            Currency = "EUR",
            Status = CommissionStatementStatus.Queued,
            IssuerLegalSnapshotJson = "{}",
            RecipientSnapshotJson = "{}",
        };
}
