using DeliverTableInfrastructure.Payments;
using DeliverTableSharedLibrary.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeliverTableScheduler.Jobs;

public sealed class OrderRestaurantTimeoutSweep(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderRestaurantTimeoutSweep> logger)
    : PeriodicSweepJob(scopeFactory, logger)
{
    protected override OrderStatus TargetStatus => OrderStatus.Pending;
    protected override TimeSpan Threshold => TimeSpan.FromHours(24);
    protected override Task InvokeLifecycleAsync(
        IPaymentLifecycleService svc, int orderId, CancellationToken ct) =>
        svc.AutoRefuseOrderAsync(orderId, ct);
}
