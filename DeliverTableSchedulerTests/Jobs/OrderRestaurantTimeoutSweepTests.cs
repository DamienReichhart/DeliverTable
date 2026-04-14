using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableScheduler.Jobs;
using DeliverTableSharedLibrary.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableSchedulerTests.Jobs;

[TestFixture]
public class OrderRestaurantTimeoutSweepTests
{
    private IOrderRepository _orderRepo = null!;
    private IPaymentLifecycleService _lifecycle = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private OrderRestaurantTimeoutSweep _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _orderRepo = Substitute.For<IOrderRepository>();
        _lifecycle = Substitute.For<IPaymentLifecycleService>();
        var services = new ServiceCollection();
        services.AddSingleton(_orderRepo);
        services.AddSingleton(_lifecycle);
        _scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        _sut = new OrderRestaurantTimeoutSweep(_scopeFactory, NullLogger<OrderRestaurantTimeoutSweep>.Instance);
    }

    [Test]
    public async Task RunTickAsync_AutoRefusesOrdersPendingLongerThan24h()
    {
        var stale = new List<Order>
        {
            new() { Id = 5, Status = OrderStatus.Pending, CreatedAt = DateTime.UtcNow.AddHours(-25) },
        };
        _orderRepo.GetOrdersOlderThanAsync(OrderStatus.Pending, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                  .Returns(stale);
        _lifecycle.AutoRefuseOrderAsync(5, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.RunTickForTestAsync(CancellationToken.None);

        await _lifecycle.Received(1).AutoRefuseOrderAsync(5, Arg.Any<CancellationToken>());
    }
}
