using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Server.Factories;
using DeliverTableTests.Server.Fixtures;
using NUnit.Framework;

namespace DeliverTableTests.Infrastructure.Unit.Repositories;

[TestFixture]
public class CommissionStatementRepositoryTests
{
    private TestDatabase _database = null!;
    private CommissionStatementRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _database = new TestDatabase();
        _sut = new CommissionStatementRepository(_database.Context);
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    [Test]
    public async Task InvoiceExistsForPeriodAsync_ReturnsTrue_WhenInvoicePresent()
    {
        CommissionStatement s = CommissionStatementFactory.CreateInvoice(restaurantId: 42, year: 2026, month: 5);
        _database.Context.CommissionStatements.Add(s);
        await _database.Context.SaveChangesAsync();

        bool exists = await _sut.InvoiceExistsForPeriodAsync(42, 2026, 5, default);

        Assert.That(exists, Is.True);
    }

    [Test]
    public async Task InvoiceExistsForPeriodAsync_ReturnsFalse_WhenOnlyCreditNotePresent()
    {
        CommissionStatement s = CommissionStatementFactory.CreateInvoice(restaurantId: 42, year: 2026, month: 5);
        s.Kind = CommissionStatementKind.CreditNote;
        _database.Context.CommissionStatements.Add(s);
        await _database.Context.SaveChangesAsync();

        bool exists = await _sut.InvoiceExistsForPeriodAsync(42, 2026, 5, default);

        Assert.That(exists, Is.False);
    }

    [Test]
    public async Task AllocateNextNumberAsync_ReturnsMonotonicallyIncreasing()
    {
        // Seed counter (TestDatabase doesn't run migrations).
        _database.Context.CommissionStatementCounters.Add(new() { Id = 1, NextNumber = 1 });
        await _database.Context.SaveChangesAsync();

        int a = await _sut.AllocateNextNumberAsync();
        int b = await _sut.AllocateNextNumberAsync();
        int c = await _sut.AllocateNextNumberAsync();

        Assert.That(a, Is.EqualTo(1));
        Assert.That(b, Is.EqualTo(2));
        Assert.That(c, Is.EqualTo(3));
    }
}
