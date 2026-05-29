# Monthly Commission Statements Implementation Plan — Part 2

Continuation of [part 1](2026-05-29-monthly-commission-statements.md). Same goal, same architecture, same AB refs (`PBI: AB#5994` / `Task: AB#6012`).

---

## Phase 3 — Repository (TDD)

### Task 12: `ICommissionStatementRepository` interface

**Files:**
- Create: `DeliverTableInfrastructure/Repositories/Interfaces/ICommissionStatementRepository.cs`

- [ ] **Step 1: Define interface**

```csharp
using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface ICommissionStatementRepository
{
    Task<CommissionStatement> CreateAsync(CommissionStatement statement, CancellationToken ct = default);
    Task UpdateAsync(CommissionStatement statement, CancellationToken ct = default);

    Task<CommissionStatement?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CommissionStatement?> GetByIdWithLinesAndRecipientAsync(int id, CancellationToken ct = default);

    Task<bool> InvoiceExistsForPeriodAsync(int restaurantId, int year, int month, CancellationToken ct = default);

    Task<List<int>> ListRestaurantIdsWithEligibleOrdersAsync(
        DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken ct = default);

    Task<List<Order>> ListEligibleOrdersForRestaurantAsync(
        int restaurantId, DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken ct = default);

    Task<CommissionStatementLine?> FindLineByRefundEventIdAsync(string refundEventId, CancellationToken ct = default);

    Task<int> AllocateNextNumberAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```
Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add DeliverTableInfrastructure/Repositories/Interfaces/ICommissionStatementRepository.cs
git commit -m "$(cat <<'EOF'
feat(server): add commission statement repository interface

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

### Task 13: Repository implementation

**Files:**
- Create: `DeliverTableInfrastructure/Repositories/CommissionStatementRepository.cs`
- Modify: `DeliverTableServer/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Implement repository**

```csharp
using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class CommissionStatementRepository(DeliverTableContext dbContext) : ICommissionStatementRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<CommissionStatement> CreateAsync(CommissionStatement statement, CancellationToken ct = default)
    {
        _dbContext.CommissionStatements.Add(statement);
        await _dbContext.SaveChangesAsync(ct);
        return statement;
    }

    public async Task UpdateAsync(CommissionStatement statement, CancellationToken ct = default)
    {
        statement.UpdatedAt = DateTime.UtcNow;
        _dbContext.CommissionStatements.Update(statement);
        await _dbContext.SaveChangesAsync(ct);
    }

    public Task<CommissionStatement?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _dbContext.CommissionStatements.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<CommissionStatement?> GetByIdWithLinesAndRecipientAsync(int id, CancellationToken ct = default) =>
        _dbContext.CommissionStatements
            .Include(s => s.Lines)
            .Include(s => s.RecipientRestaurant)
                .ThenInclude(r => r.Owner)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> InvoiceExistsForPeriodAsync(int restaurantId, int year, int month, CancellationToken ct = default) =>
        _dbContext.CommissionStatements.AnyAsync(
            s => s.RecipientRestaurantId == restaurantId
              && s.PeriodYear == year
              && s.PeriodMonth == month
              && s.Kind == CommissionStatementKind.Invoice,
            ct);

    public Task<List<int>> ListRestaurantIdsWithEligibleOrdersAsync(
        DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken ct = default) =>
        _dbContext.Orders
            .Where(o => o.Status == DeliverTableSharedLibrary.Enums.OrderStatus.Delivered
                     && o.PaymentStatus == DeliverTableSharedLibrary.Enums.PaymentStatus.Completed
                     && o.DeliveredAt != null
                     && o.DeliveredAt >= periodStartUtc
                     && o.DeliveredAt < periodEndUtc
                     && o.CommissionStatementId == null)
            .Select(o => o.RestaurantId)
            .Distinct()
            .ToListAsync(ct);

    public Task<List<Order>> ListEligibleOrdersForRestaurantAsync(
        int restaurantId, DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken ct = default) =>
        _dbContext.Orders
            .Include(o => o.Payments).ThenInclude(p => p.Refunds)
            .Where(o => o.RestaurantId == restaurantId
                     && o.Status == DeliverTableSharedLibrary.Enums.OrderStatus.Delivered
                     && o.PaymentStatus == DeliverTableSharedLibrary.Enums.PaymentStatus.Completed
                     && o.DeliveredAt != null
                     && o.DeliveredAt >= periodStartUtc
                     && o.DeliveredAt < periodEndUtc
                     && o.CommissionStatementId == null)
            .OrderBy(o => o.DeliveredAt)
            .ToListAsync(ct);

    public Task<CommissionStatementLine?> FindLineByRefundEventIdAsync(string refundEventId, CancellationToken ct = default) =>
        _dbContext.CommissionStatementLines.FirstOrDefaultAsync(l => l.RefundEventId == refundEventId, ct);

    public async Task<int> AllocateNextNumberAsync(CancellationToken ct = default)
    {
        const int maxAttempts = 5;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var counter = await _dbContext.CommissionStatementCounters
                    .FirstOrDefaultAsync(c => c.Id == 1, ct)
                    ?? throw new InvalidOperationException("CommissionStatementCounter row missing — migration seed not applied.");
                var n = counter.NextNumber;
                counter.NextNumber = n + 1;
                await _dbContext.SaveChangesAsync(ct);
                return n;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                _dbContext.ChangeTracker.Clear();
                await Task.Delay(10 * attempt, ct);
            }
        }
        throw new InvalidOperationException("Failed to allocate commission statement number after retries.");
    }
}
```

> Note: if `Order.Payments` does not currently include a `Refunds` navigation, replace the include chain with a direct query on `Refunds` joined by `PaymentId`. Verify against `Payment.cs` model.

- [ ] **Step 2: Register in DI**

Open `DeliverTableServer/Extensions/ServiceCollectionExtensions.cs`. In the `RegisterRepositories` method, after the existing `services.AddScoped<IInvoiceRepository, InvoiceRepository>();` line, add:

```csharp
services.AddScoped<ICommissionStatementRepository, CommissionStatementRepository>();
```

- [ ] **Step 3: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```
Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add DeliverTableInfrastructure/Repositories/CommissionStatementRepository.cs DeliverTableServer/Extensions/ServiceCollectionExtensions.cs
git commit -m "$(cat <<'EOF'
feat(server): add commission statement repository

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

### Task 14: Repository integration tests

**Files:**
- Create: `DeliverTableTests/Server/Factories/CommissionStatementFactory.cs`
- Create: `DeliverTableTests/Infrastructure/Unit/Repositories/CommissionStatementRepositoryTests.cs`

- [ ] **Step 1: Add factory**

```csharp
using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableTests.Server.Factories;

public static class CommissionStatementFactory
{
    public static CommissionStatement CreateInvoice(
        int restaurantId,
        int year,
        int month,
        string number = "COMM-2026-05-000001") => new()
    {
        Number = number,
        Kind = CommissionStatementKind.Invoice,
        RecipientRestaurantId = restaurantId,
        PeriodYear = year,
        PeriodMonth = month,
        IssuedAt = DateTime.UtcNow,
        Currency = "EUR",
        Status = CommissionStatementStatus.Queued,
        IssuerLegalSnapshotJson = "{}",
        RecipientSnapshotJson = "{}",
    };
}
```

- [ ] **Step 2: Write failing test for `InvoiceExistsForPeriodAsync`**

```csharp
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
        var s = CommissionStatementFactory.CreateInvoice(restaurantId: 42, year: 2026, month: 5);
        _database.Context.CommissionStatements.Add(s);
        await _database.Context.SaveChangesAsync();

        var exists = await _sut.InvoiceExistsForPeriodAsync(42, 2026, 5, default);

        Assert.That(exists, Is.True);
    }

    [Test]
    public async Task InvoiceExistsForPeriodAsync_ReturnsFalse_WhenOnlyCreditNotePresent()
    {
        var s = CommissionStatementFactory.CreateInvoice(restaurantId: 42, year: 2026, month: 5);
        s.Kind = CommissionStatementKind.CreditNote;
        _database.Context.CommissionStatements.Add(s);
        await _database.Context.SaveChangesAsync();

        var exists = await _sut.InvoiceExistsForPeriodAsync(42, 2026, 5, default);

        Assert.That(exists, Is.False);
    }

    [Test]
    public async Task AllocateNextNumberAsync_ReturnsMonotonicallyIncreasing()
    {
        // Seed counter (TestDatabase doesn't run migrations).
        _database.Context.CommissionStatementCounters.Add(new() { Id = 1, NextNumber = 1 });
        await _database.Context.SaveChangesAsync();

        var a = await _sut.AllocateNextNumberAsync();
        var b = await _sut.AllocateNextNumberAsync();
        var c = await _sut.AllocateNextNumberAsync();

        Assert.That(a, Is.EqualTo(1));
        Assert.That(b, Is.EqualTo(2));
        Assert.That(c, Is.EqualTo(3));
    }
}
```

- [ ] **Step 3: Run tests, verify failures**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~CommissionStatementRepositoryTests"
```
Expected: tests fail (or pass if implementation already exists — the goal here is verifying the harness sees them).

- [ ] **Step 4: Adjust implementation if any test fails for the wrong reason**

Re-read the test failure and the repository code from Task 13. Common in-memory-EF pitfalls: `.HasFilter` is ignored by the in-memory provider, so uniqueness constraints from partial indexes won't fire in tests — assert on behavior, not on DB-level exceptions.

- [ ] **Step 5: Re-run tests, expect pass**

Same command as step 3. Expected: all three pass.

- [ ] **Step 6: Commit**

```bash
git add DeliverTableTests/Server/Factories/CommissionStatementFactory.cs DeliverTableTests/Infrastructure/Unit/Repositories/CommissionStatementRepositoryTests.cs
git commit -m "$(cat <<'EOF'
test(server): add commission statement repository tests

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

## Phase 4 — `Order.DeliveredAt` wiring

### Task 15: Populate `Order.DeliveredAt` on Delivered transition

**Files:**
- Modify: wherever `Order.Status = OrderStatus.Delivered` is assigned (search to find)
- Test: `DeliverTableTests/Server/Unit/Services/OrderServiceTests.cs` (or relevant existing test file)

- [ ] **Step 1: Find all assignments to `OrderStatus.Delivered`**

```bash
grep -rn "Status = OrderStatus.Delivered\|Status = DeliverTableSharedLibrary.Enums.OrderStatus.Delivered\|\.Delivered;" \
  DeliverTableServer DeliverTableInfrastructure DeliverTableScheduler --include="*.cs"
```

List the results. Likely candidates: `OrderService`, possibly a state-transition helper, possibly the dispatch flow.

- [ ] **Step 2: Write a failing test against the most direct caller**

For each call site, add a test asserting `DeliveredAt` is set to a non-null UTC time when status transitions to `Delivered`. Mirror the existing tests in that file's style (NSubstitute on repos, etc.). Example shape:

```csharp
[Test]
public async Task MarkDelivered_SetsDeliveredAt_WhenTransitioning()
{
    var order = OrderFactory.CreateOrder(status: OrderStatus.Delivering);
    orderRepo.GetByIdAsync(order.Id, default).Returns(order);

    var result = await _sut.MarkDeliveredAsync(order.Id, default);

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(order.DeliveredAt, Is.Not.Null);
    Assert.That(order.DeliveredAt!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
}
```

(Adjust method names to match the real call site.)

- [ ] **Step 3: Run test, expect failure**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~MarkDelivered_SetsDeliveredAt"
```
Expected: FAIL (DeliveredAt remains null).

- [ ] **Step 4: At each call site found in step 1, add `order.DeliveredAt = DateTime.UtcNow;` immediately after `order.Status = OrderStatus.Delivered;`**

- [ ] **Step 5: Re-run, expect pass**

Same command as step 3. Expected: PASS.

- [ ] **Step 6: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```
Expected: build succeeds.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
feat(server): set Order.DeliveredAt on Delivered transition

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

## Phase 5 — Commission statement service: generation (TDD)

### Task 16: Service interface

**Files:**
- Create: `DeliverTableServer/Services/ICommissionStatementService.cs`

- [ ] **Step 1: Define interface**

```csharp
using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableServer.Services;

public interface ICommissionStatementService
{
    Task<ServiceResult<CommissionStatementGenerationResultDto>> GenerateForPeriodAsync(
        int year, int month, CancellationToken ct);

    Task<ServiceResult> HandleRefundForPriorPeriodAsync(
        int orderId, int refundId, string stripeRefundId, decimal refundedAmount, CancellationToken ct);
}
```

- [ ] **Step 2: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```
Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add DeliverTableServer/Services/ICommissionStatementService.cs
git commit -m "$(cat <<'EOF'
feat(server): add commission statement service interface

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

### Task 17: Service `GenerateForPeriodAsync` — happy path test

**Files:**
- Create: `DeliverTableTests/Server/Unit/Services/CommissionStatementServiceTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using DeliverTableInfrastructure.Configuration;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Configuration;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class CommissionStatementServiceTests
{
    private ICommissionStatementRepository _repo = null!;
    private IRestaurantRepository _restaurantRepo = null!;
    private IMessagePublisher _publisher = null!;
    private AppEnvironment _env = null!;
    private CommissionStatementService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ICommissionStatementRepository>();
        _restaurantRepo = Substitute.For<IRestaurantRepository>();
        _publisher = Substitute.For<IMessagePublisher>();
        _env = TestAppEnvironment.WithCommission(rate: 0.10m, vatApplicable: true);

        _sut = new CommissionStatementService(_repo, _restaurantRepo, _publisher, _env, NullLogger);
    }

    [Test]
    public async Task GenerateForPeriodAsync_CreatesOneStatement_PerRestaurantWithEligibleOrders()
    {
        _repo.ListRestaurantIdsWithEligibleOrdersAsync(default, default, default)
             .ReturnsForAnyArgs(new List<int> { 7 });
        _repo.InvoiceExistsForPeriodAsync(7, 2026, 5, default).Returns(false);
        var restaurant = TestEntities.Restaurant(id: 7);
        _restaurantRepo.GetByIdWithOwnerAsync(7, default).Returns(restaurant);
        var order = TestEntities.DeliveredOrder(restaurantId: 7, total: 100m, deliveredAt: new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc));
        _repo.ListEligibleOrdersForRestaurantAsync(7, default, default, default)
             .ReturnsForAnyArgs(new List<Order> { order });
        _repo.AllocateNextNumberAsync(default).Returns(1);

        var result = await _sut.GenerateForPeriodAsync(2026, 5, default);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.StatementsCreated, Is.EqualTo(1));
        await _repo.Received(1).CreateAsync(Arg.Is<CommissionStatement>(s =>
            s.RecipientRestaurantId == 7
            && s.PeriodYear == 2026
            && s.PeriodMonth == 5
            && s.Kind == CommissionStatementKind.Invoice
            && s.Lines.Count == 1
            && s.Lines[0].LineHt == 10m), default);
        await _publisher.Received(1).PublishAsync(
            MessagingExchanges.CommissionStatement,
            Arg.Any<CommissionStatementJobMessage>(),
            default);
    }
}
```

> Helpers (`TestAppEnvironment`, `TestEntities`, `NullLogger`): if they don't yet exist, create them as minimal stubs inside the same test file or in `DeliverTableTests/Server/Fixtures/`. Reuse `ServerEntityFactory.CreateRestaurant` where possible.

- [ ] **Step 2: Run test, expect compilation error**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~CommissionStatementServiceTests"
```
Expected: FAIL — `CommissionStatementService` does not exist.

- [ ] **Step 3: Commit the failing test**

```bash
git add DeliverTableTests/Server/Unit/Services/CommissionStatementServiceTests.cs
git commit -m "$(cat <<'EOF'
test(server): add failing test for commission statement generation

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

### Task 18: Service `GenerateForPeriodAsync` — implementation

**Files:**
- Create: `DeliverTableServer/Services/CommissionStatementService.cs`
- Modify: `DeliverTableServer/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Implement service**

```csharp
using System.Text.Json;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Invoicing; // for InvoiceLegalSnapshotDto
using DeliverTableSharedLibrary.Enums;
using Microsoft.Extensions.Logging;

namespace DeliverTableServer.Services;

public class CommissionStatementService(
    ICommissionStatementRepository repo,
    IRestaurantRepository restaurantRepo,
    IMessagePublisher publisher,
    AppEnvironment env,
    ILogger<CommissionStatementService> logger) : ICommissionStatementService
{
    private static readonly TimeZoneInfo ParisTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    public async Task<ServiceResult<CommissionStatementGenerationResultDto>> GenerateForPeriodAsync(
        int year, int month, CancellationToken ct)
    {
        if (month is < 1 or > 12 || year is < 2000 or > 9999)
            return ServiceError.BadRequest(ErrorMessages.CommissionStatementInvalidPeriod);

        var (startUtc, endUtc) = ComputePeriodBoundsUtc(year, month);
        var result = new CommissionStatementGenerationResultDto
        {
            PeriodYear = year,
            PeriodMonth = month,
        };

        var restaurantIds = await repo.ListRestaurantIdsWithEligibleOrdersAsync(startUtc, endUtc, ct);
        result.RestaurantsProcessed = restaurantIds.Count;

        foreach (var restaurantId in restaurantIds)
        {
            try
            {
                if (await repo.InvoiceExistsForPeriodAsync(restaurantId, year, month, ct))
                {
                    result.RestaurantsSkipped++;
                    continue;
                }

                var restaurant = await restaurantRepo.GetByIdWithOwnerAsync(restaurantId, ct);
                if (restaurant is null) continue;

                var orders = await repo.ListEligibleOrdersForRestaurantAsync(restaurantId, startUtc, endUtc, ct);
                if (orders.Count == 0)
                {
                    result.RestaurantsSkipped++;
                    continue;
                }

                var statement = BuildStatement(restaurant, year, month, orders);
                statement.Number = await FormatInvoiceNumberAsync(year, month, ct);

                foreach (var o in orders) o.CommissionStatementId = statement.Id;
                await repo.CreateAsync(statement, ct);

                await publisher.PublishAsync(
                    MessagingExchanges.CommissionStatement,
                    new CommissionStatementJobMessage(statement.Id),
                    ct);

                result.StatementsCreated++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate commission statement for restaurant {RestaurantId}", restaurantId);
                result.Failures.Add(new GenerationFailureDto
                {
                    RestaurantId = restaurantId,
                    Reason = ex.Message,
                });
            }
        }

        return result;
    }

    public Task<ServiceResult> HandleRefundForPriorPeriodAsync(
        int orderId, int refundId, string stripeRefundId, decimal refundedAmount, CancellationToken ct)
        => throw new NotImplementedException("Implemented in Task 19.");

    internal static (DateTime startUtc, DateTime endUtc) ComputePeriodBoundsUtc(int year, int month)
    {
        var startLocal = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var endLocal = startLocal.AddMonths(1);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, ParisTz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, ParisTz);
        return (startUtc, endUtc);
    }

    private CommissionStatement BuildStatement(Restaurant restaurant, int year, int month, List<Order> orders)
    {
        var issuerSnapshot = JsonSerializer.Serialize(new InvoiceLegalSnapshotDto(
            Name: env.PlatformLegalName,
            LegalForm: env.PlatformLegalForm,
            Siret: env.PlatformSiret,
            VatNumber: env.PlatformVatNumber,
            Address: env.PlatformAddress));
        var recipientSnapshot = JsonSerializer.Serialize(new InvoiceLegalSnapshotDto(
            Name: restaurant.LegalName,
            LegalForm: restaurant.LegalForm,
            Siret: restaurant.Siret,
            VatNumber: restaurant.VatNumber ?? string.Empty,
            Address: restaurant.LegalAddress));

        var statement = new CommissionStatement
        {
            Kind = CommissionStatementKind.Invoice,
            RecipientRestaurantId = restaurant.Id,
            PeriodYear = year,
            PeriodMonth = month,
            IssuedAt = DateTime.UtcNow,
            Currency = "EUR",
            Status = CommissionStatementStatus.Queued,
            IssuerLegalSnapshotJson = issuerSnapshot,
            RecipientSnapshotJson = recipientSnapshot,
            RecipientEmailSnapshot = restaurant.Owner?.Email,
        };

        var rateVat = env.PlatformVatApplicable ? 20m : 0m;
        int sort = 0;
        foreach (var order in orders)
        {
            var net = order.TotalAmount - TotalRefundedFor(order);
            if (net <= 0) continue;
            var commissionHt = Math.Round(net * env.PlatformCommissionRate, 2, MidpointRounding.AwayFromZero);
            var commissionTtc = Math.Round(commissionHt * (1 + rateVat / 100m), 2, MidpointRounding.AwayFromZero);
            var commissionVat = Math.Round(commissionTtc - commissionHt, 2, MidpointRounding.AwayFromZero);

            statement.Lines.Add(new CommissionStatementLine
            {
                OrderId = order.Id,
                OrderNumber = order.Id.ToString(),
                OrderCompletedAt = order.DeliveredAt ?? order.UpdatedAt,
                OrderTotalAmount = net,
                CommissionRateSnapshot = env.PlatformCommissionRate,
                VatRate = rateVat,
                LineHt = commissionHt,
                LineVat = commissionVat,
                LineTtc = commissionTtc,
                SortOrder = sort++,
            });
        }

        statement.TotalHt = statement.Lines.Sum(l => l.LineHt);
        statement.TotalVat = statement.Lines.Sum(l => l.LineVat);
        statement.TotalTtc = statement.Lines.Sum(l => l.LineTtc);
        return statement;
    }

    private static decimal TotalRefundedFor(Order order)
        => order.Payments
            .SelectMany(p => p.Refunds ?? Enumerable.Empty<Refund>())
            .Sum(r => r.Amount);

    private async Task<string> FormatInvoiceNumberAsync(int year, int month, CancellationToken ct)
    {
        var seq = await repo.AllocateNextNumberAsync(ct);
        return $"COMM-{year:D4}-{month:D2}-{seq:D6}";
    }
}
```

> Notes:
> - Real Order Number: this repo uses `Order.Id.ToString()` as the public identifier; if a friendlier reference exists on the Order entity, swap it in.
> - `IRestaurantRepository.GetByIdWithOwnerAsync` must exist; if it doesn't, add it. Plan currently assumes either it exists or we add a thin wrapper:
>   ```csharp
>   Task<Restaurant?> GetByIdWithOwnerAsync(int id, CancellationToken ct);
>   ```
>   Implementation: `_dbContext.Restaurants.Include(r => r.Owner).FirstOrDefaultAsync(r => r.Id == id, ct)`. Add the method + interface entry in a sub-step if needed.
> - `InvoiceLegalSnapshotDto` is a record currently defined inline in `InvoiceService`; if it's not in `DeliverTableSharedLibrary`, move it there (its own file) and update both usages.

- [ ] **Step 2: Register service in DI**

In `DeliverTableServer/Extensions/ServiceCollectionExtensions.cs` → `RegisterServices`, after the existing `services.AddScoped<IInvoiceService, InvoiceService>();` line, add:

```csharp
services.AddScoped<ICommissionStatementService, CommissionStatementService>();
```

- [ ] **Step 3: Run the Task 17 test**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~GenerateForPeriodAsync_CreatesOneStatement"
```
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add DeliverTableServer/Services/CommissionStatementService.cs DeliverTableServer/Extensions/ServiceCollectionExtensions.cs
# also add any helpers you created (InvoiceLegalSnapshotDto move, IRestaurantRepository extension, etc.)
git commit -m "$(cat <<'EOF'
feat(server): implement commission statement generation service

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

### Task 19: Service `GenerateForPeriodAsync` — edge cases

**Files:**
- Modify: `DeliverTableTests/Server/Unit/Services/CommissionStatementServiceTests.cs`

For each of the following, follow the same TDD micro-cycle: write the test → run → fix implementation if needed → commit.

- [ ] **Test 1: skip restaurant with no eligible orders**

```csharp
[Test]
public async Task GenerateForPeriodAsync_SkipsRestaurant_WhenNoEligibleOrders()
{
    _repo.ListRestaurantIdsWithEligibleOrdersAsync(default, default, default)
         .ReturnsForAnyArgs(new List<int> { 7 });
    _repo.InvoiceExistsForPeriodAsync(7, 2026, 5, default).Returns(false);
    _restaurantRepo.GetByIdWithOwnerAsync(7, default).Returns(TestEntities.Restaurant(7));
    _repo.ListEligibleOrdersForRestaurantAsync(7, default, default, default)
         .ReturnsForAnyArgs(new List<Order>());

    var r = await _sut.GenerateForPeriodAsync(2026, 5, default);

    Assert.That(r.Value!.StatementsCreated, Is.EqualTo(0));
    Assert.That(r.Value.RestaurantsSkipped, Is.EqualTo(1));
    await _repo.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
}
```

- [ ] **Test 2: skip when invoice already exists for period (idempotency)**

```csharp
[Test]
public async Task GenerateForPeriodAsync_SkipsRestaurant_WhenInvoiceAlreadyExists()
{
    _repo.ListRestaurantIdsWithEligibleOrdersAsync(default, default, default)
         .ReturnsForAnyArgs(new List<int> { 7 });
    _repo.InvoiceExistsForPeriodAsync(7, 2026, 5, default).Returns(true);

    var r = await _sut.GenerateForPeriodAsync(2026, 5, default);

    Assert.That(r.Value!.RestaurantsSkipped, Is.EqualTo(1));
    await _repo.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
}
```

- [ ] **Test 3: partial refund reduces commission base**

```csharp
[Test]
public async Task GenerateForPeriodAsync_AppliesCommissionOnNetAmount_WhenPartiallyRefunded()
{
    _repo.ListRestaurantIdsWithEligibleOrdersAsync(default, default, default)
         .ReturnsForAnyArgs(new List<int> { 7 });
    _repo.InvoiceExistsForPeriodAsync(7, 2026, 5, default).Returns(false);
    _restaurantRepo.GetByIdWithOwnerAsync(7, default).Returns(TestEntities.Restaurant(7));
    var order = TestEntities.DeliveredOrder(restaurantId: 7, total: 100m,
        deliveredAt: new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc));
    order.Payments.Add(TestEntities.PaymentWithRefund(amount: 100m, refund: 30m));
    _repo.ListEligibleOrdersForRestaurantAsync(7, default, default, default)
         .ReturnsForAnyArgs(new List<Order> { order });
    _repo.AllocateNextNumberAsync(default).Returns(1);

    await _sut.GenerateForPeriodAsync(2026, 5, default);

    await _repo.Received(1).CreateAsync(Arg.Is<CommissionStatement>(s =>
        s.Lines.Count == 1
        && s.Lines[0].OrderTotalAmount == 70m
        && s.Lines[0].LineHt == 7m), default);
}
```

- [ ] **Test 4: invalid period rejected**

```csharp
[Test]
public async Task GenerateForPeriodAsync_ReturnsBadRequest_WhenInvalidMonth()
{
    var r = await _sut.GenerateForPeriodAsync(2026, 13, default);
    Assert.That(r.IsSuccess, Is.False);
    Assert.That(r.Error!.StatusCode, Is.EqualTo(400));
}
```

- [ ] **Test 5: rate snapshot captured per line**

```csharp
[Test]
public async Task GenerateForPeriodAsync_SnapshotsCommissionRate_OnEachLine()
{
    _env = TestAppEnvironment.WithCommission(rate: 0.15m, vatApplicable: false);
    _sut = new CommissionStatementService(_repo, _restaurantRepo, _publisher, _env, NullLogger);
    // ... same arrange as Test 1 with one order ...

    await _sut.GenerateForPeriodAsync(2026, 5, default);

    await _repo.Received(1).CreateAsync(Arg.Is<CommissionStatement>(s =>
        s.Lines.All(l => l.CommissionRateSnapshot == 0.15m)
        && s.Lines.All(l => l.VatRate == 0m)), default);
}
```

- [ ] **Run, fix, commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~CommissionStatementServiceTests"
```
Expected: all five new tests pass after applying the corresponding service tweaks (most should pass with no change since the service was built against the spec).

```bash
git add DeliverTableTests/Server/Unit/Services/CommissionStatementServiceTests.cs DeliverTableServer/Services/CommissionStatementService.cs
git commit -m "$(cat <<'EOF'
test(server): cover edge cases in commission statement generation

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

## Phase 6 — Refund handling (TDD)

### Task 20: `HandleRefundForPriorPeriodAsync` — failing tests

**Files:**
- Modify: `DeliverTableTests/Server/Unit/Services/CommissionStatementServiceTests.cs`

- [ ] **Step 1: Add three tests**

```csharp
[Test]
public async Task HandleRefundForPriorPeriod_NoOp_WhenOrderHasNoStatement()
{
    var order = TestEntities.DeliveredOrder(restaurantId: 7, total: 100m,
        deliveredAt: new DateTime(2026, 5, 10));
    order.CommissionStatementId = null;
    _orderRepo.GetByIdAsync(order.Id, default).Returns(order);

    var r = await _sut.HandleRefundForPriorPeriodAsync(order.Id, refundId: 99, "re_x", 30m, default);

    Assert.That(r.IsSuccess, Is.True);
    await _repo.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
}

[Test]
public async Task HandleRefundForPriorPeriod_NoOp_WhenRefundEventAlreadyProcessed()
{
    var order = TestEntities.DeliveredOrder(restaurantId: 7, total: 100m,
        deliveredAt: new DateTime(2026, 5, 10));
    order.CommissionStatementId = 42;
    _orderRepo.GetByIdAsync(order.Id, default).Returns(order);
    _repo.FindLineByRefundEventIdAsync("re_x", default).Returns(new CommissionStatementLine());

    var r = await _sut.HandleRefundForPriorPeriodAsync(order.Id, refundId: 99, "re_x", 30m, default);

    Assert.That(r.IsSuccess, Is.True);
    await _repo.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
}

[Test]
public async Task HandleRefundForPriorPeriod_CreatesCreditNote_UsingSnapshottedRate()
{
    var order = TestEntities.DeliveredOrder(restaurantId: 7, total: 100m,
        deliveredAt: new DateTime(2026, 5, 10));
    order.CommissionStatementId = 42;
    var originalStatement = CommissionStatementFactory.CreateInvoice(7, 2026, 5);
    originalStatement.Id = 42;
    originalStatement.Lines.Add(new CommissionStatementLine
    {
        OrderId = order.Id, CommissionRateSnapshot = 0.20m, VatRate = 20m,
    });
    _orderRepo.GetByIdAsync(order.Id, default).Returns(order);
    _repo.FindLineByRefundEventIdAsync("re_x", default).Returns((CommissionStatementLine?)null);
    _repo.GetByIdWithLinesAndRecipientAsync(42, default).Returns(originalStatement);
    _restaurantRepo.GetByIdWithOwnerAsync(7, default).Returns(TestEntities.Restaurant(7));
    _repo.AllocateNextNumberAsync(default).Returns(99);

    // env still 0.10m / 20% — credit note must use snapshotted 0.20 / 20%.
    var r = await _sut.HandleRefundForPriorPeriodAsync(order.Id, refundId: 1, "re_x", refundedAmount: 30m, default);

    Assert.That(r.IsSuccess, Is.True);
    await _repo.Received(1).CreateAsync(Arg.Is<CommissionStatement>(s =>
        s.Kind == CommissionStatementKind.CreditNote
        && s.RelatedStatementId == 42
        && s.PeriodYear == 2026 && s.PeriodMonth == 5
        && s.Lines.Count == 1
        && s.Lines[0].RefundEventId == "re_x"
        && s.Lines[0].LineHt == -6m            // 30 * 0.20
        && s.Lines[0].LineTtc == -7.20m), default);
    await _publisher.Received(1).PublishAsync(
        MessagingExchanges.CommissionStatement,
        Arg.Any<CommissionStatementJobMessage>(),
        default);
}
```

You will need an `_orderRepo` field — add to `SetUp`:

```csharp
private IOrderRepository _orderRepo = null!;
// in SetUp:
_orderRepo = Substitute.For<IOrderRepository>();
_sut = new CommissionStatementService(_repo, _restaurantRepo, _orderRepo, _publisher, _env, NullLogger);
```

(This changes the service constructor — Task 21 will adjust the production code to match.)

- [ ] **Step 2: Run, expect compilation failure (constructor mismatch)**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~HandleRefundForPriorPeriod"
```
Expected: build fails.

- [ ] **Step 3: Commit failing tests**

```bash
git add DeliverTableTests/Server/Unit/Services/CommissionStatementServiceTests.cs
git commit -m "$(cat <<'EOF'
test(server): add failing tests for prior-period refund handling

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

### Task 21: `HandleRefundForPriorPeriodAsync` — implementation

**Files:**
- Modify: `DeliverTableServer/Services/CommissionStatementService.cs`

- [ ] **Step 1: Add `IOrderRepository orderRepo` to the constructor and implement the method**

Replace the throwing stub:

```csharp
public async Task<ServiceResult> HandleRefundForPriorPeriodAsync(
    int orderId, int refundId, string stripeRefundId, decimal refundedAmount, CancellationToken ct)
{
    var order = await orderRepo.GetByIdAsync(orderId, ct);
    if (order is null) return ServiceResult.Success();
    if (order.CommissionStatementId is null) return ServiceResult.Success();

    var existingLine = await repo.FindLineByRefundEventIdAsync(stripeRefundId, ct);
    if (existingLine is not null) return ServiceResult.Success();

    var original = await repo.GetByIdWithLinesAndRecipientAsync(order.CommissionStatementId.Value, ct);
    if (original is null) return ServiceError.NotFound(ErrorMessages.CommissionStatementNotFound);
    var originalLine = original.Lines.FirstOrDefault(l => l.OrderId == orderId);
    if (originalLine is null) return ServiceResult.Success();

    var restaurant = await restaurantRepo.GetByIdWithOwnerAsync(order.RestaurantId, ct);
    if (restaurant is null) return ServiceError.NotFound(ErrorMessages.CommissionStatementNotFound);

    var rate = originalLine.CommissionRateSnapshot;
    var vat = originalLine.VatRate;
    var ht = Math.Round(refundedAmount * rate, 2, MidpointRounding.AwayFromZero);
    var ttc = Math.Round(ht * (1 + vat / 100m), 2, MidpointRounding.AwayFromZero);
    var vatAmount = Math.Round(ttc - ht, 2, MidpointRounding.AwayFromZero);

    var creditNote = new CommissionStatement
    {
        Kind = CommissionStatementKind.CreditNote,
        RecipientRestaurantId = restaurant.Id,
        PeriodYear = original.PeriodYear,
        PeriodMonth = original.PeriodMonth,
        IssuedAt = DateTime.UtcNow,
        Currency = "EUR",
        Status = CommissionStatementStatus.Queued,
        RelatedStatementId = original.Id,
        IssuerLegalSnapshotJson = original.IssuerLegalSnapshotJson,
        RecipientSnapshotJson = original.RecipientSnapshotJson,
        RecipientEmailSnapshot = restaurant.Owner?.Email,
        TotalHt = -ht,
        TotalVat = -vatAmount,
        TotalTtc = -ttc,
    };
    creditNote.Lines.Add(new CommissionStatementLine
    {
        OrderId = order.Id,
        OrderNumber = order.Id.ToString(),
        OrderCompletedAt = order.DeliveredAt ?? order.UpdatedAt,
        OrderTotalAmount = refundedAmount,
        CommissionRateSnapshot = rate,
        VatRate = vat,
        LineHt = -ht,
        LineVat = -vatAmount,
        LineTtc = -ttc,
        RefundEventId = stripeRefundId,
        SortOrder = 0,
    });

    var seq = await repo.AllocateNextNumberAsync(ct);
    creditNote.Number = $"AVOIR-COMM-{original.PeriodYear:D4}-{original.PeriodMonth:D2}-{seq:D6}";

    order.CommissionRefundStatementId = creditNote.Id;
    await repo.CreateAsync(creditNote, ct);
    await orderRepo.UpdateAsync(order, ct);

    await publisher.PublishAsync(MessagingExchanges.CommissionStatement,
        new CommissionStatementJobMessage(creditNote.Id), ct);

    return ServiceResult.Success();
}
```

Also add `IOrderRepository orderRepo` to the primary constructor.

- [ ] **Step 2: Run, expect pass**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~HandleRefundForPriorPeriod"
```
Expected: all three pass.

- [ ] **Step 3: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```
Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add DeliverTableServer/Services/CommissionStatementService.cs
git commit -m "$(cat <<'EOF'
feat(server): implement prior-period refund credit-note flow

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

## Phase 7 — Cutover gate in `InvoiceService`

### Task 22: Failing test for cutover behaviour

**Files:**
- Modify: `DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs` (or create if missing)

- [ ] **Step 1: Add two tests**

```csharp
[Test]
public async Task CreatePendingInvoicesForCapturedOrderAsync_StillCreatesCommissionInvoice_BeforeCutover()
{
    // Use ITimeProvider abstraction if present; otherwise pass an override.
    // Set cutover effective time via test seam (see Task 23).
    SystemClock.OverrideUtcNow = CommissionInvoicingCutover.MonthlyStartUtc.AddDays(-1);

    // ... arrange order, restaurant, repo mocks ...

    var result = await _sut.CreatePendingInvoicesForCapturedOrderAsync(order.Id, default);

    await _invoiceRepo.Received(1).CreateBatchAsync(
        Arg.Is<IEnumerable<Invoice>>(list => list.Any(i => i.Kind == InvoiceKind.CommissionInvoiceToRestaurant)),
        default);
}

[Test]
public async Task CreatePendingInvoicesForCapturedOrderAsync_SkipsCommissionInvoice_AfterCutover()
{
    SystemClock.OverrideUtcNow = CommissionInvoicingCutover.MonthlyStartUtc.AddMinutes(1);

    // ... same arrange ...

    var result = await _sut.CreatePendingInvoicesForCapturedOrderAsync(order.Id, default);

    await _invoiceRepo.Received(1).CreateBatchAsync(
        Arg.Is<IEnumerable<Invoice>>(list =>
            list.All(i => i.Kind != InvoiceKind.CommissionInvoiceToRestaurant)
            && list.Any(i => i.Kind == InvoiceKind.OrderInvoiceToCustomer)),
        default);
}
```

If a `SystemClock`/`ITimeProvider` seam doesn't already exist in the codebase, in this task introduce it minimally:

- Create `DeliverTableServer/Common/ISystemClock.cs`:
  ```csharp
  public interface ISystemClock { DateTime UtcNow { get; } }
  public sealed class SystemClock : ISystemClock { public DateTime UtcNow => DateTime.UtcNow; }
  ```
- Register in DI singleton.
- Inject into `InvoiceService` and use `_clock.UtcNow` in the cutover gate.
- Tests use NSubstitute to control the clock.

- [ ] **Step 2: Run, expect failure**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~CreatePendingInvoicesForCapturedOrderAsync_StillCreatesCommissionInvoice_BeforeCutover|FullyQualifiedName~CreatePendingInvoicesForCapturedOrderAsync_SkipsCommissionInvoice_AfterCutover"
```
Expected: FAIL.

- [ ] **Step 3: Commit failing tests**

```bash
git add DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs DeliverTableServer/Common/ISystemClock.cs DeliverTableServer/Extensions/ServiceCollectionExtensions.cs
git commit -m "$(cat <<'EOF'
test(server): add failing tests for commission invoice cutover gate

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

### Task 23: Implement cutover gate in `InvoiceService`

**Files:**
- Modify: `DeliverTableServer/Services/InvoiceService.cs`

- [ ] **Step 1: Inject `ISystemClock` and gate commission-invoice creation**

In `CreatePendingInvoicesForCapturedOrderAsync` (around line 31), the existing code creates customer + commission invoices together. Gate commission creation:

```csharp
var invoices = new List<Invoice> { customerInvoice };
if (clock.UtcNow < CommissionInvoicingCutover.MonthlyStartUtc)
{
    var commissionNumber = await _numberingService.IssueNumberAsync(...);
    var commissionInvoice = BuildCommissionInvoice(order, restaurant, commissionNumber);
    invoices.Add(commissionInvoice);
}
await _invoiceRepository.CreateBatchAsync(invoices, ct);
```

(Adjust to actual surrounding code in `CreatePendingInvoicesForCapturedOrderAsync`.)

- [ ] **Step 2: Run, expect pass**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~CreatePendingInvoicesForCapturedOrderAsync"
```
Expected: all `CreatePendingInvoicesForCapturedOrderAsync` tests pass — including any pre-existing ones (run them all to catch regressions).

- [ ] **Step 3: Build + full test**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
make test
```
Expected: build passes, full test suite passes (ignoring the documented `AppEnvironmentTests` Docker-only failure).

- [ ] **Step 4: Commit**

```bash
git add DeliverTableServer/Services/InvoiceService.cs
git commit -m "$(cat <<'EOF'
feat(server): gate per-order commission invoice creation behind cutover

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

Continued in [part 3](2026-05-29-monthly-commission-statements-part-3.md) — refund wiring, admin controller, scheduler/Quartz, worker PDF + consumer, final polish.
