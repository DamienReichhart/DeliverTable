namespace DeliverTableSharedLibrary.Dtos.Payment;

public record RefundDto(int Id, decimal Amount, string Currency, string Reason, DateTime CreatedAt);
