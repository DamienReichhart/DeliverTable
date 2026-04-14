using DeliverTableInfrastructure.Payments;
using DeliverTableSharedLibrary.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeliverTableScheduler.Jobs;

public sealed class OrderAbandonmentSweep(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderAbandonmentSweep> logger)
    : PeriodicSweepJob(scopeFactory, logger)
{
    protected override OrderStatus TargetStatus => OrderStatus.AwaitingPayment;
    protected override TimeSpan Threshold => TimeSpan.FromMinutes(15);
    protected override Task InvokeLifecycleAsync(
        IPaymentLifecycleService svc, int orderId, CancellationToken ct) =>
        svc.CancelAbandonedOrderAsync(orderId, ct);
}
