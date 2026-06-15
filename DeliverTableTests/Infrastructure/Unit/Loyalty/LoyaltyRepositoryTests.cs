using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Server.Fixtures;

namespace DeliverTableTests.Infrastructure.Unit.Loyalty;

[TestFixture]
public class LoyaltyRepositoryTests
{
    private TestDatabase _testDb = null!;
    private LoyaltyRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _testDb = new TestDatabase();
        _sut = new LoyaltyRepository(_testDb.Context);
    }

    [TearDown]
    public void TearDown()
    {
        _testDb.Dispose();
    }

    private async Task<(LoyaltyAccount account, LoyaltyTransaction transaction)> SeedRedeemScenarioAsync(
        int initialBalance, int redeemPoints)
    {
        LoyaltyProgram program = new LoyaltyProgram
        {
            Id = 1,
            RestaurantId = 1,
            IsActive = true
        };
        _testDb.Context.LoyaltyPrograms.Add(program);

        LoyaltyAccount account = new LoyaltyAccount
        {
            Id = 1,
            LoyaltyProgramId = 1,
            CustomerId = 1,
            PointsBalance = initialBalance
        };
        _testDb.Context.LoyaltyAccounts.Add(account);

        // Redeem transactions store Points as negative (see OrderService.ApplyLoyaltyPointsAsync)
        LoyaltyTransaction transaction = new LoyaltyTransaction
        {
            Id = 1,
            LoyaltyAccountId = 1,
            LoyaltyAccount = account,
            Type = LoyaltyTransactionType.Redeem,
            Status = LoyaltyRedemptionStatus.Pending,
            Points = -redeemPoints,
            OrderId = 99
        };
        _testDb.Context.LoyaltyTransactions.Add(transaction);
        await _testDb.Context.SaveChangesAsync();

        return (account, transaction);
    }

    [Test]
    public async Task MarkPendingRedemptionsReversedForOrderAsync_RedeemTransaction_RestoresPointsToBalance()
    {
        // Arrange: account starts at 100, redeem transaction deducted 30 (stored as -30)
        (LoyaltyAccount? account, LoyaltyTransaction _) = await SeedRedeemScenarioAsync(initialBalance: 100, redeemPoints: 30);

        // Act
        await _sut.MarkPendingRedemptionsReversedForOrderAsync(orderId: 99, CancellationToken.None);

        // Assert: balance should be restored to 130 (100 + 30 back)
        Assert.That(account.PointsBalance, Is.EqualTo(130));
    }

    [Test]
    public async Task MarkPendingRedemptionsReversedForOrderAsync_RedeemTransaction_SetsStatusToReversed()
    {
        await SeedRedeemScenarioAsync(initialBalance: 100, redeemPoints: 30);

        await _sut.MarkPendingRedemptionsReversedForOrderAsync(orderId: 99, CancellationToken.None);

        LoyaltyTransaction tx = _testDb.Context.LoyaltyTransactions.Single(t => t.OrderId == 99);
        Assert.That(tx.Status, Is.EqualTo(LoyaltyRedemptionStatus.Reversed));
    }

    [Test]
    public async Task MarkPendingRedemptionsReversedForOrderAsync_NoMatchingOrder_DoesNotChangeBalance()
    {
        (LoyaltyAccount? account, LoyaltyTransaction _) = await SeedRedeemScenarioAsync(initialBalance: 100, redeemPoints: 30);

        // Reverse for a different orderId
        await _sut.MarkPendingRedemptionsReversedForOrderAsync(orderId: 999, CancellationToken.None);

        Assert.That(account.PointsBalance, Is.EqualTo(100));
    }
}
