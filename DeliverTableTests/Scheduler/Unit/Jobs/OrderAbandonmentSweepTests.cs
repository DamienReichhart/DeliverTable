using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableScheduler.Jobs;
using DeliverTableSharedLibrary.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace DeliverTableTests.Scheduler.Unit.Jobs;

[TestFixture]
public class OrderAbandonmentSweepTests
{
    private IOrderRepository _orderRepo = null!;
    private IPaymentLifecycleService _lifecycle = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private OrderAbandonmentSweep _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _orderRepo = Substitute.For<IOrderRepository>();
        _lifecycle = Substitute.For<IPaymentLifecycleService>();

        var services = new ServiceCollection();
        services.AddSingleton(_orderRepo);
        services.AddSingleton(_lifecycle);
        _scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        _sut = new OrderAbandonmentSweep(_scopeFactory, NullLogger<OrderAbandonmentSweep>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
    }

    [Test]
    public async Task RunTickAsync_CancelsOrdersOlderThan15Minutes()
    {
        var stale = new List<Order>
        {
            new() { Id = 1, Status = OrderStatus.AwaitingPayment, CreatedAt = DateTime.UtcNow.AddMinutes(-20) },
        };
        _orderRepo.GetOrdersOlderThanAsync(OrderStatus.AwaitingPayment, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                  .Returns(stale);
        _lifecycle.CancelAbandonedOrderAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.RunTickForTestAsync(CancellationToken.None);

        await _lifecycle.Received(1).CancelAbandonedOrderAsync(1, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunTickAsync_ContinuesOnPerOrderFailure()
    {
        var stale = new List<Order>
        {
            new() { Id = 1 }, new() { Id = 2 },
        };
        _orderRepo.GetOrdersOlderThanAsync(OrderStatus.AwaitingPayment, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                  .Returns(stale);
        _lifecycle.CancelAbandonedOrderAsync(1, Arg.Any<CancellationToken>())
                  .Throws(new Exception("boom"));
        _lifecycle.CancelAbandonedOrderAsync(2, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.RunTickForTestAsync(CancellationToken.None);

        await _lifecycle.Received(1).CancelAbandonedOrderAsync(2, Arg.Any<CancellationToken>());
    }
}
