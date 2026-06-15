using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeliverTableScheduler.Jobs;

public abstract class PeriodicSweepJob(
    IServiceScopeFactory scopeFactory,
    ILogger logger) : BackgroundService
{
    protected abstract OrderStatus TargetStatus { get; }
    protected abstract TimeSpan Threshold { get; }
    protected abstract Task InvokeLifecycleAsync(
        IPaymentLifecycleService svc, int orderId, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunTickAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Sweep tick failed"); }
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }

    internal async Task RunTickForTestAsync(CancellationToken ct) => await RunTickAsync(ct);

    private async Task RunTickAsync(CancellationToken ct)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IPaymentLifecycleService lifecycle = scope.ServiceProvider.GetRequiredService<IPaymentLifecycleService>();
        IOrderRepository orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        List<Order> stale = await orders.GetOrdersOlderThanAsync(TargetStatus, DateTime.UtcNow - Threshold, ct);
        foreach (Order o in stale)
        {
            try { await InvokeLifecycleAsync(lifecycle, o.Id, ct); }
            catch (Exception ex) { logger.LogError(ex, "Failed sweep for order {Id}", o.Id); }
        }
    }
}
