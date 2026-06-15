using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class PaymentRepository(DeliverTableContext dbContext) : IPaymentRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<Payment> CreateAsync(Payment payment, CancellationToken ct = default)
    {
        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync(ct);
        return payment;
    }

    public Task<Payment?> GetByIdAsync(int id, CancellationToken ct = default) =>
        PaymentsWithRefunds.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken ct = default) =>
        PaymentsWithRefunds.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

    public Task<Payment?> GetByStripePaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default) =>
        PaymentsWithRefunds.FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, ct);

    public Task<Payment?> GetByStripeChargeIdAsync(string stripeChargeId, CancellationToken ct = default) =>
        PaymentsWithRefunds.FirstOrDefaultAsync(p => p.StripeChargeId == stripeChargeId, ct);

    private IQueryable<Payment> PaymentsWithRefunds => _dbContext.Payments.Include(p => p.Refunds);

    public async Task UpdateAsync(Payment payment, CancellationToken ct = default)
    {
        payment.UpdatedAt = DateTime.UtcNow;
        _dbContext.Payments.Update(payment);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<Refund> AddRefundAsync(Refund refund, CancellationToken ct = default)
    {
        _dbContext.Refunds.Add(refund);
        await _dbContext.SaveChangesAsync(ct);
        return refund;
    }

    public Task<Refund?> GetRefundByIdAsync(int id, CancellationToken ct = default) =>
        _dbContext.Refunds.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Refund?> GetRefundByStripeIdAsync(string stripeRefundId, CancellationToken ct = default) =>
        _dbContext.Refunds.FirstOrDefaultAsync(r => r.StripeRefundId == stripeRefundId, ct);

    public async Task<decimal> GetTotalRefundedAsync(int paymentId, CancellationToken ct = default)
    {
        return await _dbContext.Refunds
            .Where(r => r.PaymentId == paymentId)
            .SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;
    }

    public async Task<bool> TryRegisterProcessedEventAsync(string stripeEventId, string eventType, CancellationToken ct = default)
    {
        bool exists = await _dbContext.ProcessedStripeEvents.AnyAsync(e => e.StripeEventId == stripeEventId, ct);
        if (exists) return false;

        _dbContext.ProcessedStripeEvents.Add(new ProcessedStripeEvent
        {
            StripeEventId = stripeEventId,
            EventType = eventType,
            ProcessedAt = DateTime.UtcNow,
        });
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
}
