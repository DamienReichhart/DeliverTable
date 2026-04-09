using DeliverTableInfrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
        var options = new DbContextOptionsBuilder<DeliverTableContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new DeliverTableContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}
