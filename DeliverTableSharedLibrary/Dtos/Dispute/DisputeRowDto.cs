using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Dispute;

public record DisputeRowDto(
    int Id,
    string StripeDisputeId,
    int OrderId,
    decimal Amount,
    string Currency,
    string ReasonCode,
    DisputeState State,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    DateTime? DueBy);
