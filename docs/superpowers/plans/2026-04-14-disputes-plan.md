# Disputes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Track Stripe disputes (chargebacks) end-to-end: persist via webhook, reverse the restaurant's credit immediately on open, restore on won, notify admins (in-app + email) and restaurant owners, expose dedicated admin/restaurant UIs, and block refunds while a dispute is open.

**Architecture:** `charge.dispute.*` webhooks dispatch through the Spec 1 `PaymentService.HandleStripeEventAsync` into a new `IDisputeService`. Dispute rows are upserted by `StripeDisputeId`. On open, a `RestaurantTransaction` with new `DisputeReversal` type debits the restaurant balance; on won, a `DisputeRestored` transaction restores it. Notifications are raised in-app to all admins + the restaurant owner, and two emails are enqueued per event. Platform evidence submission stays out-of-band in the Stripe dashboard.

**Tech Stack:** .NET 10, EF Core + Npgsql, Stripe.net (already in Infrastructure), NUnit 4 + NSubstitute for tests, MailKit + Razor templates via existing `DeliverTableWorker`.

**Spec:** `docs/superpowers/specs/2026-04-14-disputes-design.md`

**Dependencies**:
- Spec 1 (Stripe Payments Core): `Payment`, `StripeWebhookController`, `PaymentService.HandleStripeEventAsync`, `ProcessedStripeEvent`, `IStripeGateway`, `AdminRefundAsync`, `RestaurantTransaction`, `EmailJobMessage`, `IEmailJobService`/`IEmailJobRepository`, `IMessagePublisher`, `AdminNotificationService`.
- Spec 2 (Invoices): **optional**. Lost disputes generate credit notes only when `IInvoiceService` is registered in DI. If Spec 2 is unmerged, Lost handling simply logs + skips credit-note generation.

**Conventions (CLAUDE.md)**:
- All dotnet commands run inside the dev stack: `docker compose -f docker-dev.yaml exec backend dotnet ...`.
- Run a specific test: `docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~ClassName"`.
- Full suite: `make test`.
- Format: `make format-check` / `make format-fix`.
- TDD is mandatory for services and controllers. Entities, enums, DTOs, mappers, EF configs, migrations, DI registration don't require tests.
- Never include `Co-Authored-By` in commits. Don't add PBI/Task refs unless explicitly given.
- French for all `ErrorMessages` values and user-visible strings. **No trailing periods** on error messages (established codebase convention — spec §9 has them; drop them in actual implementation).
- `nameof(UserRole.X)` not hardcoded strings.
- EF: annotations on entities, fluent config only for things annotations can't express.
- Enums: implicit ordinals for contiguous zero-based; explicit values only when gapping/reserving (e.g. `Dispute = 100` is a reservation gap for future enum values).
- All tests live in `DeliverTableTests/` (the separate scheduler/worker test projects were consolidated away).
- For webhook dispatch, reuse Spec 1's transaction pattern: register `ProcessedStripeEvent` + dispatch inside a single `BeginTransactionAsync`; `deferredPublishes` list collects RabbitMQ publications that fire AFTER `CommitAsync` (Spec 1 Fix 1 / Fix 2 lessons).

---

## File Structure

### Shared (enums, DTOs, routes)
- Create: `DeliverTableSharedLibrary/Enums/DisputeState.cs`
- Modify: `DeliverTableSharedLibrary/Enums/NotificationType.cs` (add `Dispute = 100`)
- Modify: `DeliverTableSharedLibrary/Enums/TransactionType.cs` (add `DisputeReversal = 100`, `DisputeRestored = 101`)
- Modify: `DeliverTableSharedLibrary/Constants/ApiRoutes.cs` (new `Dispute` class + admin routes)
- Create: `DeliverTableSharedLibrary/Dtos/Dispute/DisputeRowDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Dispute/AdminDisputeRowDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Dispute/AdminDisputeDetailDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Dispute/DisputeAdminFilter.cs`

### Data model (Infrastructure)
- Create: `DeliverTableInfrastructure/Models/Dispute.cs`
- Create: `DeliverTableInfrastructure/Data/ModelConfiguration/DisputeConfiguration.cs`
- Modify: `DeliverTableInfrastructure/Data/Contexts/DeliverTableContext.Order.cs` (or matching partial — add DbSet)
- Create: `DeliverTableInfrastructure/Migrations/{timestamp}_AddDisputes.cs`

### Infrastructure repositories + shared lifecycle
- Create: `DeliverTableInfrastructure/Repositories/Interfaces/IDisputeRepository.cs`
- Create: `DeliverTableInfrastructure/Repositories/DisputeRepository.cs`

### Server services + controllers + config
- Modify: `DeliverTableServer/Configuration/AppEnvironment.cs` (add `AdminDisputeEmail` required string)
- Modify: `DeliverTableServer/Constants/ErrorMessages.cs` (4 new French messages)
- Create: `DeliverTableServer/Services/Interfaces/IDisputeService.cs`
- Create: `DeliverTableServer/Services/DisputeService.cs`
- Create: `DeliverTableServer/Controllers/DisputeController.cs`
- Create: `DeliverTableServer/Controllers/AdminDisputeController.cs`
- Modify: `DeliverTableServer/Services/PaymentService.cs` (extend webhook dispatch + add refund guard)
- Modify: `DeliverTableServer/Services/AdminNotificationService.cs` (add `RaiseForAllAdminsAsync` if missing)
- Modify: `DeliverTableServer/Extensions/ServiceCollectionExtensions.cs` (DI registration)

### Worker (email templates)
- Modify: `DeliverTableWorker/Services/EmailJobType.cs` (or wherever the enum lives — add 6 dispute template values)
- Create: `DeliverTableWorker/Templates/Email/DisputeOpenedAdmin.cshtml`
- Create: `DeliverTableWorker/Templates/Email/DisputeOpenedRestaurant.cshtml`
- Create: `DeliverTableWorker/Templates/Email/DisputeWonAdmin.cshtml`
- Create: `DeliverTableWorker/Templates/Email/DisputeWonRestaurant.cshtml`
- Create: `DeliverTableWorker/Templates/Email/DisputeLostAdmin.cshtml`
- Create: `DeliverTableWorker/Templates/Email/DisputeLostRestaurant.cshtml`
- Modify: `DeliverTableWorker/Services/RazorEmailTemplateRenderer.cs` (register new templates if the registry is explicit)

### Client
- Create: `DeliverTableClient/Services/Dispute/IDisputeApiClient.cs`
- Create: `DeliverTableClient/Services/Dispute/DisputeApiClient.cs`
- Modify: `DeliverTableClient/Extensions/ApiClientServiceCollectionExtensions.cs` (register `IDisputeApiClient`)
- Modify: the existing restaurant dashboard page (`Pages/Restaurant/Account/RestaurantAccount.razor` per Spec 2 Task 23 — add "Litiges" section)
- Create: `DeliverTableClient/Pages/Admin/Disputes/AdminDisputes/AdminDisputes.razor`
- Create: `DeliverTableClient/Pages/Admin/Disputes/AdminDisputes/AdminDisputes.razor.scss`
- Create: `DeliverTableClient/Pages/Admin/Disputes/AdminDisputeDetail/AdminDisputeDetail.razor`
- Create: `DeliverTableClient/Pages/Admin/Disputes/AdminDisputeDetail/AdminDisputeDetail.razor.scss`
- Modify: an admin-visible order detail view (add "Litige en cours" banner)

### Tests (in `DeliverTableTests/`)
- Create: `DeliverTableTests/Server/Unit/Services/DisputeServiceTests.cs`
- Create: `DeliverTableTests/Server/Unit/Controllers/DisputeControllerTests.cs`
- Create: `DeliverTableTests/Server/Unit/Controllers/AdminDisputeControllerTests.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs` (webhook dispatch + refund guard)
- Modify: `DeliverTableTests/Server/Unit/Controllers/StripeWebhookControllerTests.cs` (dispute event dispatch — may not need changes since webhook handler is already type-agnostic)
- Modify: `DeliverTableTests/Server/Unit/Configuration/AppEnvironmentTests.cs` (new required var)
- Modify: `DeliverTableTests/Global/Helpers/AppEnvironmentTestHelper.cs` (same)

### Docs + env
- Modify: `.env.example` (add `ADMIN_DISPUTE_EMAIL`)
- Modify: `docker-dev.yaml` + `docker-prod.yaml` (pass `ADMIN_DISPUTE_EMAIL` into backend)
- Modify: `docs/db/er-diagram.md`
- Modify: `docs/db/data-dictionary.md`

---

## Task 1: Add `DisputeState` enum and extend `NotificationType` and `TransactionType`

**Files:**
- Create: `DeliverTableSharedLibrary/Enums/DisputeState.cs`
- Modify: `DeliverTableSharedLibrary/Enums/NotificationType.cs`
- Modify: `DeliverTableSharedLibrary/Enums/TransactionType.cs`

No tests (enum additions).

- [ ] **Step 1: Create `DisputeState.cs`**

Implicit ordinals for contiguous sequence (codebase convention):

```csharp
namespace DeliverTableSharedLibrary.Enums;

public enum DisputeState
{
    Open,
    Won,
    Lost,
}
```

- [ ] **Step 2: Add `Dispute = 100` to `NotificationType`**

Open `NotificationType.cs`, add the new member with an explicit value (gap for future reserved values, matches the `AwaitingPayment = 100` pattern from Spec 1):

```csharp
Dispute = 100,
```

Preserve existing values.

- [ ] **Step 3: Add `DisputeReversal = 100, DisputeRestored = 101` to `TransactionType`**

```csharp
DisputeReversal = 100,
DisputeRestored = 101,
```

- [ ] **Step 4: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 5: Commit**

```bash
git add DeliverTableSharedLibrary/Enums/DisputeState.cs \
        DeliverTableSharedLibrary/Enums/NotificationType.cs \
        DeliverTableSharedLibrary/Enums/TransactionType.cs
git commit -m "feat(shared): add DisputeState enum and extend NotificationType and TransactionType"
```

---

## Task 2: Add `Dispute` entity with EF config

**Files:**
- Create: `DeliverTableInfrastructure/Models/Dispute.cs`
- Create: `DeliverTableInfrastructure/Data/ModelConfiguration/DisputeConfiguration.cs`
- Modify: the existing DbContext partial that holds Order/Payment/Invoice DbSets (inspect `DeliverTableInfrastructure/Data/Contexts/`)

No tests. EF convention: annotations on entity; fluent config only for indexes, relationships, conversions.

- [ ] **Step 1: Create `Dispute.cs`**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Models;

public class Dispute
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string StripeDisputeId { get; set; } = string.Empty;

    public int PaymentId { get; set; }

    [ForeignKey(nameof(PaymentId))]
    public Payment Payment { get; set; } = null!;

    public int OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;

    public int RestaurantId { get; set; }

    [ForeignKey(nameof(RestaurantId))]
    public Restaurant Restaurant { get; set; } = null!;

    [Column(TypeName = "decimal(9, 2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    [Required]
    [MaxLength(60)]
    public string ReasonCode { get; set; } = string.Empty;

    public DisputeState State { get; set; } = DisputeState.Open;

    public DateTime? DueBy { get; set; }

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }

    [MaxLength(8000)]
    public string StripePayload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Create `DisputeConfiguration.cs`**

```csharp
using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => d.StripeDisputeId).IsUnique();
        builder.HasIndex(d => d.PaymentId);
        builder.HasIndex(d => d.OrderId);
        builder.HasIndex(d => d.RestaurantId);
        builder.HasIndex(d => new { d.RestaurantId, d.State });

        builder.Property(d => d.State).HasConversion<string>();

        builder.HasOne(d => d.Payment)
               .WithMany()
               .HasForeignKey(d => d.PaymentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Order)
               .WithMany()
               .HasForeignKey(d => d.OrderId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Restaurant)
               .WithMany()
               .HasForeignKey(d => d.RestaurantId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 3: Register DbSet**

Inspect `DeliverTableInfrastructure/Data/Contexts/`. Add to the partial that holds `Payments`/`Invoices` (likely `DeliverTableContext.Order.cs`):

```csharp
public DbSet<Dispute> Disputes => Set<Dispute>();
```

EF configs auto-picked-up via `ApplyConfigurationsFromAssembly` — no manual registration.

- [ ] **Step 4: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 5: Commit**

```bash
git add DeliverTableInfrastructure/Models/Dispute.cs \
        DeliverTableInfrastructure/Data/ModelConfiguration/DisputeConfiguration.cs \
        DeliverTableInfrastructure/Data/Contexts/
git commit -m "feat(server): add Dispute entity with EF config"
```

---

## Task 3: Generate migration `AddDisputes`

**File:**
- Create: `DeliverTableInfrastructure/Migrations/{timestamp}_AddDisputes.cs` (generated)

- [ ] **Step 1: Generate migration**

```bash
docker compose -f docker-dev.yaml exec backend dotnet ef migrations add AddDisputes \
    --project /src/DeliverTableInfrastructure \
    --startup-project /src/DeliverTableServer
```

- [ ] **Step 2: Review generated migration**

Open the new file. Verify:
- Creates `Disputes` table with all columns + 4 indexes (unique on `StripeDisputeId`, non-unique on `PaymentId`/`OrderId`/`RestaurantId`, composite on `(RestaurantId, State)`).
- FKs with `ON DELETE RESTRICT` (Payment, Order, Restaurant).
- `State` column stored as `text` (per `.HasConversion<string>()`).
- No changes to other tables (enum additions are int-compatible with existing columns).

No hand-edits expected.

- [ ] **Step 3: Apply migration**

```bash
make dev-migrate
```

- [ ] **Step 4: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 5: Commit**

```bash
git add DeliverTableInfrastructure/Migrations/
git commit -m "feat(db): add migration AddDisputes"
```

---

## Task 4: Update DB docs

**Files:**
- Modify: `docs/db/er-diagram.md`
- Modify: `docs/db/data-dictionary.md`

- [ ] **Step 1: Update `er-diagram.md`**

Add `DISPUTE` entity to the Mermaid diagram with all fields, and these relationships:
- `PAYMENT ||--o{ DISPUTE`
- `ORDER ||--o{ DISPUTE`
- `RESTAURANT ||--o{ DISPUTE`

Add a short design-notes paragraph: Dispute rows mirror Stripe chargebacks, linked to the Payment/Order/Restaurant triad; lifecycle is `Open → Won|Lost`; triggers `DisputeReversal`/`DisputeRestored` RestaurantTransaction rows.

Update Enumerations list in the design notes to reflect `DisputeState` and the new `TransactionType` / `NotificationType` members.

- [ ] **Step 2: Update `data-dictionary.md`**

- Add a numbered `DISPUTE` section (insert after the existing PAYMENT/REFUND group).
- Update the `TRANSACTION_TYPE` enum entry with `DisputeReversal` (100) and `DisputeRestored` (101).
- Update the `NOTIFICATION_TYPE` enum with `Dispute` (100).
- Add `DisputeState` enum.

Renumber downstream sections if the format requires it.

- [ ] **Step 3: Commit**

```bash
git add docs/db/er-diagram.md docs/db/data-dictionary.md
git commit -m "docs(db): update ER diagram and data dictionary for disputes"
```

---

## Task 5: Add `ADMIN_DISPUTE_EMAIL` env var + French error messages

**Files:**
- Modify: `.env.example`
- Modify: `docker-dev.yaml` (backend service env block)
- Modify: `docker-prod.yaml` (backend service env block)
- Modify: `DeliverTableServer/Configuration/AppEnvironment.cs`
- Modify: `DeliverTableServer/Constants/ErrorMessages.cs`
- Modify: `DeliverTableTests/Server/Unit/Configuration/AppEnvironmentTests.cs`
- Modify: `DeliverTableTests/Global/Helpers/AppEnvironmentTestHelper.cs`

**Critical**: lesson from Spec 1/2 Task 6 — adding a required env var without updating test helpers breaks every test that calls `AppEnvironment.Load()`. Update both places.

- [ ] **Step 1: `.env.example` append**

```bash

# ─────────────────────────────────────────────────
# Dispute alerts
# ─────────────────────────────────────────────────
ADMIN_DISPUTE_EMAIL=disputes@delivertable.example
```

Also set a real value in local `.env` (e.g. your dev email).

- [ ] **Step 2: Add to `docker-dev.yaml` and `docker-prod.yaml`**

In each file's `backend:` service `environment:` block, add:

```yaml
      ADMIN_DISPUTE_EMAIL: ${ADMIN_DISPUTE_EMAIL}
```

- [ ] **Step 3: Extend `AppEnvironment.cs`**

Add required property:

```csharp
public required string AdminDisputeEmail { get; init; }
```

In `Load()`:

```csharp
string adminDisputeEmail = RequireVar("ADMIN_DISPUTE_EMAIL", errors);
```

Pass to the constructor / object initializer.

- [ ] **Step 4: Append French error messages to `ErrorMessages.cs`**

No trailing periods per codebase convention (discovered in Spec 1 Task 6 fix):

```csharp
public const string DisputeNotFound            = "Litige introuvable";
public const string DisputeAccessDenied        = "Vous n'êtes pas autorisé à consulter ce litige";
public const string RefundBlockedByOpenDispute = "Impossible de rembourser : un litige est ouvert sur cette commande";
public const string DisputePaymentNotFound     = "Aucun paiement correspondant à ce litige n'a été trouvé";
```

- [ ] **Step 5: Update test helpers**

`AppEnvironmentTestHelper.cs`: add `ADMIN_DISPUTE_EMAIL` = `"disputes@test.local"` to `RequiredVars` (or `SetupEnvironment()` — match existing pattern).

`AppEnvironmentTests.cs`:
- Add `ADMIN_DISPUTE_EMAIL` to the `AllRequiredVars` collection.
- Add a `TestCase` row for the missing-var test.
- Add an assertion in the happy-path `Load_ReadsAllRequiredVariables`-style test.
- Add a `SetEnvironmentVariable` call in `SetAllRequired()`.

- [ ] **Step 6: Restart backend**

```bash
docker compose -f docker-dev.yaml restart backend
docker compose -f docker-dev.yaml logs backend --tail=20
```

Must start cleanly.

- [ ] **Step 7: Build + test**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
make test
```

All existing tests must still pass.

- [ ] **Step 8: Commit**

```bash
git add .env.example \
        docker-dev.yaml docker-prod.yaml \
        DeliverTableServer/Configuration/AppEnvironment.cs \
        DeliverTableServer/Constants/ErrorMessages.cs \
        DeliverTableTests/Server/Unit/Configuration/AppEnvironmentTests.cs \
        DeliverTableTests/Global/Helpers/AppEnvironmentTestHelper.cs
git commit -m "feat(server): add ADMIN_DISPUTE_EMAIL env var and French dispute error messages"
```

---

## Task 6: Add dispute DTOs and routes

**Files:**
- Modify: `DeliverTableSharedLibrary/Constants/ApiRoutes.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Dispute/DisputeRowDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Dispute/AdminDisputeRowDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Dispute/AdminDisputeDetailDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Dispute/DisputeAdminFilter.cs`

No tests.

- [ ] **Step 1: Add routes**

In `ApiRoutes.cs`, add a top-level nested class:

```csharp
public static class Dispute
{
    public const string Base = "api/v1/dispute";
    public const string RestaurantListRoute = "restaurant/{id:int}";
}
```

Inside the `Admin` nested class:

```csharp
public const string DisputesRoute = "disputes";
public const string DisputeByIdRoute = "disputes/{id:int}";
```

- [ ] **Step 2: DTOs**

`DisputeRowDto.cs`:

```csharp
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Dispute;

public record DisputeRowDto(
    int Id,
    string StripeDisputeId,
    int OrderId,
    decimal Amount,
    string Currency,
    string ReasonCode,
    DisputeState State,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    DateTime? DueBy);
```

`AdminDisputeRowDto.cs`:

```csharp
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Dispute;

public record AdminDisputeRowDto(
    int Id,
    string StripeDisputeId,
    int OrderId,
    int RestaurantId,
    string RestaurantName,
    string CustomerEmail,
    decimal Amount,
    string Currency,
    string ReasonCode,
    DisputeState State,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    DateTime? DueBy);
```

`AdminDisputeDetailDto.cs`:

```csharp
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;

namespace DeliverTableSharedLibrary.Dtos.Dispute;

public record AdminDisputeDetailDto(
    AdminDisputeRowDto Header,
    string StripeDashboardUrl,
    int PaymentId,
    string StripeChargeId,
    decimal PaymentAmount,
    List<RestaurantTransactionDto> LinkedTransactions);
```

(Inspect the actual namespace of `RestaurantTransactionDto`; adjust the using.)

`DisputeAdminFilter.cs`:

```csharp
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Dispute;

public class DisputeAdminFilter
{
    public DisputeState? State { get; set; }
    public int? RestaurantId { get; set; }
    public int? Year { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

- [ ] **Step 3: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 4: Commit**

```bash
git add DeliverTableSharedLibrary/
git commit -m "feat(shared): add dispute DTOs and routes"
```

---

## Task 7: Add `IDisputeRepository`

**Files:**
- Create: `DeliverTableInfrastructure/Repositories/Interfaces/IDisputeRepository.cs`
- Create: `DeliverTableInfrastructure/Repositories/DisputeRepository.cs`

No tests (repository).

- [ ] **Step 1: Interface**

```csharp
using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface IDisputeRepository
{
    Task<Dispute> CreateAsync(Dispute dispute, CancellationToken ct = default);
    Task UpdateAsync(Dispute dispute, CancellationToken ct = default);
    Task<Dispute?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Dispute?> GetByStripeDisputeIdAsync(string stripeDisputeId, CancellationToken ct = default);
    Task<bool> HasOpenForOrderAsync(int orderId, CancellationToken ct = default);
    Task<(List<Dispute> Items, int Total)> ListForRestaurantAsync(int restaurantId, int page, int pageSize, CancellationToken ct = default);
    Task<(List<Dispute> Items, int Total)> AdminListAsync(DisputeState? state, int? restaurantId, int? year, int page, int pageSize, CancellationToken ct = default);
}
```

- [ ] **Step 2: Implementation**

```csharp
using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class DisputeRepository(DeliverTableContext dbContext) : IDisputeRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<Dispute> CreateAsync(Dispute dispute, CancellationToken ct = default)
    {
        _dbContext.Disputes.Add(dispute);
        await _dbContext.SaveChangesAsync(ct);
        return dispute;
    }

    public async Task UpdateAsync(Dispute dispute, CancellationToken ct = default)
    {
        dispute.UpdatedAt = DateTime.UtcNow;
        _dbContext.Disputes.Update(dispute);
        await _dbContext.SaveChangesAsync(ct);
    }

    public Task<Dispute?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _dbContext.Disputes
            .Include(d => d.Restaurant)
            .Include(d => d.Order)
            .Include(d => d.Payment)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<Dispute?> GetByStripeDisputeIdAsync(string stripeDisputeId, CancellationToken ct = default) =>
        _dbContext.Disputes.FirstOrDefaultAsync(d => d.StripeDisputeId == stripeDisputeId, ct);

    public Task<bool> HasOpenForOrderAsync(int orderId, CancellationToken ct = default) =>
        _dbContext.Disputes.AnyAsync(d => d.OrderId == orderId && d.State == DisputeState.Open, ct);

    public async Task<(List<Dispute> Items, int Total)> ListForRestaurantAsync(int restaurantId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.Disputes.Where(d => d.RestaurantId == restaurantId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(d => d.OpenedAt)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .ToListAsync(ct);
        return (items, total);
    }

    public async Task<(List<Dispute> Items, int Total)> AdminListAsync(DisputeState? state, int? restaurantId, int? year, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.Disputes
            .Include(d => d.Restaurant)
            .Include(d => d.Order).ThenInclude(o => o.Customer)
            .AsQueryable();
        if (state.HasValue) query = query.Where(d => d.State == state.Value);
        if (restaurantId.HasValue) query = query.Where(d => d.RestaurantId == restaurantId.Value);
        if (year.HasValue) query = query.Where(d => d.OpenedAt.Year == year.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(d => d.OpenedAt)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .ToListAsync(ct);
        return (items, total);
    }
}
```

- [ ] **Step 3: Build + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
git add DeliverTableInfrastructure/Repositories/Interfaces/IDisputeRepository.cs \
        DeliverTableInfrastructure/Repositories/DisputeRepository.cs
git commit -m "feat(infra): add IDisputeRepository"
```

---

## Task 8: `IDisputeService` with `HandleCreatedAsync/HandleUpdatedAsync/HandleClosedAsync` and `HasOpenDisputeForOrderAsync`

**Files:**
- Create: `DeliverTableServer/Services/Interfaces/IDisputeService.cs`
- Create: `DeliverTableServer/Services/DisputeService.cs`
- Create: `DeliverTableTests/Server/Unit/Services/DisputeServiceTests.cs`

TDD. This is the core orchestration. Subsequent tasks wire it into the webhook dispatch and refund guard.

**Key design notes**:
- Follow the **deferred-publish** pattern from Spec 1 Fix 1: service methods return `ServiceResult<List<Func<Task>>>` or `ServiceResult` with a separate `out` list, OR accept the `deferredPublishes` list as an argument. The simpler pattern (used in this plan): service methods return `ServiceResult` and directly call `IAdminNotificationService` + `IMessagePublisher`; the CALLER (PaymentService) wraps the whole handler in a transaction and adds the publish calls to its `deferredPublishes` list. So `DisputeService` exposes the service result plus the notifications/emails are sent via method parameter injection pattern — see the impl below.
- Actual cleanest approach: `DisputeService` takes `INotificationService` + `IMessagePublisher` in its constructor and calls them inline. The caller (`PaymentService.HandleStripeEventAsync`) wraps DB work in a transaction; notifications and email publishes happen inline too but any that throw will roll back. For RabbitMQ publishes (non-transactional), the caller defers them. So `DisputeService` needs a way to collect deferred publishes.
- Follow the pattern: pass `List<Func<Task>> deferredPublishes` as an argument to handler methods so publishes can be captured and fired after commit.

- [ ] **Step 1: Interface**

```csharp
using DeliverTableInfrastructure.Models;
using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dispute;

namespace DeliverTableServer.Services.Interfaces;

public interface IDisputeService
{
    Task<ServiceResult<Dispute>> HandleCreatedAsync(Stripe.Dispute stripeDispute, List<Func<Task>> deferredPublishes, CancellationToken ct);
    Task<ServiceResult> HandleUpdatedAsync(Stripe.Dispute stripeDispute, CancellationToken ct);
    Task<ServiceResult> HandleClosedAsync(Stripe.Dispute stripeDispute, List<Func<Task>> deferredPublishes, CancellationToken ct);

    Task<bool> HasOpenDisputeForOrderAsync(int orderId, CancellationToken ct);

    Task<ServiceResult<PaginatedResult<AdminDisputeRowDto>>> ListForAdminAsync(DisputeAdminFilter filter, CancellationToken ct);
    Task<ServiceResult<PaginatedResult<DisputeRowDto>>> ListForRestaurantAsync(int restaurantId, int page, int pageSize, int userId, bool isAdmin, CancellationToken ct);
    Task<ServiceResult<AdminDisputeDetailDto>> GetAdminDetailAsync(int disputeId, CancellationToken ct);
}
```

- [ ] **Step 2: Failing tests**

Create `DisputeServiceTests.cs` with these initial scenarios:

```csharp
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Configuration;
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Factories;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class DisputeServiceTests
{
    private IDisputeRepository _disputeRepo = null!;
    private IPaymentRepository _paymentRepo = null!;
    private IRestaurantRepository _restaurantRepo = null!;
    private IRestaurantTransactionRepository _txnRepo = null!;
    private IAdminNotificationService _notifications = null!;
    private IMessagePublisher _publisher = null!;
    private AppEnvironment _env = null!;
    private DisputeService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _disputeRepo = Substitute.For<IDisputeRepository>();
        _paymentRepo = Substitute.For<IPaymentRepository>();
        _restaurantRepo = Substitute.For<IRestaurantRepository>();
        _txnRepo = Substitute.For<IRestaurantTransactionRepository>();
        _notifications = Substitute.For<IAdminNotificationService>();
        _publisher = Substitute.For<IMessagePublisher>();
        _env = TestEnvironmentFactory.Create();
        _sut = new DisputeService(_disputeRepo, _paymentRepo, _restaurantRepo, _txnRepo, _notifications, _publisher, _env);
    }

    [Test]
    public async Task HandleCreatedAsync_HappyPath_PersistsDisputeAndReversal()
    {
        var stripeDispute = new Stripe.Dispute
        {
            Id = "dp_1",
            ChargeId = "ch_1",
            Amount = 2500,
            Currency = "eur",
            Reason = "fraudulent",
            Created = DateTime.UtcNow,
            EvidenceDetails = new Stripe.DisputeEvidenceDetails { DueBy = DateTime.UtcNow.AddDays(7) },
        };
        var payment = new Payment { Id = 1, StripeChargeId = "ch_1", OrderId = 10 };
        var order = new Order { Id = 10, RestaurantId = 5, CustomerId = 2 };
        var restaurant = new Restaurant { Id = 5, Balance = 100m, OwnerId = 99 };
        _disputeRepo.GetByStripeDisputeIdAsync("dp_1", Arg.Any<CancellationToken>()).Returns((Dispute?)null);
        _paymentRepo.GetByStripeChargeIdAsync("ch_1", Arg.Any<CancellationToken>()).Returns(payment);  // assume method exists
        _paymentRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(payment);
        _restaurantRepo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(restaurant);
        _disputeRepo.CreateAsync(Arg.Any<Dispute>(), Arg.Any<CancellationToken>()).Returns(ci => { var d = ci.Arg<Dispute>(); d.Id = 42; return d; });

        var deferred = new List<Func<Task>>();
        var result = await _sut.HandleCreatedAsync(stripeDispute, deferred, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _disputeRepo.Received(1).CreateAsync(
            Arg.Is<Dispute>(d => d.StripeDisputeId == "dp_1" && d.Amount == 25m && d.RestaurantId == 5 && d.State == DisputeState.Open),
            Arg.Any<CancellationToken>());
        await _txnRepo.Received(1).CreateAsync(
            Arg.Is<RestaurantTransaction>(t => t.Type == TransactionType.DisputeReversal && t.NetAmount == -25m && t.BalanceAfter == 75m),
            Arg.Any<CancellationToken>());
        Assert.That(restaurant.Balance, Is.EqualTo(75m));
        Assert.That(deferred.Count, Is.GreaterThanOrEqualTo(2));  // at least admin + restaurant email publishes
    }

    [Test]
    public async Task HandleCreatedAsync_DuplicateStripeDisputeId_SkipsIdempotently()
    {
        var existing = new Dispute { Id = 42, StripeDisputeId = "dp_1", State = DisputeState.Open };
        _disputeRepo.GetByStripeDisputeIdAsync("dp_1", Arg.Any<CancellationToken>()).Returns(existing);
        var stripeDispute = new Stripe.Dispute { Id = "dp_1", ChargeId = "ch_1", Amount = 2500, Currency = "eur", Reason = "fraudulent", Created = DateTime.UtcNow };

        var deferred = new List<Func<Task>>();
        var result = await _sut.HandleCreatedAsync(stripeDispute, deferred, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _disputeRepo.DidNotReceive().CreateAsync(Arg.Any<Dispute>(), Arg.Any<CancellationToken>());
        await _txnRepo.DidNotReceive().CreateAsync(Arg.Any<RestaurantTransaction>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleCreatedAsync_PaymentNotFound_ReturnsError()
    {
        _disputeRepo.GetByStripeDisputeIdAsync("dp_1", Arg.Any<CancellationToken>()).Returns((Dispute?)null);
        _paymentRepo.GetByStripeChargeIdAsync("ch_missing", Arg.Any<CancellationToken>()).Returns((Payment?)null);
        var stripeDispute = new Stripe.Dispute { Id = "dp_1", ChargeId = "ch_missing", Amount = 100, Currency = "eur", Reason = "other", Created = DateTime.UtcNow };

        var result = await _sut.HandleCreatedAsync(stripeDispute, new List<Func<Task>>(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public async Task HandleClosedAsync_Won_CreatesRestoreTransactionAndRestoresBalance()
    {
        var dispute = new Dispute { Id = 42, StripeDisputeId = "dp_1", OrderId = 10, RestaurantId = 5, Amount = 25m, State = DisputeState.Open };
        var restaurant = new Restaurant { Id = 5, Balance = 75m, OwnerId = 99 };
        _disputeRepo.GetByStripeDisputeIdAsync("dp_1", Arg.Any<CancellationToken>()).Returns(dispute);
        _restaurantRepo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(restaurant);
        var stripeDispute = new Stripe.Dispute { Id = "dp_1", Status = "won" };

        var deferred = new List<Func<Task>>();
        await _sut.HandleClosedAsync(stripeDispute, deferred, CancellationToken.None);

        Assert.That(dispute.State, Is.EqualTo(DisputeState.Won));
        Assert.That(dispute.ClosedAt, Is.Not.Null);
        await _txnRepo.Received(1).CreateAsync(
            Arg.Is<RestaurantTransaction>(t => t.Type == TransactionType.DisputeRestored && t.NetAmount == 25m && t.BalanceAfter == 100m),
            Arg.Any<CancellationToken>());
        Assert.That(restaurant.Balance, Is.EqualTo(100m));
    }

    [Test]
    public async Task HandleClosedAsync_Lost_NoBalanceChange()
    {
        var dispute = new Dispute { Id = 42, StripeDisputeId = "dp_1", OrderId = 10, RestaurantId = 5, Amount = 25m, State = DisputeState.Open };
        var restaurant = new Restaurant { Id = 5, Balance = 75m };
        _disputeRepo.GetByStripeDisputeIdAsync("dp_1", Arg.Any<CancellationToken>()).Returns(dispute);
        _restaurantRepo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(restaurant);
        var stripeDispute = new Stripe.Dispute { Id = "dp_1", Status = "lost" };

        await _sut.HandleClosedAsync(stripeDispute, new List<Func<Task>>(), CancellationToken.None);

        Assert.That(dispute.State, Is.EqualTo(DisputeState.Lost));
        Assert.That(restaurant.Balance, Is.EqualTo(75m));  // unchanged
        await _txnRepo.DidNotReceive().CreateAsync(Arg.Any<RestaurantTransaction>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HasOpenDisputeForOrderAsync_OpenExists_ReturnsTrue()
    {
        _disputeRepo.HasOpenForOrderAsync(10, Arg.Any<CancellationToken>()).Returns(true);
        var result = await _sut.HasOpenDisputeForOrderAsync(10, CancellationToken.None);
        Assert.That(result, Is.True);
    }
}
```

**Prerequisites**: `IPaymentRepository.GetByStripeChargeIdAsync(string, CancellationToken)` may not exist. If not, add it to the interface + implementation (simple `FirstOrDefaultAsync(p => p.StripeChargeId == chargeId)`).

Also `IRestaurantTransactionRepository` must exist — if the existing transaction-persistence method is directly on another service, find the actual path and adjust. If it's `IRestaurantAccountService.CreditOrDebitAsync`, use that; if the plan's `_txnRepo.CreateAsync` doesn't match reality, adjust tests.

- [ ] **Step 3: Run to confirm failure**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: build fails.

- [ ] **Step 4: Implement `DisputeService.cs`**

```csharp
using System.Text.Json;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dispute;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Services;

public class DisputeService(
    IDisputeRepository disputeRepository,
    IPaymentRepository paymentRepository,
    IRestaurantRepository restaurantRepository,
    IRestaurantTransactionRepository transactionRepository,
    IAdminNotificationService notifications,
    IMessagePublisher publisher,
    AppEnvironment env) : IDisputeService
{
    public async Task<ServiceResult<Dispute>> HandleCreatedAsync(Stripe.Dispute stripeDispute, List<Func<Task>> deferredPublishes, CancellationToken ct)
    {
        // 1. Idempotency via StripeDisputeId
        var existing = await disputeRepository.GetByStripeDisputeIdAsync(stripeDispute.Id, ct);
        if (existing is not null)
        {
            return ServiceResult<Dispute>.Success(existing);
        }

        // 2. Resolve payment/order/restaurant
        var payment = await paymentRepository.GetByStripeChargeIdAsync(stripeDispute.ChargeId, ct);
        if (payment is null)
        {
            return new ServiceError(ErrorMessages.DisputePaymentNotFound);
        }

        var orderId = payment.OrderId;
        // Need restaurantId — load order. If OrderRepository has GetByIdAsync that includes Restaurant, use it; else add a minimal Order load.
        // The plan assumes payment.Order navigation is available; if not, fetch it.
        // For this impl we'll rely on an OrderRepository (inject IOrderRepository).
        // (Simpler: add IOrderRepository to constructor and use GetByIdAsync.)
        // NOTE: Also update the test SetUp accordingly if you add this dep.
        // For brevity, the actual impl uses `payment.Order` if included, else an extra fetch.

        // Temporary: load restaurant directly via payment.OrderId -> need an Order lookup.
        // Best: inject IOrderRepository. Adjust ctor + test mocks.

        // ... load order
        // Order is expected to carry RestaurantId.
        // Adjust if necessary.

        // For the sample impl, assume we've loaded order:
        // var order = await orderRepository.GetByIdAsync(orderId, ct);
        // var restaurantId = order.RestaurantId;

        // The production code should inject IOrderRepository and use it here.
        // Leaving the pattern explicit in the plan rather than wrapping in pseudo-code:

        var orderRepo = (IOrderRepository?)null!;  // — replace with injected field
        // Engineer: add `IOrderRepository orderRepository` to constructor and field.
        throw new NotImplementedException("Replace with injected orderRepository — see note above.");
    }

    // ... other methods similarly structured
}
```

**Simplified concrete impl** — engineer should produce the below, injecting `IOrderRepository` into the constructor (add it to tests' SetUp too):

```csharp
public class DisputeService(
    IDisputeRepository disputeRepository,
    IPaymentRepository paymentRepository,
    IOrderRepository orderRepository,
    IRestaurantRepository restaurantRepository,
    IRestaurantTransactionRepository transactionRepository,
    IAdminNotificationService notifications,
    IMessagePublisher publisher,
    AppEnvironment env,
    ILogger<DisputeService> logger) : IDisputeService
{
    public async Task<ServiceResult<Dispute>> HandleCreatedAsync(Stripe.Dispute stripeDispute, List<Func<Task>> deferredPublishes, CancellationToken ct)
    {
        var existing = await disputeRepository.GetByStripeDisputeIdAsync(stripeDispute.Id, ct);
        if (existing is not null) return ServiceResult<Dispute>.Success(existing);

        var payment = await paymentRepository.GetByStripeChargeIdAsync(stripeDispute.ChargeId, ct);
        if (payment is null)
        {
            logger.LogWarning("Dispute {Id} references charge {Charge} with no matching Payment row", stripeDispute.Id, stripeDispute.ChargeId);
            return new ServiceError(ErrorMessages.DisputePaymentNotFound);
        }

        var order = await orderRepository.GetByIdAsync(payment.OrderId, ct);
        if (order is null) return new ServiceError(ErrorMessages.OrderNotFound);

        var restaurant = await restaurantRepository.GetByIdAsync(order.RestaurantId, ct);
        if (restaurant is null) return new ServiceError(ErrorMessages.RestaurantNotFound);  // confirm name

        decimal amount = (decimal)stripeDispute.Amount / 100m;

        var dispute = new Dispute
        {
            StripeDisputeId = stripeDispute.Id,
            PaymentId = payment.Id,
            OrderId = order.Id,
            RestaurantId = restaurant.Id,
            Amount = amount,
            Currency = stripeDispute.Currency.ToUpperInvariant(),
            ReasonCode = stripeDispute.Reason ?? string.Empty,
            State = DisputeState.Open,
            DueBy = stripeDispute.EvidenceDetails?.DueBy,
            OpenedAt = stripeDispute.Created,
            StripePayload = SerializeDispute(stripeDispute),
        };
        await disputeRepository.CreateAsync(dispute, ct);

        // Reversal transaction
        var txn = new RestaurantTransaction
        {
            RestaurantId = restaurant.Id,
            OrderId = order.Id,
            Type = TransactionType.DisputeReversal,
            GrossAmount = amount,
            CommissionAmount = 0m,
            NetAmount = -amount,
            BalanceAfter = restaurant.Balance - amount,
            CreatedAt = DateTime.UtcNow,
        };
        await transactionRepository.CreateAsync(txn, ct);
        restaurant.Balance -= amount;
        await restaurantRepository.UpdateAsync(restaurant, ct);

        // Notifications + emails (deferred publishes for RabbitMQ)
        await RaiseNotificationsAndQueueEmailsAsync(dispute, restaurant, "open", deferredPublishes, ct);

        return ServiceResult<Dispute>.Success(dispute);
    }

    public async Task<ServiceResult> HandleUpdatedAsync(Stripe.Dispute stripeDispute, CancellationToken ct)
    {
        var dispute = await disputeRepository.GetByStripeDisputeIdAsync(stripeDispute.Id, ct);
        if (dispute is null)
        {
            logger.LogWarning("Dispute update for unknown StripeDisputeId {Id}", stripeDispute.Id);
            return new ServiceError(ErrorMessages.DisputeNotFound);
        }
        dispute.DueBy = stripeDispute.EvidenceDetails?.DueBy ?? dispute.DueBy;
        dispute.StripePayload = SerializeDispute(stripeDispute);
        await disputeRepository.UpdateAsync(dispute, ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> HandleClosedAsync(Stripe.Dispute stripeDispute, List<Func<Task>> deferredPublishes, CancellationToken ct)
    {
        var dispute = await disputeRepository.GetByStripeDisputeIdAsync(stripeDispute.Id, ct);
        if (dispute is null) return new ServiceError(ErrorMessages.DisputeNotFound);
        if (dispute.State != DisputeState.Open)
        {
            return ServiceResult.Success();  // idempotent — already closed
        }

        var restaurant = await restaurantRepository.GetByIdAsync(dispute.RestaurantId, ct);
        if (restaurant is null) return new ServiceError(ErrorMessages.RestaurantNotFound);

        var status = (stripeDispute.Status ?? string.Empty).ToLowerInvariant();
        dispute.ClosedAt = DateTime.UtcNow;
        dispute.StripePayload = SerializeDispute(stripeDispute);

        string eventKey;
        if (status == "won")
        {
            dispute.State = DisputeState.Won;
            var txn = new RestaurantTransaction
            {
                RestaurantId = restaurant.Id,
                OrderId = dispute.OrderId,
                Type = TransactionType.DisputeRestored,
                GrossAmount = dispute.Amount,
                CommissionAmount = 0m,
                NetAmount = dispute.Amount,
                BalanceAfter = restaurant.Balance + dispute.Amount,
                CreatedAt = DateTime.UtcNow,
            };
            await transactionRepository.CreateAsync(txn, ct);
            restaurant.Balance += dispute.Amount;
            await restaurantRepository.UpdateAsync(restaurant, ct);
            eventKey = "won";
        }
        else if (status == "lost")
        {
            dispute.State = DisputeState.Lost;
            // Spec 2 optional integration: if IInvoiceService available, generate credit notes.
            // Concretely: resolve IInvoiceService via IServiceProvider if registered; else skip.
            // Keeping it skipped for now — see follow-up note.
            eventKey = "lost";
        }
        else
        {
            // Other statuses (e.g. warning_closed) — out of scope; log + ack.
            logger.LogInformation("Dispute {Id} closed with unhandled status {Status}", stripeDispute.Id, status);
            return ServiceResult.Success();
        }

        await disputeRepository.UpdateAsync(dispute, ct);
        await RaiseNotificationsAndQueueEmailsAsync(dispute, restaurant, eventKey, deferredPublishes, ct);
        return ServiceResult.Success();
    }

    public Task<bool> HasOpenDisputeForOrderAsync(int orderId, CancellationToken ct) =>
        disputeRepository.HasOpenForOrderAsync(orderId, ct);

    // ListForAdminAsync / ListForRestaurantAsync / GetAdminDetailAsync — implemented in Task 12; add stubs that throw NotImplementedException for now.

    private string SerializeDispute(Stripe.Dispute d)
    {
        return JsonSerializer.Serialize(new
        {
            id = d.Id,
            chargeId = d.ChargeId,
            status = d.Status,
            reason = d.Reason,
            amount = d.Amount,
            created = d.Created,
            dueBy = d.EvidenceDetails?.DueBy,
        });
    }

    private async Task RaiseNotificationsAndQueueEmailsAsync(Dispute dispute, Restaurant restaurant, string eventKey, List<Func<Task>> deferredPublishes, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            disputeId = dispute.Id,
            stripeDisputeId = dispute.StripeDisputeId,
            orderId = dispute.OrderId,
            restaurantId = dispute.RestaurantId,
            amount = dispute.Amount,
            reason = dispute.ReasonCode,
            state = dispute.State.ToString(),
            eventKey,
        });

        await notifications.RaiseForAllAdminsAsync(NotificationType.Dispute, payload, ct);
        if (restaurant.OwnerId != 0)
        {
            await notifications.RaiseForUserAsync(restaurant.OwnerId, NotificationType.Dispute, payload, ct);
        }

        string adminTemplate = eventKey switch
        {
            "open" => "DisputeOpenedAdmin",
            "won" => "DisputeWonAdmin",
            "lost" => "DisputeLostAdmin",
            _ => "DisputeOpenedAdmin",
        };
        string restoTemplate = eventKey switch
        {
            "open" => "DisputeOpenedRestaurant",
            "won" => "DisputeWonRestaurant",
            "lost" => "DisputeLostRestaurant",
            _ => "DisputeOpenedRestaurant",
        };

        // Defer RabbitMQ publishes until after DB transaction commits.
        var capturedAdmin = BuildEmailJobMessage(adminTemplate, env.AdminDisputeEmail, dispute);
        deferredPublishes.Add(() => publisher.PublishAsync("email", capturedAdmin, ct));

        var restaurantOwnerEmail = await ResolveRestaurantOwnerEmailAsync(restaurant.Id, ct);
        if (!string.IsNullOrEmpty(restaurantOwnerEmail))
        {
            var capturedResto = BuildEmailJobMessage(restoTemplate, restaurantOwnerEmail, dispute);
            deferredPublishes.Add(() => publisher.PublishAsync("email", capturedResto, ct));
        }
    }

    private async Task<string?> ResolveRestaurantOwnerEmailAsync(int restaurantId, CancellationToken ct)
    {
        var resto = await restaurantRepository.GetByIdWithOwnerAsync(restaurantId, ct);  // may need adding if missing
        return resto?.Owner?.Email;
    }

    private object BuildEmailJobMessage(string template, string toEmail, Dispute dispute)
    {
        // Match the actual EmailJob / EmailJobMessage shape used elsewhere. Spec 2 Task 16/17
        // created EmailJob rows first and published just the JobId. Do the same here:
        //   1. create an EmailJob row (via IEmailJobService.QueueAsync(template, toEmail, modelJson))
        //   2. publish EmailJobMessage(jobId)
        // For simplicity here we defer that to the engineer matching the existing pattern.
        throw new NotImplementedException("Match IEmailJobService.QueueAsync shape from Spec 2 Task 16/17");
    }

    public Task<ServiceResult<PaginatedResult<AdminDisputeRowDto>>> ListForAdminAsync(DisputeAdminFilter filter, CancellationToken ct) => throw new NotImplementedException("Task 12");
    public Task<ServiceResult<PaginatedResult<DisputeRowDto>>> ListForRestaurantAsync(int restaurantId, int page, int pageSize, int userId, bool isAdmin, CancellationToken ct) => throw new NotImplementedException("Task 12");
    public Task<ServiceResult<AdminDisputeDetailDto>> GetAdminDetailAsync(int disputeId, CancellationToken ct) => throw new NotImplementedException("Task 12");
}
```

The `BuildEmailJobMessage` and `IEmailJobService` path must match the pattern Spec 2 used — the engineer should inspect `InvoiceJobConsumer` + `AdminInvoiceController.ResendEmail` to see how emails are queued (`IEmailJobService.QueueAsync` then `publisher.PublishAsync("email", new EmailJobMessage(jobId))`). Replicate that.

Inject `IEmailJobService emailJobService` into `DisputeService` constructor to enable this.

Similarly inject `ILogger<DisputeService> logger`.

- [ ] **Step 5: Run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~DisputeServiceTests"
```

All 6 tests must PASS.

- [ ] **Step 6: Commit**

```bash
git add DeliverTableServer/Services/Interfaces/IDisputeService.cs \
        DeliverTableServer/Services/DisputeService.cs \
        DeliverTableInfrastructure/Repositories/ \
        DeliverTableTests/Server/Unit/Services/DisputeServiceTests.cs
git commit -m "feat(server): add IDisputeService with created/updated/closed handlers and tests"
```

---

## Task 9: Extend `StripeWebhookController` / `PaymentService.HandleStripeEventAsync` to dispatch dispute events

**Files:**
- Modify: `DeliverTableServer/Services/PaymentService.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs`

`StripeWebhookController` itself is type-agnostic (reads raw body, calls `PaymentService.HandleStripeEventAsync(evt, ct)`). No controller changes needed. Dispatch happens inside `PaymentService`.

- [ ] **Step 1: Inject `IDisputeService`**

Extend `PaymentService`'s primary constructor. Update `PaymentServiceTests.SetUp` to add `_disputeService = Substitute.For<IDisputeService>();` and pass it.

- [ ] **Step 2: Failing tests**

Append:

```csharp
[Test]
public async Task HandleStripeEventAsync_ChargeDisputeCreated_DispatchesToHandleCreated()
{
    var stripeDispute = new Stripe.Dispute { Id = "dp_1", ChargeId = "ch_1", Amount = 1000, Currency = "eur", Reason = "fraudulent", Created = DateTime.UtcNow };
    var evt = new Stripe.Event { Id = "evt_disp_c", Type = "charge.dispute.created", Data = new Stripe.EventData { Object = stripeDispute } };
    _paymentRepo.TryRegisterProcessedEventAsync("evt_disp_c", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
    _disputeService.HandleCreatedAsync(stripeDispute, Arg.Any<List<Func<Task>>>(), Arg.Any<CancellationToken>())
                   .Returns(ServiceResult<Dispute>.Success(new Dispute { Id = 1 }));

    var result = await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    await _disputeService.Received(1).HandleCreatedAsync(stripeDispute, Arg.Any<List<Func<Task>>>(), Arg.Any<CancellationToken>());
}

[Test]
public async Task HandleStripeEventAsync_ChargeDisputeUpdated_DispatchesToHandleUpdated()
{
    var stripeDispute = new Stripe.Dispute { Id = "dp_2" };
    var evt = new Stripe.Event { Id = "evt_disp_u", Type = "charge.dispute.updated", Data = new Stripe.EventData { Object = stripeDispute } };
    _paymentRepo.TryRegisterProcessedEventAsync("evt_disp_u", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
    _disputeService.HandleUpdatedAsync(stripeDispute, Arg.Any<CancellationToken>()).Returns(ServiceResult.Success());

    await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

    await _disputeService.Received(1).HandleUpdatedAsync(stripeDispute, Arg.Any<CancellationToken>());
}

[Test]
public async Task HandleStripeEventAsync_ChargeDisputeClosed_DispatchesToHandleClosed()
{
    var stripeDispute = new Stripe.Dispute { Id = "dp_3", Status = "won" };
    var evt = new Stripe.Event { Id = "evt_disp_cl", Type = "charge.dispute.closed", Data = new Stripe.EventData { Object = stripeDispute } };
    _paymentRepo.TryRegisterProcessedEventAsync("evt_disp_cl", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
    _disputeService.HandleClosedAsync(stripeDispute, Arg.Any<List<Func<Task>>>(), Arg.Any<CancellationToken>()).Returns(ServiceResult.Success());

    await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

    await _disputeService.Received(1).HandleClosedAsync(stripeDispute, Arg.Any<List<Func<Task>>>(), Arg.Any<CancellationToken>());
}

[Test]
public async Task HandleStripeEventAsync_ChargeWarning_AcksWithoutDispatch()
{
    var evt = new Stripe.Event { Id = "evt_w", Type = "charge.dispute.warning_needs_response", Data = new Stripe.EventData { Object = new Stripe.Dispute() } };
    _paymentRepo.TryRegisterProcessedEventAsync("evt_w", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

    var result = await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    await _disputeService.DidNotReceive().HandleCreatedAsync(Arg.Any<Stripe.Dispute>(), Arg.Any<List<Func<Task>>>(), Arg.Any<CancellationToken>());
}
```

- [ ] **Step 3: Implement dispatch**

In `PaymentService.HandleStripeEventAsync`, add cases inside the switch (within the existing transaction scope):

```csharp
case "charge.dispute.created":
    await disputeService.HandleCreatedAsync((Stripe.Dispute)evt.Data.Object, deferredPublishes, ct);
    break;
case "charge.dispute.updated":
    await disputeService.HandleUpdatedAsync((Stripe.Dispute)evt.Data.Object, ct);
    break;
case "charge.dispute.closed":
    await disputeService.HandleClosedAsync((Stripe.Dispute)evt.Data.Object, deferredPublishes, ct);
    break;
case "charge.dispute.funds_withdrawn":
case "charge.dispute.funds_reinstated":
    logger.LogInformation("Dispute funds event {Type} acknowledged (state derived from created/closed)", evt.Type);
    break;
case "charge.dispute.warning_needs_response":
case "charge.dispute.warning_under_review":
case "charge.dispute.warning_closed":
    logger.LogInformation("Dispute warning event {Type} acknowledged (out of scope)", evt.Type);
    break;
```

- [ ] **Step 4: Run + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~PaymentServiceTests"
git add DeliverTableServer/Services/PaymentService.cs \
        DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs
git commit -m "feat(server): extend StripeWebhookController to dispatch dispute events with tests"
```

---

## Task 10: Block admin refund when open dispute exists

**Files:**
- Modify: `DeliverTableServer/Services/PaymentService.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs`

- [ ] **Step 1: Failing test**

```csharp
[Test]
public async Task RefundAsync_WithOpenDispute_ReturnsBlockedError()
{
    var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_d", Amount = 50m };
    _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
    _disputeService.HasOpenDisputeForOrderAsync(10, Arg.Any<CancellationToken>()).Returns(true);

    var result = await _sut.RefundAsync(10, 10m, "test", adminUserId: 99, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.False);
    Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.RefundBlockedByOpenDispute));
    await _stripe.DidNotReceive().CreateRefundAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
}
```

- [ ] **Step 2: Implement guard**

In `PaymentService.RefundAsync`, right after the payment lookup (before computing amounts), add:

```csharp
if (await disputeService.HasOpenDisputeForOrderAsync(orderId, ct))
{
    return new ServiceError(ErrorMessages.RefundBlockedByOpenDispute);
}
```

- [ ] **Step 3: Run + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~PaymentServiceTests"
git add DeliverTableServer/Services/PaymentService.cs \
        DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs
git commit -m "feat(server): block admin refund when open dispute exists with tests"
```

---

## Task 11: Extend `AdminNotificationService` with `RaiseForAllAdminsAsync` if missing

**Files:**
- Modify: `DeliverTableServer/Services/AdminNotificationService.cs` (or wherever the service lives)
- Modify: `DeliverTableServer/Services/Interfaces/IAdminNotificationService.cs`

- [ ] **Step 1: Read existing service**

```bash
grep -rn "interface IAdminNotificationService\|class AdminNotificationService" /home/damien/DeliverTable/DeliverTableServer/
```

Check for existing methods. If a `RaiseForAllAdminsAsync(NotificationType type, string payload, CancellationToken ct)` or similar already exists, confirm signature matches and skip this task.

If missing:

- [ ] **Step 2: Add method to interface**

```csharp
Task RaiseForAllAdminsAsync(NotificationType type, string payload, CancellationToken ct = default);
Task RaiseForUserAsync(int userId, NotificationType type, string payload, CancellationToken ct = default);
```

(Add the `RaiseForUserAsync` too if not already present — DisputeService calls both.)

- [ ] **Step 3: Implement**

```csharp
public async Task RaiseForAllAdminsAsync(NotificationType type, string payload, CancellationToken ct = default)
{
    var admins = await _userRepository.ListByRoleAsync(nameof(UserRole.Administrator), ct);
    foreach (var admin in admins)
    {
        _dbContext.Notifications.Add(new Notification
        {
            UserId = admin.Id,
            Type = type,
            Payload = payload,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
        });
    }
    await _dbContext.SaveChangesAsync(ct);
}

public async Task RaiseForUserAsync(int userId, NotificationType type, string payload, CancellationToken ct = default)
{
    _dbContext.Notifications.Add(new Notification
    {
        UserId = userId,
        Type = type,
        Payload = payload,
        IsRead = false,
        CreatedAt = DateTime.UtcNow,
    });
    await _dbContext.SaveChangesAsync(ct);
}
```

If `IUserRepository.ListByRoleAsync` doesn't exist, add it (simple query filtered by ASP.NET Identity role).

- [ ] **Step 4: Build + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
git add DeliverTableServer/Services/ DeliverTableInfrastructure/Repositories/
git commit -m "feat(server): extend INotificationService with admin bulk raise if missing"
```

(If no changes were needed because methods already existed, skip the commit.)

---

## Task 12: Add `DisputeController` + `AdminDisputeController` with tests

**Files:**
- Create: `DeliverTableServer/Controllers/DisputeController.cs`
- Create: `DeliverTableServer/Controllers/AdminDisputeController.cs`
- Modify: `DeliverTableServer/Services/DisputeService.cs` (implement the 3 list/detail methods stubbed in Task 8)
- Create: `DeliverTableTests/Server/Unit/Controllers/DisputeControllerTests.cs`
- Create: `DeliverTableTests/Server/Unit/Controllers/AdminDisputeControllerTests.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/DisputeServiceTests.cs` (add list/detail scenarios)

- [ ] **Step 1: Implement `DisputeService` list + detail methods**

Implement `ListForRestaurantAsync`, `ListForAdminAsync`, `GetAdminDetailAsync` — map entities → DTOs using the repository's `ListForRestaurantAsync` / `AdminListAsync` / `GetByIdAsync`. Owner check in `ListForRestaurantAsync`:

```csharp
if (!isAdmin)
{
    var resto = await restaurantRepository.GetByIdAsync(restaurantId, ct);
    if (resto is null || resto.OwnerId != userId)
        return new ServiceError(ErrorMessages.DisputeAccessDenied);
}
```

For `GetAdminDetailAsync`, load linked transactions via a new or existing repo method `IRestaurantTransactionRepository.ListByOrderIdAsync(orderId, ct)` filtered to `Type in { DisputeReversal, DisputeRestored }` for the specific dispute's order — include these in `AdminDisputeDetailDto.LinkedTransactions`. Also compute `StripeDashboardUrl`:

```csharp
string testPrefix = env.StripeSecretKey.StartsWith("sk_test_") ? "test/" : string.Empty;
string stripeUrl = $"https://dashboard.stripe.com/{testPrefix}disputes/{dispute.StripeDisputeId}";
```

- [ ] **Step 2: Controller tests** for both controllers

Standard happy-path + auth/ownership + not-found cases. Use `AuthenticationTestHelper.SetupAuthenticatedUser`.

- [ ] **Step 3: Controllers**

`DisputeController.cs`:

```csharp
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Dispute.Base)]
[Authorize]
public class DisputeController(IDisputeService disputeService) : ControllerBase
{
    private readonly IDisputeService _disputeService = disputeService;

    [HttpGet(ApiRoutes.Dispute.RestaurantListRoute)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner) + "," + nameof(UserRole.Administrator))]
    public async Task<IActionResult> GetForRestaurant(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        bool isAdmin = User.IsInRole(nameof(UserRole.Administrator));
        var result = await _disputeService.ListForRestaurantAsync(id, page, pageSize, userId, isAdmin, ct);
        return result.ToOkResult();
    }
}
```

`AdminDisputeController.cs`:

```csharp
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos.Dispute;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Admin.Base)]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminDisputeController(IDisputeService disputeService) : ControllerBase
{
    [HttpGet(ApiRoutes.Admin.DisputesRoute)]
    public async Task<IActionResult> List([FromQuery] DisputeAdminFilter filter, CancellationToken ct)
    {
        var result = await disputeService.ListForAdminAsync(filter, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.DisputeByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await disputeService.GetAdminDetailAsync(id, ct);
        return result.ToOkResult();
    }
}
```

- [ ] **Step 4: Tests + build + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~DisputeController|FullyQualifiedName~AdminDisputeController|FullyQualifiedName~DisputeServiceTests"
git add DeliverTableServer/Controllers/ \
        DeliverTableServer/Services/DisputeService.cs \
        DeliverTableInfrastructure/Repositories/ \
        DeliverTableTests/Server/Unit/Controllers/ \
        DeliverTableTests/Server/Unit/Services/DisputeServiceTests.cs
git commit -m "feat(server): add DisputeController and admin dispute endpoints with tests"
```

---

## Task 13: Add dispute email templates

**Files:**
- Modify: `DeliverTableWorker/Services/EmailJobType.cs` (or the enum that lists template keys)
- Create: `DeliverTableWorker/Templates/Email/DisputeOpenedAdmin.cshtml`
- Create: `DeliverTableWorker/Templates/Email/DisputeOpenedRestaurant.cshtml`
- Create: `DeliverTableWorker/Templates/Email/DisputeWonAdmin.cshtml`
- Create: `DeliverTableWorker/Templates/Email/DisputeWonRestaurant.cshtml`
- Create: `DeliverTableWorker/Templates/Email/DisputeLostAdmin.cshtml`
- Create: `DeliverTableWorker/Templates/Email/DisputeLostRestaurant.cshtml`
- Modify: `DeliverTableWorker/Services/RazorEmailTemplateRenderer.cs` if template registration is explicit

- [ ] **Step 1: Extend `EmailJobType` enum**

Add 6 members mirroring Spec 2's `InvoiceReadyCustomer`/`InvoiceReadyRestaurant` pattern:

```csharp
DisputeOpenedAdmin,
DisputeOpenedRestaurant,
DisputeWonAdmin,
DisputeWonRestaurant,
DisputeLostAdmin,
DisputeLostRestaurant,
```

- [ ] **Step 2: Create template files**

Each template is a simple Razor page. Example (`DisputeOpenedAdmin.cshtml`):

```razor
@{ Layout = null; }
<!DOCTYPE html>
<html lang="fr">
<head><meta charset="utf-8"><title>Nouveau litige</title></head>
<body>
    <h1>Nouveau litige</h1>
    <p>Un litige a été ouvert sur la commande #@Model.OrderId par le client.</p>
    <ul>
        <li>Montant : @Model.Amount.ToString("0.00 €")</li>
        <li>Motif : @Model.Reason</li>
        <li>Date d'échéance Stripe : @(Model.DueBy?.ToString("dd/MM/yyyy") ?? "—")</li>
        <li>Restaurant ID : @Model.RestaurantId</li>
    </ul>
    <p>Action requise : soumettez des preuves dans le tableau de bord Stripe.</p>
    <p><a href="@Model.StripeDashboardUrl">Ouvrir dans Stripe</a></p>
    <p><a href="@Model.AdminDetailUrl">Consulter le détail dans l'espace admin</a></p>
    <p>— L'équipe DeliverTable</p>
</body>
</html>
```

Repeat with appropriate content for the other 5 templates (won/lost + admin/restaurant). Restaurant-facing templates do not include the Stripe dashboard link (owner doesn't have Stripe access).

Use these model fields, provided by `DisputeService` when queuing the email:
- `OrderId`, `Amount`, `Reason`, `DueBy`, `RestaurantId`, `StripeDashboardUrl`, `AdminDetailUrl`

Create a corresponding `DisputeEmailModel` class in the worker:

```csharp
namespace DeliverTableWorker.Models.Email;

public class DisputeEmailModel
{
    public int DisputeId { get; set; }
    public int OrderId { get; set; }
    public int RestaurantId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public DateTime? DueBy { get; set; }
    public string StripeDashboardUrl { get; set; } = "";
    public string AdminDetailUrl { get; set; } = "";
}
```

- [ ] **Step 3: Register templates in the renderer if needed**

If `RazorEmailTemplateRenderer` has an explicit template map, add the new 6. If it auto-discovers by filename convention, no change.

- [ ] **Step 4: `DisputeService.BuildEmailJobMessage` populates the right model**

Update the stub from Task 8 to:
1. Serialize a `DisputeEmailModel` with populated fields.
2. Call `IEmailJobService.QueueAsync(EmailJobType, toEmail, model)` — matching the pattern Spec 2 used.
3. Return the `EmailJobMessage(jobId)` for publishing.

Adjust `DisputeService` tests if the returned flow changed.

- [ ] **Step 5: Build + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
git add DeliverTableWorker/Services/EmailJobType.cs \
        DeliverTableWorker/Templates/Email/Dispute*.cshtml \
        DeliverTableWorker/Models/Email/DisputeEmailModel.cs \
        DeliverTableWorker/Services/RazorEmailTemplateRenderer.cs \
        DeliverTableServer/Services/DisputeService.cs \
        DeliverTableTests/Server/Unit/Services/DisputeServiceTests.cs
git commit -m "feat(worker): add dispute email templates"
```

---

## Task 14: Add restaurant "Litiges" tab

**File:**
- Modify: `DeliverTableClient/Pages/Restaurant/Account/RestaurantAccount.razor` (the page Spec 2 Task 23 modified — reuse pattern)
- Create: `DeliverTableClient/Services/Dispute/IDisputeApiClient.cs`
- Create: `DeliverTableClient/Services/Dispute/DisputeApiClient.cs`
- Modify: `DeliverTableClient/Extensions/ApiClientServiceCollectionExtensions.cs`

- [ ] **Step 1: Create `IDisputeApiClient`**

```csharp
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dispute;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableClient.Services.Dispute;

public interface IDisputeApiClient
{
    Task<PaginatedResult<DisputeRowDto>?> GetForRestaurantAsync(int restaurantId, int page, int pageSize);
    Task<PaginatedResult<AdminDisputeRowDto>?> AdminListAsync(DisputeState? state, int? year, int? restaurantId, int page, int pageSize);
    Task<AdminDisputeDetailDto?> AdminGetAsync(int id);
}
```

Implementation mirrors `InvoiceApiClient`.

Register via `RegisterDisputeService` in `ApiClientServiceCollectionExtensions`.

- [ ] **Step 2: Add "Litiges" section to `RestaurantAccount.razor`**

Inject `IDisputeApiClient`. Load disputes in parallel with existing data. Render a read-only table with: Date, Commande, Montant, Motif, État, Échéance. Banner: "Nous gérons la défense du litige avec Stripe. Vous serez notifié du résultat.".

- [ ] **Step 3: Build + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
git add DeliverTableClient/Services/Dispute/ \
        DeliverTableClient/Extensions/ \
        DeliverTableClient/Pages/Restaurant/Account/
git commit -m "feat(client): add restaurant Litiges tab"
```

---

## Task 15: Add admin disputes list + detail pages

**Files:**
- Create: `DeliverTableClient/Pages/Admin/Disputes/AdminDisputes/AdminDisputes.razor`
- Create: `DeliverTableClient/Pages/Admin/Disputes/AdminDisputes/AdminDisputes.razor.scss`
- Create: `DeliverTableClient/Pages/Admin/Disputes/AdminDisputeDetail/AdminDisputeDetail.razor`
- Create: `DeliverTableClient/Pages/Admin/Disputes/AdminDisputeDetail/AdminDisputeDetail.razor.scss`

Mirror Spec 2's admin invoices pages for style.

- [ ] **Step 1: `AdminDisputes.razor`** at `/admin/litiges`:

Mimic `/admin/factures` structure with filters (State, Year, RestaurantId, input), Administrator-only, table with link to detail.

- [ ] **Step 2: `AdminDisputeDetail.razor`** at `/admin/litiges/{Id:int}`:

Show full `AdminDisputeDetailDto`. Prominent "Soumettre des preuves sur Stripe →" link to `StripeDashboardUrl` (external, `target="_blank"`). Linked transactions table. Deadline countdown ("J-3") when Open.

- [ ] **Step 3: Build + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
git add DeliverTableClient/Pages/Admin/Disputes/
git commit -m "feat(client): add admin disputes list and detail pages"
```

---

## Task 16: Add dispute banner to order detail for admin viewers

**Files:**
- Modify: the existing admin-visible order detail page (find via grep; e.g. `Pages/Admin/Orders/...` or wherever admins view an order)

- [ ] **Step 1: Find page**

```bash
grep -rn "OrderDetail\|/admin/order\|/admin/commande" DeliverTableClient/Pages/
```

If no dedicated admin order page exists, inject the banner into `OrderConfirmation.razor` conditionally (only visible to admins).

- [ ] **Step 2: Add banner**

Inject `IDisputeApiClient`. If the user is in the `Administrator` role AND a dispute exists for the current order (call a new helper endpoint `GET /api/v1/admin/disputes?orderId={id}` — or load via `AdminListAsync` filtered by the order), render:

```razor
@if (_hasDispute)
{
    <div class="alert alert-warning">
        ⚠ Litige en cours sur cette commande.
        <a href="@($"/admin/litiges/{_disputeId}")">Voir le détail</a>
    </div>
}
```

Simplest implementation: call `AdminListAsync` with no filters that matches this order (API may need a `?orderId=` filter — if not present, add to `DisputeAdminFilter` and the repo query).

- [ ] **Step 3: Build + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
git add DeliverTableClient/ DeliverTableSharedLibrary/ DeliverTableInfrastructure/ DeliverTableServer/
git commit -m "feat(client): add dispute banner to order detail for admin viewers"
```

---

## Task 17: Final DI registration + verification

**Files:**
- Modify: `DeliverTableServer/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Verify DI**

Confirm `IDisputeRepository → DisputeRepository` (scoped) and `IDisputeService → DisputeService` (scoped) are registered. Add if missing.

- [ ] **Step 2: Format check**

```bash
make format-check
```

If it fails:

```bash
make format-fix
```

- [ ] **Step 3: Full suite**

```bash
make test
```

Expected: all tests pass (except the documented `Load_AppliesDefaults_WhenOptionalVarsAreMissing` Docker failure).

- [ ] **Step 4: Manual end-to-end QA** (user's responsibility — implementer reports readiness)

With dev stack + `stripe listen` running:
1. Complete an order end-to-end (capture succeeds).
2. `stripe trigger charge.dispute.created` — verify:
   - `Dispute` row created in DB with `State=Open`.
   - A `RestaurantTransaction` with `Type=DisputeReversal` exists; restaurant's `Balance` decreased.
   - Email received at `ADMIN_DISPUTE_EMAIL` and at the restaurant owner's address.
   - In-app notifications for all admins + restaurant owner.
3. Log in as admin → `/admin/litiges` → verify the dispute appears with `Open` state and a deadline countdown.
4. `stripe trigger charge.dispute.closed` (default `won` status) — verify:
   - `Dispute.State = Won`, `ClosedAt` set.
   - A `DisputeRestored` transaction; restaurant balance restored.
   - Won emails received.
5. Try the admin refund endpoint on a charge with an open dispute → should return the French blocked error.

- [ ] **Step 5: Commit any format fixes**

```bash
git add -u
git commit -m "style: apply formatting fixes"
```

(Skip if nothing changed.)

---

## Self-Review

- [x] **Spec coverage**: each of the 14 spec sections maps to tasks above. Data model → Tasks 1-4. Env + errors → Task 5. DTOs + routes → Task 6. Repo → Task 7. Service → Tasks 8, 12. Webhook dispatch → Task 9. Refund guard → Task 10. Notifications → Task 11. Email templates → Task 13. Client UX → Tasks 14-16. DI + verification → Task 17.
- [x] **Placeholder scan**: Task 8 Step 4 contains a stub block (`throw new NotImplementedException("Replace with injected orderRepository — see note above.");`) intentionally — the engineer is told to inject `IOrderRepository` and re-do the block; the "Simplified concrete impl" section below it has the actual code they should write. Reviewed and deemed acceptable given the complexity.
- [x] **Type consistency**: `IDisputeRepository`, `IDisputeService`, `DisputeState`, `DisputeRowDto`, `AdminDisputeRowDto`, `AdminDisputeDetailDto`, `DisputeAdminFilter`, `EmailJobType` dispute members, `RestaurantTransaction.Type = DisputeReversal/DisputeRestored`, `NotificationType.Dispute`, `AdminDisputeUrl`, `StripeDashboardUrl` — referenced consistently.
- [x] **Dependency gaps**: `IPaymentRepository.GetByStripeChargeIdAsync(string, ct)` may not exist — Task 8 calls it out. `IRestaurantRepository.GetByIdWithOwnerAsync` may not exist — called out. `IAdminNotificationService.RaiseForAllAdminsAsync/RaiseForUserAsync` → explicit Task 11. `IRestaurantTransactionRepository` — may need adding if the current transaction persistence is directly on a service; called out.
- [x] **Reality check on Stripe types**: `Stripe.Dispute` has `ChargeId` (confirmed via Stripe.net v51), `Status`, `Reason`, `Amount`, `Currency`, `Created`, `EvidenceDetails.DueBy`. All referenced correctly.

---
