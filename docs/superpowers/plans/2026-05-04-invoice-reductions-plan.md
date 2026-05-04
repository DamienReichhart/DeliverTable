# Invoice Reductions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make customer invoices display every applied reduction (`Promotion`, `DiscountCode`, `LoyaltyPoints`) as its own line below the items, with HT/VAT/TTC values that produce a legally correct French invoice with correct totals.

**Architecture:** Extend `InvoiceLine` with a `Kind` discriminator (`Item` | `Discount`). Discount lines store negative HT/VAT/TTC values, so `invoice.Lines.Sum(...)` continues to produce correct totals without special casing. A new pure helper `BuildDiscountLines` allocates each `OrderDiscount.Amount` (TTC) proportionally across the order's distinct VAT rates, emitting one `InvoiceLine` per (source × rate). The PDF renderer adds a **Réductions** section between items and totals.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core (Npgsql), QuestPDF, NUnit 4, NSubstitute, Docker Compose.

**Spec:** [`docs/superpowers/specs/2026-05-04-invoice-reductions-design.md`](../specs/2026-05-04-invoice-reductions-design.md)

---

## File Structure

### Created
- `DeliverTableSharedLibrary/Enums/InvoiceLineKind.cs` — new enum.
- `DeliverTableInfrastructure/Migrations/<timestamp>_AddInvoiceLineKind.cs` (+ `.Designer.cs`) — generated.
- `DeliverTableTests/Infrastructure/Unit/Repositories/OrderRepositoryTests.cs` — new test file.

### Modified
- `DeliverTableInfrastructure/Models/InvoiceLine.cs` — add `Kind` property.
- `DeliverTableInfrastructure/Data/ModelConfiguration/InvoiceLineConfiguration.cs` — configure `Kind` column.
- `DeliverTableInfrastructure/Migrations/DeliverTableContextModelSnapshot.cs` — auto-updated by EF.
- `DeliverTableInfrastructure/Repositories/OrderRepository.cs` — load `Discounts` and `Items.Dish`.
- `DeliverTableSharedLibrary/Dtos/Invoice/InvoiceLineDto.cs` — add `Kind` field with default.
- `DeliverTableServer/Services/InvoiceService.cs` — set `Kind` on item lines, add `BuildDiscountLines`, integrate into `BuildCustomerInvoice`, map `Kind` in DTO.
- `DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs` — new tests.
- `DeliverTableWorker/Services/InvoicePdfRenderer.cs` — split items/Réductions sections.
- `DeliverTableTests/Worker/Unit/Services/InvoicePdfRendererTests.cs` — new smoke test.

---

## Pre-flight

Before starting, ensure the dev stack is running:

```bash
make dev
```

This starts the backend, database, and supporting services. Tasks 1–7 assume the stack is up — `dotnet build`, `dotnet test`, and `dotnet ef migrations add` all run inside the `backend` container.

---

## Task 1: Bug fix — load `Order.Discounts` and `Order.Items.Dish`

**Why:** `OrderRepository.GetByIdWithFullDetailsAsync` is the data source for `InvoiceService.CreatePendingInvoicesForCapturedOrderAsync`. Today it doesn't `Include` either nav, so the discount feature would be invisible even after the rest of the work lands. Also, the existing `BuildCustomerInvoice` already reads `item.Dish.VatRate`, relying on whatever happens to be tracked in the `DbContext`. We make both explicit.

**Files:**
- Create: `DeliverTableTests/Infrastructure/Unit/Repositories/OrderRepositoryTests.cs`
- Modify: `DeliverTableInfrastructure/Repositories/OrderRepository.cs` (the `GetByIdWithFullDetailsAsync` method, currently around line 99)

- [ ] **Step 1: Write the failing test**

Create `DeliverTableTests/Infrastructure/Unit/Repositories/OrderRepositoryTests.cs`:

```csharp
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Server.Fixtures;

namespace DeliverTableTests.Infrastructure.Unit.Repositories;

[TestFixture]
public class OrderRepositoryTests
{
    private TestDatabase _testDb = null!;
    private OrderRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _testDb = new TestDatabase();
        _sut = new OrderRepository(_testDb.Context);
    }

    [TearDown]
    public void TearDown()
    {
        _testDb.Dispose();
    }

    [Test]
    public async Task GetByIdWithFullDetailsAsync_LoadsDiscountsAndItemDish()
    {
        var customer = new User { UserName = "c@example.fr", Email = "c@example.fr", FirstName = "Jean", LastName = "Dupont" };
        var owner = new User { UserName = "o@example.fr", Email = "o@example.fr", FirstName = "Owner", LastName = "X" };
        _testDb.Context.Users.AddRange(customer, owner);
        await _testDb.Context.SaveChangesAsync();

        var restaurant = new Restaurant
        {
            OwnerId = owner.Id,
            Name = "Resto",
            LegalName = "Resto SAS",
            LegalAddress = "1 rue",
            LegalForm = "SAS",
            Siret = "73282932000074",
            IsVatRegistered = true,
        };
        _testDb.Context.Restaurants.Add(restaurant);
        await _testDb.Context.SaveChangesAsync();

        var dish = new Dish { RestaurantId = restaurant.Id, Name = "Plat", Price = 10m, VatRate = VatRate.Intermediate10 };
        _testDb.Context.Dishes.Add(dish);
        await _testDb.Context.SaveChangesAsync();

        var order = new Order
        {
            CustomerId = customer.Id,
            RestaurantId = restaurant.Id,
            OrderType = OrderType.Delivery,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            OriginalAmount = 20m,
            DiscountAmount = 2m,
            TotalAmount = 18m,
            Source = BookingSource.CustomerApp,
            Items = new List<OrderItem>
            {
                new() { DishId = dish.Id, DishName = dish.Name, Quantity = 2, UnitPrice = 10m },
            },
            Discounts = new List<OrderDiscount>
            {
                new() { Source = OrderDiscountSource.Promotion, Description = "Promo X", Amount = 2m },
            },
        };
        _testDb.Context.Orders.Add(order);
        await _testDb.Context.SaveChangesAsync();

        var result = await _sut.GetByIdWithFullDetailsAsync(order.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Discounts, Has.Count.EqualTo(1));
        Assert.That(result.Discounts[0].Description, Is.EqualTo("Promo X"));
        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items[0].Dish, Is.Not.Null);
        Assert.That(result.Items[0].Dish.Id, Is.EqualTo(dish.Id));
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~OrderRepositoryTests.GetByIdWithFullDetailsAsync_LoadsDiscountsAndItemDish"
```

Expected: FAIL. Either `result.Discounts` is empty (count 0, expected 1) or `result.Items[0].Dish` is null.

- [ ] **Step 3: Apply the fix**

In `DeliverTableInfrastructure/Repositories/OrderRepository.cs`, replace the body of `GetByIdWithFullDetailsAsync` with:

```csharp
public async Task<Order?> GetByIdWithFullDetailsAsync(int id, CancellationToken ct = default)
{
    return await _dbContext.Orders
        .Include(o => o.Customer)
        .Include(o => o.Restaurant)
        .Include(o => o.Items)
            .ThenInclude(i => i.Dish)
        .Include(o => o.Payments)
        .Include(o => o.Discounts)
        .FirstOrDefaultAsync(o => o.Id == id, ct);
}
```

- [ ] **Step 4: Run the test to verify it passes**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~OrderRepositoryTests.GetByIdWithFullDetailsAsync_LoadsDiscountsAndItemDish"
```

Expected: PASS.

- [ ] **Step 5: Build and run full test suite**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build
```

Expected: build green; all tests pass (the pre-existing `AppEnvironmentTests.Load_AppliesDefaults_WhenOptionalVarsAreMissing` Docker failure is acceptable per CLAUDE.md).

- [ ] **Step 6: Commit**

```bash
git add DeliverTableInfrastructure/Repositories/OrderRepository.cs \
        DeliverTableTests/Infrastructure/Unit/Repositories/OrderRepositoryTests.cs
git commit -m "fix(server): include Discounts and Items.Dish in GetByIdWithFullDetailsAsync"
```

---

## Task 2: Add `InvoiceLineKind` enum and DTO field

**Why:** Discriminator type used by entity, DTO, and renderer. DTO field has a default so existing call sites continue to compile without changes.

**Files:**
- Create: `DeliverTableSharedLibrary/Enums/InvoiceLineKind.cs`
- Modify: `DeliverTableSharedLibrary/Dtos/Invoice/InvoiceLineDto.cs`

This is data-shape work — no TDD per CLAUDE.md ("Enums, entities/models, DTOs ... create directly").

- [ ] **Step 1: Create the enum**

Create `DeliverTableSharedLibrary/Enums/InvoiceLineKind.cs`:

```csharp
namespace DeliverTableSharedLibrary.Enums;

public enum InvoiceLineKind
{
    Item,
    Discount,
}
```

- [ ] **Step 2: Update the DTO**

Replace the contents of `DeliverTableSharedLibrary/Dtos/Invoice/InvoiceLineDto.cs`:

```csharp
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Invoice;

public record InvoiceLineDto(
    string Description,
    decimal Quantity,
    decimal UnitPriceHt,
    decimal UnitPriceTtc,
    decimal VatRate,
    decimal LineHt,
    decimal LineVat,
    decimal LineTtc,
    InvoiceLineKind Kind = InvoiceLineKind.Item);
```

- [ ] **Step 3: Build to verify nothing breaks**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: green. The new `Kind` parameter has a default, so existing positional calls (e.g. the mapper at `InvoiceService.cs:250`) keep compiling.

- [ ] **Step 4: Commit**

```bash
git add DeliverTableSharedLibrary/Enums/InvoiceLineKind.cs \
        DeliverTableSharedLibrary/Dtos/Invoice/InvoiceLineDto.cs
git commit -m "feat(shared): add InvoiceLineKind enum and DTO field"
```

---

## Task 3: Add `Kind` to `InvoiceLine` entity and EF configuration

**Why:** Persist the discriminator on the existing `InvoiceLines` table.

**Files:**
- Modify: `DeliverTableInfrastructure/Models/InvoiceLine.cs`
- Modify: `DeliverTableInfrastructure/Data/ModelConfiguration/InvoiceLineConfiguration.cs`

No TDD — entity + EF config per CLAUDE.md.

- [ ] **Step 1: Add `Kind` to the entity**

In `DeliverTableInfrastructure/Models/InvoiceLine.cs`, add the using and the property at the end of the class (just after `SortOrder`):

```csharp
using DeliverTableSharedLibrary.Enums;
```

```csharp
public InvoiceLineKind Kind { get; set; } = InvoiceLineKind.Item;
```

- [ ] **Step 2: Configure the column**

Replace the contents of `DeliverTableInfrastructure/Data/ModelConfiguration/InvoiceLineConfiguration.cs`:

```csharp
using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Kind)
            .HasConversion<int>()
            .HasDefaultValue(InvoiceLineKind.Item)
            .IsRequired();
    }
}
```

- [ ] **Step 3: Build to verify**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: green.

- [ ] **Step 4: Commit**

```bash
git add DeliverTableInfrastructure/Models/InvoiceLine.cs \
        DeliverTableInfrastructure/Data/ModelConfiguration/InvoiceLineConfiguration.cs
git commit -m "feat(server): add Kind column to InvoiceLine entity"
```

---

## Task 4: EF migration `AddInvoiceLineKind`

**Why:** Apply the new column to the database schema.

**Files:**
- Create: `DeliverTableInfrastructure/Migrations/<timestamp>_AddInvoiceLineKind.cs` (+ `.Designer.cs`) — generated.
- Modify: `DeliverTableInfrastructure/Migrations/DeliverTableContextModelSnapshot.cs` — auto-updated.

- [ ] **Step 1: Generate the migration**

```bash
docker compose -f docker-dev.yaml exec backend dotnet ef migrations add AddInvoiceLineKind \
    --project /src/DeliverTableInfrastructure \
    --startup-project /src/DeliverTableServer \
    --output-dir Migrations
```

Expected: three new files in `DeliverTableInfrastructure/Migrations/`. The timestamp prefix should be ≥ `20260415062539` (the current latest, `AddDisputes`).

- [ ] **Step 2: Verify the generated migration**

Open the new `<timestamp>_AddInvoiceLineKind.cs` and confirm `Up` contains roughly:

```csharp
migrationBuilder.AddColumn<int>(
    name: "Kind",
    table: "InvoiceLines",
    type: "integer",
    nullable: false,
    defaultValue: 0);
```

And `Down` contains:

```csharp
migrationBuilder.DropColumn(
    name: "Kind",
    table: "InvoiceLines");
```

If the generated default is missing or wrong, edit the file to match exactly the snippet above.

- [ ] **Step 3: Apply the migration**

```bash
make dev-migrate
```

Expected: migration applied without errors.

- [ ] **Step 4: Build to verify**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: green.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableInfrastructure/Migrations/
git commit -m "feat(db): add migration AddInvoiceLineKind"
```

---

## Task 5: `BuildDiscountLines` helper, integration, and tests

**Why:** Translate each `OrderDiscount.Amount` (TTC) into one or more `InvoiceLine` rows (`Kind = Discount`), allocating proportionally across the order's distinct VAT rates and reconciling rounding drift exactly.

**Files:**
- Modify: `DeliverTableServer/Services/InvoiceService.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs`

This is core business logic — strict TDD.

- [ ] **Step 1: Write the failing tests**

In `DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs`, add the following tests inside the existing `[TestFixture] class InvoiceServiceTests` (after the existing test methods). Add this private helper at the bottom of the class first — it captures invoices passed to `CreateBatchAsync` so each test can inspect them:

```csharp
private List<Invoice> ArrangeCapture()
{
    var captured = new List<Invoice>();
    _invoiceRepo
        .CreateBatchAsync(Arg.Any<IEnumerable<Invoice>>(), Arg.Any<CancellationToken>())
        .Returns(ci =>
        {
            int id = 100;
            captured.Clear();
            foreach (var inv in ci.Arg<IEnumerable<Invoice>>())
            {
                inv.Id = id++;
                captured.Add(inv);
            }
            return Task.CompletedTask;
        });
    return captured;
}

private static Order BuildOrder(
    int orderId,
    Restaurant restaurant,
    User customer,
    List<OrderItem> items,
    List<OrderDiscount> discounts)
{
    var totalDiscount = discounts.Sum(d => d.Amount);
    var original = items.Sum(i => i.UnitPrice * i.Quantity);
    return new Order
    {
        Id = orderId,
        CustomerId = customer.Id,
        RestaurantId = restaurant.Id,
        OrderType = OrderType.Delivery,
        Status = OrderStatus.Pending,
        PaymentStatus = PaymentStatus.Pending,
        OriginalAmount = original,
        DiscountAmount = totalDiscount,
        TotalAmount = original - totalDiscount,
        Source = BookingSource.CustomerApp,
        Customer = customer,
        Restaurant = restaurant,
        Items = items,
        Discounts = discounts,
    };
}

private void ArrangeDefaultMocks(int orderId, Order order, int restaurantId)
{
    _orderRepo.GetByIdWithFullDetailsAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
    _numbering
        .IssueNumberAsync(InvoiceIssuerType.Restaurant, restaurantId, Arg.Any<int>(), false, Arg.Any<CancellationToken>())
        .Returns($"R{restaurantId:0000}-2026-000001");
    _numbering
        .IssueNumberAsync(InvoiceIssuerType.Platform, null, Arg.Any<int>(), false, Arg.Any<CancellationToken>())
        .Returns("DT-2026-000123");
}

private static Restaurant Resto(bool vatRegistered = true) => new()
{
    Id = 5,
    Name = "Resto",
    Siret = "73282932000074",
    LegalName = "Resto SAS",
    LegalAddress = "1 rue",
    LegalForm = "SAS",
    IsVatRegistered = vatRegistered,
};

private static User Cust() => new() { Id = 1, Email = "c@example.fr", FirstName = "Jean", LastName = "Dupont" };

private static OrderItem Item(string name, decimal unitPrice, int qty, VatRate rate) => new()
{
    DishId = 10,
    Dish = new Dish { Id = 10, VatRate = rate },
    DishName = name,
    Quantity = qty,
    UnitPrice = unitPrice,
};

private static InvoiceLine[] CustomerInvoiceLines(IEnumerable<Invoice> captured) =>
    captured.Single(i => i.Kind == InvoiceKind.OrderInvoiceToCustomer)
        .Lines.OrderBy(l => l.SortOrder).ToArray();
```

Now add the seven test methods:

```csharp
[Test]
public async Task BuildCustomerInvoice_WithSingleRateDiscount_EmitsOneDiscountLine()
{
    var captured = ArrangeCapture();
    var resto = Resto();
    var order = BuildOrder(
        orderId: 42,
        resto,
        Cust(),
        items: new() { Item("Plat", 50m, 2, VatRate.Normal20) }, // 100€ TTC @ 20%
        discounts: new() { new OrderDiscount { Source = OrderDiscountSource.Promotion, Description = "Promo Midi", Amount = 10m } });
    ArrangeDefaultMocks(42, order, resto.Id);

    var result = await _sut.CreatePendingInvoicesForCapturedOrderAsync(42, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    var lines = CustomerInvoiceLines(captured);
    Assert.That(lines, Has.Length.EqualTo(2));
    Assert.That(lines[0].Kind, Is.EqualTo(InvoiceLineKind.Item));
    Assert.That(lines[1].Kind, Is.EqualTo(InvoiceLineKind.Discount));
    Assert.That(lines[1].Description, Is.EqualTo("Promo Midi"));
    Assert.That(lines[1].LineTtc, Is.EqualTo(-10m));
    Assert.That(lines[1].VatRate, Is.EqualTo(20m));
    Assert.That(lines[1].LineHt + lines[1].LineVat, Is.EqualTo(lines[1].LineTtc)); // exact
    var customerInvoice = captured.Single(i => i.Kind == InvoiceKind.OrderInvoiceToCustomer);
    Assert.That(customerInvoice.TotalTtc, Is.EqualTo(90m));
}

[Test]
public async Task BuildCustomerInvoice_WithMultiRateDiscount_SplitsAcrossRates()
{
    var captured = ArrangeCapture();
    var resto = Resto();
    var order = BuildOrder(
        orderId: 43,
        resto,
        Cust(),
        items: new() { Item("Plat", 60m, 1, VatRate.Normal20), Item("Boisson", 40m, 1, VatRate.Intermediate10) },
        discounts: new() { new OrderDiscount { Source = OrderDiscountSource.DiscountCode, Description = "SUMMER10", Amount = 10m } });
    ArrangeDefaultMocks(43, order, resto.Id);

    var result = await _sut.CreatePendingInvoicesForCapturedOrderAsync(43, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    var discountLines = CustomerInvoiceLines(captured).Where(l => l.Kind == InvoiceLineKind.Discount).ToArray();
    Assert.That(discountLines, Has.Length.EqualTo(2));
    Assert.That(discountLines.Select(l => l.LineTtc), Is.EquivalentTo(new[] { -6m, -4m }));
    Assert.That(discountLines.Sum(l => l.LineTtc), Is.EqualTo(-10m));
    Assert.That(discountLines.All(l => l.Description.StartsWith("SUMMER10")), Is.True);
    Assert.That(discountLines.Any(l => l.Description.Contains("TVA 20")), Is.True);
    Assert.That(discountLines.Any(l => l.Description.Contains("TVA 10")), Is.True);
}

[Test]
public async Task BuildCustomerInvoice_WithThreeDiscountSources_RendersAllLabels()
{
    var captured = ArrangeCapture();
    var resto = Resto();
    var order = BuildOrder(
        orderId: 44,
        resto,
        Cust(),
        items: new() { Item("Plat", 100m, 1, VatRate.Normal20) },
        discounts: new()
        {
            new OrderDiscount { Source = OrderDiscountSource.Promotion, Description = "Promotion: Menu midi", Amount = 5m },
            new OrderDiscount { Source = OrderDiscountSource.DiscountCode, Description = "WELCOME — Bienvenue", Amount = 3m },
            new OrderDiscount { Source = OrderDiscountSource.LoyaltyPoints, Description = "Points fidélité (20 pts)", Amount = 2m },
        });
    ArrangeDefaultMocks(44, order, resto.Id);

    var result = await _sut.CreatePendingInvoicesForCapturedOrderAsync(44, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    var lines = CustomerInvoiceLines(captured);
    Assert.That(lines.Count(l => l.Kind == InvoiceLineKind.Discount), Is.EqualTo(3));
    var descriptions = lines.Where(l => l.Kind == InvoiceLineKind.Discount).Select(l => l.Description).ToArray();
    Assert.That(descriptions, Does.Contain("Promotion: Menu midi"));
    Assert.That(descriptions, Does.Contain("WELCOME — Bienvenue"));
    Assert.That(descriptions, Does.Contain("Points fidélité (20 pts)"));
    var customerInvoice = captured.Single(i => i.Kind == InvoiceKind.OrderInvoiceToCustomer);
    Assert.That(customerInvoice.TotalTtc, Is.EqualTo(90m));
}

[Test]
public async Task BuildCustomerInvoice_WithVatExemptRestaurant_EmitsZeroVatDiscountLine()
{
    var captured = ArrangeCapture();
    var resto = Resto(vatRegistered: false);
    var order = BuildOrder(
        orderId: 45,
        resto,
        Cust(),
        items: new() { Item("Plat", 50m, 2, VatRate.Normal20) }, // VAT rate ignored when restaurant not VAT-registered
        discounts: new() { new OrderDiscount { Source = OrderDiscountSource.Promotion, Description = "Promo", Amount = 10m } });
    ArrangeDefaultMocks(45, order, resto.Id);

    var result = await _sut.CreatePendingInvoicesForCapturedOrderAsync(45, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    var discountLines = CustomerInvoiceLines(captured).Where(l => l.Kind == InvoiceLineKind.Discount).ToArray();
    Assert.That(discountLines, Has.Length.EqualTo(1));
    Assert.That(discountLines[0].VatRate, Is.EqualTo(0m));
    Assert.That(discountLines[0].LineVat, Is.EqualTo(0m));
    Assert.That(discountLines[0].LineHt, Is.EqualTo(-10m));
    Assert.That(discountLines[0].LineTtc, Is.EqualTo(-10m));
}

[Test]
public async Task BuildCustomerInvoice_WithRoundingDrift_ReconcilesToExactDiscountTotal()
{
    var captured = ArrangeCapture();
    var resto = Resto();
    // 10€@20% + 10€@10% = 20€ TTC. Discount 0.03€ splits to 0.015 / 0.015,
    // each rounding (AwayFromZero) to 0.02 → sum 0.04, drift -0.01.
    // Largest abs slice tie → tie-break on rate descending (20% wins) → 0.02 - 0.01 = 0.01.
    var order = BuildOrder(
        orderId: 46,
        resto,
        Cust(),
        items: new() { Item("A", 10m, 1, VatRate.Normal20), Item("B", 10m, 1, VatRate.Intermediate10) },
        discounts: new() { new OrderDiscount { Source = OrderDiscountSource.Promotion, Description = "Tiny", Amount = 0.03m } });
    ArrangeDefaultMocks(46, order, resto.Id);

    var result = await _sut.CreatePendingInvoicesForCapturedOrderAsync(46, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    var discountLines = CustomerInvoiceLines(captured).Where(l => l.Kind == InvoiceLineKind.Discount).ToArray();
    Assert.That(discountLines, Has.Length.EqualTo(2));
    Assert.That(discountLines.Sum(l => l.LineTtc), Is.EqualTo(-0.03m));
    var line20 = discountLines.Single(l => l.VatRate == 20m);
    var line10 = discountLines.Single(l => l.VatRate == 10m);
    Assert.That(line20.LineTtc, Is.EqualTo(-0.01m));
    Assert.That(line10.LineTtc, Is.EqualTo(-0.02m));
}

[Test]
public async Task BuildCustomerInvoice_WithNoDiscounts_EmitsNoDiscountLines()
{
    var captured = ArrangeCapture();
    var resto = Resto();
    var order = BuildOrder(
        orderId: 47,
        resto,
        Cust(),
        items: new() { Item("Plat", 10m, 2, VatRate.Intermediate10) },
        discounts: new());
    ArrangeDefaultMocks(47, order, resto.Id);

    var result = await _sut.CreatePendingInvoicesForCapturedOrderAsync(47, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    var lines = CustomerInvoiceLines(captured);
    Assert.That(lines.All(l => l.Kind == InvoiceLineKind.Item), Is.True);
}

[Test]
public async Task BuildCustomerInvoice_DiscountLinesContinueSortOrderAfterItems()
{
    var captured = ArrangeCapture();
    var resto = Resto();
    var order = BuildOrder(
        orderId: 48,
        resto,
        Cust(),
        items: new() { Item("A", 50m, 1, VatRate.Normal20), Item("B", 50m, 1, VatRate.Normal20) },
        discounts: new() { new OrderDiscount { Source = OrderDiscountSource.Promotion, Description = "P", Amount = 10m } });
    ArrangeDefaultMocks(48, order, resto.Id);

    await _sut.CreatePendingInvoicesForCapturedOrderAsync(48, CancellationToken.None);

    var lines = CustomerInvoiceLines(captured);
    Assert.That(lines.Select(l => l.SortOrder), Is.EqualTo(new[] { 0, 1, 2 }));
    Assert.That(lines[2].Kind, Is.EqualTo(InvoiceLineKind.Discount));
}
```

- [ ] **Step 2: Run the new tests to verify they fail**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~InvoiceServiceTests.BuildCustomerInvoice_With"
```

Expected: 7 failures. Each one fails because no discount lines are emitted (existing `BuildCustomerInvoice` ignores `order.Discounts`).

- [ ] **Step 3: Implement `BuildDiscountLines`**

In `DeliverTableServer/Services/InvoiceService.cs`, add this private helper inside the `InvoiceService` class (place it directly below `BuildCustomerInvoice`):

```csharp
private static List<InvoiceLine> BuildDiscountLines(
    IReadOnlyList<InvoiceLine> itemLines,
    IReadOnlyList<OrderDiscount> discounts,
    int startSortOrder)
{
    var result = new List<InvoiceLine>();
    if (discounts.Count == 0) return result;

    var subtotalByRate = itemLines
        .GroupBy(l => l.VatRate)
        .ToDictionary(g => g.Key, g => g.Sum(l => l.LineTtc));
    var subtotalTtc = subtotalByRate.Values.Sum();
    if (subtotalTtc <= 0m) return result;

    int sort = startSortOrder;
    foreach (var d in discounts)
    {
        var slices = new List<(decimal Rate, decimal Slice)>();
        foreach (var (rate, rateSubtotal) in subtotalByRate)
        {
            if (rateSubtotal <= 0m) continue;
            var share = rateSubtotal / subtotalTtc;
            var slice = Math.Round(d.Amount * share, 2, MidpointRounding.AwayFromZero);
            if (slice > 0m)
                slices.Add((rate, slice));
        }
        if (slices.Count == 0) continue;

        var drift = d.Amount - slices.Sum(s => s.Slice);
        if (drift != 0m)
        {
            var idx = slices
                .Select((s, i) => (s, i))
                .OrderByDescending(t => Math.Abs(t.s.Slice))
                .ThenByDescending(t => t.s.Rate)
                .First().i;
            var (r, sl) = slices[idx];
            slices[idx] = (r, sl + drift);
        }

        var multiRate = slices.Count > 1;
        foreach (var (rate, slice) in slices)
        {
            var lineTtc = -slice;
            var lineHt = Math.Round(lineTtc / (1 + rate / 100m), 2, MidpointRounding.AwayFromZero);
            var lineVat = lineTtc - lineHt;
            var description = multiRate ? $"{d.Description} (TVA {rate:0.#}%)" : d.Description;
            result.Add(new InvoiceLine
            {
                Kind = InvoiceLineKind.Discount,
                Description = description,
                Quantity = 1m,
                UnitPriceTtc = lineTtc,
                UnitPriceHt = lineHt,
                VatRate = rate,
                LineHt = lineHt,
                LineVat = lineVat,
                LineTtc = lineTtc,
                SortOrder = sort++,
            });
        }
    }

    return result;
}
```

- [ ] **Step 4: Integrate into `BuildCustomerInvoice`**

In `DeliverTableServer/Services/InvoiceService.cs`, locate `BuildCustomerInvoice` (around line 449). Find the block that starts with `int sort = 0;` and ends with `return invoice;`. Replace that whole block with the version below — it tags every item line with `Kind = InvoiceLineKind.Item` and then appends discount lines.

```csharp
int sort = 0;
foreach (var item in order.Items)
{
    var rate = restaurant.IsVatRegistered ? item.Dish.VatRate.ToDecimal() : 0m;
    var lineTtc = Math.Round(item.UnitPrice * item.Quantity, 2, MidpointRounding.AwayFromZero);
    var unitHt = Math.Round(item.UnitPrice / (1 + rate / 100m), 2, MidpointRounding.AwayFromZero);
    var lineHt = Math.Round(unitHt * item.Quantity, 2, MidpointRounding.AwayFromZero);
    var lineVat = Math.Round(lineTtc - lineHt, 2, MidpointRounding.AwayFromZero);

    invoice.Lines.Add(new InvoiceLine
    {
        Kind = InvoiceLineKind.Item,
        Description = item.DishName,
        Quantity = item.Quantity,
        UnitPriceTtc = item.UnitPrice,
        UnitPriceHt = unitHt,
        VatRate = rate,
        LineHt = lineHt,
        LineVat = lineVat,
        LineTtc = lineTtc,
        SortOrder = sort++,
    });
}

foreach (var discountLine in BuildDiscountLines(invoice.Lines.ToList(), order.Discounts, sort))
{
    invoice.Lines.Add(discountLine);
    sort++;
}

invoice.TotalTtc = invoice.Lines.Sum(l => l.LineTtc);
invoice.TotalHt = invoice.Lines.Sum(l => l.LineHt);
invoice.TotalVat = invoice.Lines.Sum(l => l.LineVat);

return invoice;
```

- [ ] **Step 5: Update the DTO mapper to pass `Kind`**

In `DeliverTableServer/Services/InvoiceService.cs`, find the `InvoiceLineDto` constructor invocation (currently around line 250 — search for `new InvoiceLineDto(`). Replace that construction with:

```csharp
var lines = invoice.Lines.OrderBy(l => l.SortOrder).Select(l => new InvoiceLineDto(
    l.Description,
    l.Quantity,
    l.UnitPriceHt,
    l.UnitPriceTtc,
    l.VatRate,
    l.LineHt,
    l.LineVat,
    l.LineTtc,
    l.Kind)).ToList();
```

- [ ] **Step 6: Run the new tests to verify they pass**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~InvoiceServiceTests.BuildCustomerInvoice_With"
```

Expected: all 7 PASS.

- [ ] **Step 7: Run the full server unit test suite to catch regressions**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~InvoiceServiceTests"
```

Expected: all `InvoiceServiceTests` pass (including the pre-existing `CreatePendingInvoices_HappyPath_CreatesTwoAndReturnsMessages` — its order has no discounts so behavior is unchanged).

- [ ] **Step 8: Commit**

```bash
git add DeliverTableServer/Services/InvoiceService.cs \
        DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs
git commit -m "feat(server): emit discount lines in customer invoices with proportional VAT"
```

---

## Task 6: PDF renderer Réductions section

**Why:** Render discount lines as a distinct "Réductions" section between items and totals. The totals box already sums `invoice.Lines` so the post-discount HT/TVA/TTC values appear correctly with no further work.

**Files:**
- Modify: `DeliverTableWorker/Services/InvoicePdfRenderer.cs`
- Modify: `DeliverTableTests/Worker/Unit/Services/InvoicePdfRendererTests.cs`

- [ ] **Step 1: Write the failing smoke test**

In `DeliverTableTests/Worker/Unit/Services/InvoicePdfRendererTests.cs`, add at the end of the class:

```csharp
[Test]
public void Render_InvoiceWithDiscountLines_ProducesValidPdf()
{
    var invoice = new Invoice
    {
        Number = "TEST-DISC-000001",
        Kind = InvoiceKind.OrderInvoiceToCustomer,
        OrderId = 99,
        IssuedAt = DateTime.UtcNow,
        Currency = "EUR",
        IssuerLegalSnapshotJson =
            """{"Name":"Resto","LegalForm":"SAS","Siret":"73282932000074","VatNumber":"","Address":"1 rue"}""",
        RecipientSnapshotJson =
            """{"Name":"Client","LegalForm":"","Siret":"","VatNumber":"","Address":""}""",
        Lines = new List<InvoiceLine>
        {
            new() { Kind = InvoiceLineKind.Item, Description = "Plat", Quantity = 1m,
                    UnitPriceHt = 50m, UnitPriceTtc = 60m, VatRate = 20m,
                    LineHt = 50m, LineVat = 10m, LineTtc = 60m, SortOrder = 0 },
            new() { Kind = InvoiceLineKind.Item, Description = "Boisson", Quantity = 1m,
                    UnitPriceHt = 36.36m, UnitPriceTtc = 40m, VatRate = 10m,
                    LineHt = 36.36m, LineVat = 3.64m, LineTtc = 40m, SortOrder = 1 },
            new() { Kind = InvoiceLineKind.Discount, Description = "SUMMER10 (TVA 20%)", Quantity = 1m,
                    UnitPriceHt = -5m, UnitPriceTtc = -6m, VatRate = 20m,
                    LineHt = -5m, LineVat = -1m, LineTtc = -6m, SortOrder = 2 },
            new() { Kind = InvoiceLineKind.Discount, Description = "SUMMER10 (TVA 10%)", Quantity = 1m,
                    UnitPriceHt = -3.64m, UnitPriceTtc = -4m, VatRate = 10m,
                    LineHt = -3.64m, LineVat = -0.36m, LineTtc = -4m, SortOrder = 3 },
        },
    };
    invoice.TotalHt = invoice.Lines.Sum(l => l.LineHt);
    invoice.TotalVat = invoice.Lines.Sum(l => l.LineVat);
    invoice.TotalTtc = invoice.Lines.Sum(l => l.LineTtc);

    var pdf = new InvoicePdfRenderer().Render(invoice);

    Assert.That(pdf, Is.Not.Null);
    Assert.That(pdf.Length, Is.GreaterThan(1000));
    Assert.That(pdf[0], Is.EqualTo((byte)'%'));
    Assert.That(pdf[1], Is.EqualTo((byte)'P'));
    Assert.That(pdf[2], Is.EqualTo((byte)'D'));
    Assert.That(pdf[3], Is.EqualTo((byte)'F'));
}
```

- [ ] **Step 2: Run the smoke test to verify it passes against current renderer**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~InvoicePdfRendererTests.Render_InvoiceWithDiscountLines_ProducesValidPdf"
```

Expected: this is a smoke test; it likely **passes** today even before changes (the current renderer iterates all `invoice.Lines` and would render discount lines mixed in with items). That's fine — the test guards against future regressions. If it fails (e.g. a null-ref because the renderer can't handle negative numbers), that's the failure to fix.

If the test passes here, that's the "before" baseline. The renderer changes below won't break it.

- [ ] **Step 3: Update the renderer to render the Réductions section separately**

Open `DeliverTableWorker/Services/InvoicePdfRenderer.cs`. Find the existing items table loop (the `foreach (var line in invoice.Lines.OrderBy(l => l.SortOrder))` inside `col.Item().PaddingTop(15).Table(table => { ... })`). Replace that whole `foreach` with a filter to item-kind lines only. Then immediately after the items `Table(...)` call, add a conditional Réductions section.

Concretely, modify the items-table block as follows:

```csharp
col.Item().PaddingTop(15).Table(table =>
{
    table.ColumnsDefinition(cd =>
    {
        cd.RelativeColumn(4);
        cd.RelativeColumn(1);
        cd.RelativeColumn(2);
        if (!isVatExempt)
            cd.RelativeColumn(1);
        cd.RelativeColumn(2);
        cd.RelativeColumn(2);
    });
    table.Header(h =>
    {
        h.Cell().Text("Description").Bold();
        h.Cell().AlignRight().Text("Qté").Bold();
        h.Cell().AlignRight().Text("PU HT").Bold();
        if (!isVatExempt)
            h.Cell().AlignRight().Text("TVA %").Bold();
        h.Cell().AlignRight().Text("Total HT").Bold();
        h.Cell().AlignRight().Text("Total TTC").Bold();
    });
    foreach (var line in invoice.Lines
        .Where(l => l.Kind == InvoiceLineKind.Item)
        .OrderBy(l => l.SortOrder))
    {
        table.Cell().Text(line.Description);
        table.Cell().AlignRight().Text(line.Quantity.ToString("0.###"));
        table.Cell().AlignRight().Text(line.UnitPriceHt.ToString("0.00 €"));
        if (!isVatExempt)
            table.Cell().AlignRight().Text($"{line.VatRate:0.#} %");
        table.Cell().AlignRight().Text(line.LineHt.ToString("0.00 €"));
        table.Cell().AlignRight().Text(line.LineTtc.ToString("0.00 €"));
    }
});

var discountLines = invoice.Lines
    .Where(l => l.Kind == InvoiceLineKind.Discount)
    .OrderBy(l => l.SortOrder)
    .ToList();
if (discountLines.Count > 0)
{
    col.Item().PaddingTop(10).Text("Réductions").Bold();
    col.Item().Table(table =>
    {
        table.ColumnsDefinition(cd =>
        {
            cd.RelativeColumn(4);
            cd.RelativeColumn(1);
            cd.RelativeColumn(2);
            if (!isVatExempt)
                cd.RelativeColumn(1);
            cd.RelativeColumn(2);
            cd.RelativeColumn(2);
        });
        foreach (var line in discountLines)
        {
            table.Cell().Text(line.Description);
            table.Cell().AlignRight().Text(line.Quantity.ToString("0.###"));
            table.Cell().AlignRight().Text(line.UnitPriceHt.ToString("0.00 €"));
            if (!isVatExempt)
                table.Cell().AlignRight().Text($"{line.VatRate:0.#} %");
            table.Cell().AlignRight().Text(line.LineHt.ToString("0.00 €"));
            table.Cell().AlignRight().Text(line.LineTtc.ToString("0.00 €"));
        }
    });
}
```

Add the `using` if not already present:

```csharp
using DeliverTableSharedLibrary.Enums;
```

(`InvoiceLineKind` lives in `DeliverTableSharedLibrary.Enums`, which is most likely already imported via the existing `using DeliverTableSharedLibrary.Enums;` directive — check and only add if missing.)

- [ ] **Step 4: Run the smoke tests to verify they still pass**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~InvoicePdfRendererTests"
```

Expected: all tests in `InvoicePdfRendererTests` pass (existing 3 + new 1 = 4).

- [ ] **Step 5: Run the full test suite**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build
```

Expected: all tests pass (the pre-existing `AppEnvironmentTests.Load_AppliesDefaults_WhenOptionalVarsAreMissing` Docker failure is acceptable per CLAUDE.md).

- [ ] **Step 6: Commit**

```bash
git add DeliverTableWorker/Services/InvoicePdfRenderer.cs \
        DeliverTableTests/Worker/Unit/Services/InvoicePdfRendererTests.cs
git commit -m "feat(worker): render Réductions section on customer invoice PDFs"
```

---

## Task 7: Format gate

**Why:** CLAUDE.md mandates `make format-check` passes before final commit.

- [ ] **Step 1: Run the format check**

```bash
make format-check
```

- [ ] **Step 2: If format-check failed, apply fixes**

```bash
make format-fix
```

If `make format-check` already passed, **skip steps 3 and 4** — there's nothing to commit.

- [ ] **Step 3: Build and run full test suite again**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build
```

Expected: green.

- [ ] **Step 4: Commit formatting fixes**

```bash
git add -A
git commit -m "style: apply formatting fixes"
```

---

## Final Verification

- [ ] **All commits land on the current branch:**

```bash
git log --oneline -10
```

You should see (newest first):

```
<hash> style: apply formatting fixes                                            (skipped if not needed)
<hash> feat(worker): render Réductions section on customer invoice PDFs
<hash> feat(server): emit discount lines in customer invoices with proportional VAT
<hash> feat(db): add migration AddInvoiceLineKind
<hash> feat(server): add Kind column to InvoiceLine entity
<hash> feat(shared): add InvoiceLineKind enum and DTO field
<hash> fix(server): include Discounts and Items.Dish in GetByIdWithFullDetailsAsync
```

- [ ] **CI gate:**

```bash
make ci
```

Expected: full gate passes.
