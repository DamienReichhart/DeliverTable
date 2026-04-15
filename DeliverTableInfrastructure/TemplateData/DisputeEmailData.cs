namespace DeliverTableInfrastructure.TemplateData;

public sealed record DisputeEmailData(
    int DisputeId,
    string StripeDisputeId,
    int OrderId,
    int RestaurantId,
    string RestaurantName,
    string Amount,
    string Currency,
    string Reason,
    string? DueBy,
    string StripeDashboardUrl,
    string AdminDetailUrl);
