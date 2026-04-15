using DeliverTableSharedLibrary.Dtos.RestaurantAccount;

namespace DeliverTableSharedLibrary.Dtos.Dispute;

public record AdminDisputeDetailDto(
    AdminDisputeRowDto Header,
    string StripeDashboardUrl,
    int PaymentId,
    string StripeChargeId,
    decimal PaymentAmount,
    List<RestaurantTransactionDto> LinkedTransactions);
