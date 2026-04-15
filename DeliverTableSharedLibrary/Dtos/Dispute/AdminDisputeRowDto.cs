using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Dispute;

public record AdminDisputeRowDto(
    int Id,
    string StripeDisputeId,
    int OrderId,
    int RestaurantId,
    string RestaurantName,
    string CustomerEmail,
    decimal Amount,
    string Currency,
    string ReasonCode,
    DisputeState State,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    DateTime? DueBy);
