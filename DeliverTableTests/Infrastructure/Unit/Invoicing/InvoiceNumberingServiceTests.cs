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
        string n1 = await _sut.IssueNumberAsync(InvoiceIssuerType.Platform, null, 2026, false, CancellationToken.None);
        string n2 = await _sut.IssueNumberAsync(InvoiceIssuerType.Platform, null, 2026, false, CancellationToken.None);
        Assert.That(n1, Is.EqualTo("DT-2026-000001"));
        Assert.That(n2, Is.EqualTo("DT-2026-000002"));
    }

    [Test]
    public async Task IssueNumberAsync_Restaurant_PrefixedById()
    {
        string n = await _sut.IssueNumberAsync(InvoiceIssuerType.Restaurant, 42, 2026, false, CancellationToken.None);
        Assert.That(n, Is.EqualTo("R0042-2026-000001"));
    }

    [Test]
    public async Task IssueNumberAsync_CreditNote_HasAvPrefixAndSharesCounter()
    {
        await _sut.IssueNumberAsync(InvoiceIssuerType.Restaurant, 42, 2026, false, CancellationToken.None);
        string avNumber = await _sut.IssueNumberAsync(InvoiceIssuerType.Restaurant, 42, 2026, true, CancellationToken.None);
        Assert.That(avNumber, Is.EqualTo("AV-R0042-2026-000002"));
    }

    [Test]
    public async Task IssueNumberAsync_NewYear_RestartsAtOne()
    {
        await _sut.IssueNumberAsync(InvoiceIssuerType.Platform, null, 2026, false, CancellationToken.None);
        string y2 = await _sut.IssueNumberAsync(InvoiceIssuerType.Platform, null, 2027, false, CancellationToken.None);
        Assert.That(y2, Is.EqualTo("DT-2027-000001"));
    }

    [Test]
    public async Task IssueNumberAsync_DistinctEntities_DoNotShareCounter()
    {
        string r1 = await _sut.IssueNumberAsync(InvoiceIssuerType.Restaurant, 1, 2026, false, CancellationToken.None);
        string r2 = await _sut.IssueNumberAsync(InvoiceIssuerType.Restaurant, 2, 2026, false, CancellationToken.None);
        Assert.That(r1, Is.EqualTo("R0001-2026-000001"));
        Assert.That(r2, Is.EqualTo("R0002-2026-000001"));
    }

    [Test]
    public async Task IssueNumberAsync_AfterChangeTrackerClear_StillSucceeds()
    {
        // Simulate the retry path: issue a number, clear the change tracker (as the retry
        // loop does after a DbUpdateException), then issue again. This verifies that
        // re-reading from the DB after ChangeTracker.Clear() produces correct results.
        // In-memory EF does not enforce the unique index the same way a real DB does, so
        // a true concurrent-collision test would require integration tests; this is a
        // best-effort defensive check that the retry infrastructure stays functional.
        await _sut.IssueNumberAsync(InvoiceIssuerType.Platform, null, 2026, false, CancellationToken.None);

        // Manually clear tracker to replicate what the retry loop does.
        _database.Context.ChangeTracker.Clear();

        string n2 = await _sut.IssueNumberAsync(InvoiceIssuerType.Platform, null, 2026, false, CancellationToken.None);

        Assert.That(n2, Is.EqualTo("DT-2026-000002"));
    }
}
