using DeliverTableInfrastructure.Invoicing;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Server.Fixtures;
using NUnit.Framework;

namespace DeliverTableTests.Infrastructure.Unit.Invoicing;

[TestFixture]
public class InvoiceNumberingServiceTests
{
    private TestDatabase _database = null!;
    private InvoiceNumberingService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _database = new TestDatabase();
        _sut = new InvoiceNumberingService(_database.Context);
    }

    [TearDown]
    public void TearDown()
    {
        _database.Dispose();
    }

    [Test]
    public async Task IssueNumberAsync_Platform_FormatsAsExpected()
    {
        var n1 = await _sut.IssueNumberAsync(InvoiceIssuerType.Platform, null, 2026, false, CancellationToken.None);
        var n2 = await _sut.IssueNumberAsync(InvoiceIssuerType.Platform, null, 2026, false, CancellationToken.None);
        Assert.That(n1, Is.EqualTo("DT-2026-000001"));
        Assert.That(n2, Is.EqualTo("DT-2026-000002"));
    }

    [Test]
    public async Task IssueNumberAsync_Restaurant_PrefixedById()
    {
        var n = await _sut.IssueNumberAsync(InvoiceIssuerType.Restaurant, 42, 2026, false, CancellationToken.None);
        Assert.That(n, Is.EqualTo("R0042-2026-000001"));
    }

    [Test]
    public async Task IssueNumberAsync_CreditNote_HasAvPrefixAndSharesCounter()
    {
        await _sut.IssueNumberAsync(InvoiceIssuerType.Restaurant, 42, 2026, false, CancellationToken.None);
        var avNumber = await _sut.IssueNumberAsync(InvoiceIssuerType.Restaurant, 42, 2026, true, CancellationToken.None);
        Assert.That(avNumber, Is.EqualTo("AV-R0042-2026-000002"));
    }

    [Test]
    public async Task IssueNumberAsync_NewYear_RestartsAtOne()
    {
        await _sut.IssueNumberAsync(InvoiceIssuerType.Platform, null, 2026, false, CancellationToken.None);
        var y2 = await _sut.IssueNumberAsync(InvoiceIssuerType.Platform, null, 2027, false, CancellationToken.None);
        Assert.That(y2, Is.EqualTo("DT-2027-000001"));
    }

    [Test]
    public async Task IssueNumberAsync_DistinctEntities_DoNotShareCounter()
    {
        var r1 = await _sut.IssueNumberAsync(InvoiceIssuerType.Restaurant, 1, 2026, false, CancellationToken.None);
        var r2 = await _sut.IssueNumberAsync(InvoiceIssuerType.Restaurant, 2, 2026, false, CancellationToken.None);
        Assert.That(r1, Is.EqualTo("R0001-2026-000001"));
        Assert.That(r2, Is.EqualTo("R0002-2026-000001"));
    }
}
