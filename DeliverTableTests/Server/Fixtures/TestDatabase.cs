using DeliverTableInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DeliverTableTests.Server.Fixtures;

/// <summary>
///     Provides an isolated in-memory <see cref="DeliverTableContext"/> per test.
///     Each instance gets a unique database name so tests never interfere with each other.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    public DeliverTableContext Context { get; }

    public TestDatabase()
    {
        DbContextOptions<DeliverTableContext> options = new DbContextOptionsBuilder<DeliverTableContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            // The in-memory provider does not support real transactions; suppress the warning
            // so that service code using BeginTransactionAsync works in unit tests.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        Context = new DeliverTableContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}
