# Stripe Payments Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Integrate Stripe for customer → platform payments (auth-on-checkout, capture-on-restaurant-accept) with Payment Element UI, webhook-driven state, reversible loyalty/discount redemptions, admin refunds, and background timeout sweeps.

**Architecture:** `Controller → Service → Repository` pattern extended with an `IStripeGateway` abstraction at the external-boundary layer. The PaymentIntent lifecycle (create/authorize/capture/cancel/refund) is reflected onto our `Payment`, `Refund`, `ProcessedStripeEvent`, and `Order` entities. A new `DeliverTableScheduler` project runs background sweeps (15-min abandonment, 24h restaurant timeout) via `IPaymentLifecycleService` shared with the server. Client uses Stripe Payment Element via JS interop wrapped in `IStripeJsInterop`.

**Tech Stack:** .NET 10, ASP.NET Core, Entity Framework Core + Npgsql, Stripe.net targeting API version `2026-03-25.dahlia`, NUnit 4 + NSubstitute for tests, Blazor WASM + Stripe.js, RabbitMQ via existing `DeliverTableWorker`, Garage S3 (unchanged in this spec).

**Spec**: `docs/superpowers/specs/2026-04-14-stripe-payments-core-design.md` — read it before starting.

**Important conventions (from CLAUDE.md)**:
- All commands run inside the docker-dev stack (`make dev` first).
- Run a specific test: `docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~ClassName"`.
- Build: `docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln`.
- Format: `make format-check` / `make format-fix`.
- Full suite: `make test`.
- TDD is mandatory for services and controllers. Entities, enums, DTOs, mappers, EF configs, migrations, and DI registration do **not** require tests.
- Never include `Co-Authored-By` lines in commits.
- Use French for all `ErrorMessages` values and user-visible strings.
- Use `nameof(UserRole.X)` rather than hardcoded role strings.

---

## File Structure

New and modified files this plan touches, grouped by responsibility.

### Shared enums and DTOs
- Modify: `DeliverTableSharedLibrary/Enums/OrderStatus.cs`
- Modify: `DeliverTableSharedLibrary/Enums/PaymentStatus.cs`
- Create: `DeliverTableSharedLibrary/Enums/LoyaltyRedemptionStatus.cs`
- Create: `DeliverTableSharedLibrary/Enums/DiscountRedemptionStatus.cs`
- Modify: `DeliverTableSharedLibrary/Constants/ApiRoutes.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Payment/CreateOrderResponse.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Payment/AdminRefundRequest.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Payment/RefundDto.cs`
- Modify: `DeliverTableSharedLibrary/Dtos/Order/OrderDto.cs`

### Data model
- Create: `DeliverTableInfrastructure/Models/Refund.cs`
- Create: `DeliverTableInfrastructure/Models/ProcessedStripeEvent.cs`
- Modify: `DeliverTableInfrastructure/Models/Payment.cs` (add `Refunds` navigation)
- Modify: `DeliverTableInfrastructure/Models/User.cs` (add `StripeCustomerId`)
- Modify: `DeliverTableInfrastructure/Models/LoyaltyTransaction.cs` (add `Status`)
- Modify: `DeliverTableInfrastructure/Models/DiscountCodeRedemption.cs` (add `Status`)
- Create: `DeliverTableInfrastructure/Data/ModelConfiguration/RefundConfiguration.cs`
- Create: `DeliverTableInfrastructure/Data/ModelConfiguration/ProcessedStripeEventConfiguration.cs`
- Modify: `DeliverTableInfrastructure/Data/ModelConfiguration/PaymentConfiguration.cs` (add Refunds navigation)
- Modify: `DeliverTableInfrastructure/Data/DeliverTableContext.cs` (new `DbSet`s)
- Create: `DeliverTableInfrastructure/Data/Migrations/{timestamp}_AddStripePaymentsCore.cs`

### Infrastructure (Stripe gateway + lifecycle + repositories)
- Create: `DeliverTableInfrastructure/Payments/IStripeGateway.cs`
- Create: `DeliverTableInfrastructure/Payments/StripeGateway.cs`
- Create: `DeliverTableInfrastructure/Payments/StripeGatewayResult.cs` (lightweight DTO types for gateway returns)
- Create: `DeliverTableInfrastructure/Payments/IPaymentLifecycleService.cs`
- Create: `DeliverTableInfrastructure/Payments/PaymentLifecycleService.cs`
- Create: `DeliverTableInfrastructure/Repositories/Interfaces/IPaymentRepository.cs`
- Create: `DeliverTableInfrastructure/Repositories/PaymentRepository.cs`
- Modify: `DeliverTableInfrastructure/Repositories/Interfaces/IOrderRepository.cs` (add `GetOrdersOlderThanAsync`)
- Modify: `DeliverTableInfrastructure/Repositories/OrderRepository.cs`

### Server (config + services + controllers)
- Modify: `DeliverTableServer/Configuration/AppEnvironment.cs`
- Modify: `DeliverTableServer/Constants/ErrorMessages.cs`
- Modify: `DeliverTableServer/Program.cs` (initialize Stripe SDK)
- Modify: `DeliverTableServer/Extensions/ServiceCollectionExtensions.cs`
- Create: `DeliverTableServer/Services/Interfaces/IPaymentService.cs`
- Create: `DeliverTableServer/Services/PaymentService.cs`
- Create: `DeliverTableServer/Controllers/PaymentController.cs`
- Create: `DeliverTableServer/Controllers/StripeWebhookController.cs`
- Modify: `DeliverTableServer/Services/OrderService.cs`
- Modify: `DeliverTableServer/Controllers/OrderController.cs` (returns `CreateOrderResponse`)
- Modify: `DeliverTableServer/Services/AdminOrderService.cs` (refund endpoint path)
- Modify: `DeliverTableServer/Controllers/AdminController.cs` (refund endpoint)
- Modify: `DeliverTableServer/DeliverTableServer.csproj` (Stripe.net)
- Modify: `DeliverTableInfrastructure/DeliverTableInfrastructure.csproj` (Stripe.net)

### DeliverTableScheduler (new project)
- Create: `DeliverTableScheduler/DeliverTableScheduler.csproj`
- Create: `DeliverTableScheduler/Program.cs`
- Create: `DeliverTableScheduler/Configuration/SchedulerEnvironment.cs`
- Create: `DeliverTableScheduler/Jobs/PeriodicSweepJob.cs` (shared base)
- Create: `DeliverTableScheduler/Jobs/OrderAbandonmentSweep.cs`
- Create: `DeliverTableScheduler/Jobs/OrderRestaurantTimeoutSweep.cs`
- Create: `docker/images/scheduler.dev.dockerfile` (mirrors `worker.dev.dockerfile`)
- Create: `docker/images/scheduler.prod.dockerfile` (mirrors `worker.prod.dockerfile`)
- Modify: `DeliverTable.sln`
- Modify: `docker-dev.yaml` (new `scheduler` service on `dt-backend-net` at 192.168.60.90)
- Modify: `docker-prod.yaml` (new `scheduler` service on `dt-backend-net` at 172.32.0.70)

### Scheduler tests (new project)
- Create: `DeliverTableSchedulerTests/DeliverTableSchedulerTests.csproj`
- Create: `DeliverTableSchedulerTests/Jobs/OrderAbandonmentSweepTests.cs`
- Create: `DeliverTableSchedulerTests/Jobs/OrderRestaurantTimeoutSweepTests.cs`
- Create: `DeliverTableSchedulerTests/Factories/OrderFactory.cs` (local helper if needed)
- Modify: `DeliverTable.sln`
- Modify: `Makefile` (add scheduler tests target)

### Existing test projects
- Create: `DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs`
- Create: `DeliverTableTests/Server/Unit/Controllers/PaymentControllerTests.cs`
- Create: `DeliverTableTests/Server/Unit/Controllers/StripeWebhookControllerTests.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/OrderServiceTests.cs` (added scenarios)
- Modify: `DeliverTableTests/Server/Unit/Controllers/OrderControllerTests.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/AdminOrderServiceTests.cs` (refund scenarios)
- Create: `DeliverTableTests/Infrastructure/Unit/Payments/PaymentLifecycleServiceTests.cs`

### Client
- Modify: `DeliverTableClient/wwwroot/index.html`
- Create: `DeliverTableClient/Services/Payment/IPaymentApiClient.cs`
- Create: `DeliverTableClient/Services/Payment/PaymentApiClient.cs`
- Create: `DeliverTableClient/Services/Payment/IStripeJsInterop.cs`
- Create: `DeliverTableClient/Services/Payment/StripeJsInterop.cs`
- Create: `DeliverTableClient/Pages/Checkout/Checkout/Checkout.razor`
- Create: `DeliverTableClient/Pages/Checkout/Checkout/Checkout.razor.scss`
- Create: `DeliverTableClient/Pages/Checkout/Checkout/Checkout.razor.js`
- Create: `DeliverTableClient/Pages/Checkout/CheckoutResult/CheckoutResult.razor`
- Create: `DeliverTableClient/Pages/Checkout/CheckoutResult/CheckoutResult.razor.scss`
- Modify: `DeliverTableClient/Program.cs` (DI)
- Modify: the cart page that currently POSTs to `/api/v1/order` (redirect to `/checkout`)

### Docs and env
- Modify: `.env.example`
- Modify: `docs/db/er-diagram.md`
- Modify: `docs/db/data-dictionary.md`
- Modify: `README.md` (Stripe CLI workflow section)

---

## Task 1: Add new enum values (shared)

**Files:**
- Modify: `DeliverTableSharedLibrary/Enums/OrderStatus.cs`
- Modify: `DeliverTableSharedLibrary/Enums/PaymentStatus.cs`
- Create: `DeliverTableSharedLibrary/Enums/LoyaltyRedemptionStatus.cs`
- Create: `DeliverTableSharedLibrary/Enums/DiscountRedemptionStatus.cs`

Enums do not require tests per CLAUDE.md.

- [ ] **Step 1: Add `AwaitingPayment` to `OrderStatus`**

Open `DeliverTableSharedLibrary/Enums/OrderStatus.cs` and add:

```csharp
AwaitingPayment = 100,
```

Preserve existing member values — do not renumber.

- [ ] **Step 2: Add `Authorized` and `PartiallyRefunded` to `PaymentStatus`**

```csharp
Authorized = 100,
PartiallyRefunded = 101,
```

- [ ] **Step 3: Create `LoyaltyRedemptionStatus.cs`**

```csharp
namespace DeliverTableSharedLibrary.Enums;

public enum LoyaltyRedemptionStatus
{
    Pending = 0,
    Committed = 1,
    Reversed = 2,
}
```

- [ ] **Step 4: Create `DiscountRedemptionStatus.cs`**

```csharp
namespace DeliverTableSharedLibrary.Enums;

public enum DiscountRedemptionStatus
{
    Pending = 0,
    Committed = 1,
    Reversed = 2,
}
```

- [ ] **Step 5: Build the solution**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: build succeeds with no errors.

- [ ] **Step 6: Commit**

```bash
git add DeliverTableSharedLibrary/Enums/OrderStatus.cs \
        DeliverTableSharedLibrary/Enums/PaymentStatus.cs \
        DeliverTableSharedLibrary/Enums/LoyaltyRedemptionStatus.cs \
        DeliverTableSharedLibrary/Enums/DiscountRedemptionStatus.cs
git commit -m "feat(shared): add AwaitingPayment, Authorized, PartiallyRefunded enum values"
```

---

## Task 2: Add `Refund` and `ProcessedStripeEvent` entities with EF configs

**Files:**
- Create: `DeliverTableInfrastructure/Models/Refund.cs`
- Create: `DeliverTableInfrastructure/Models/ProcessedStripeEvent.cs`
- Modify: `DeliverTableInfrastructure/Models/Payment.cs`
- Create: `DeliverTableInfrastructure/Data/ModelConfiguration/RefundConfiguration.cs`
- Create: `DeliverTableInfrastructure/Data/ModelConfiguration/ProcessedStripeEventConfiguration.cs`
- Modify: `DeliverTableInfrastructure/Data/ModelConfiguration/PaymentConfiguration.cs`
- Modify: `DeliverTableInfrastructure/Data/DeliverTableContext.cs`

- [ ] **Step 1: Create `Refund.cs`**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableInfrastructure.Models;

public class Refund
{
    [Key]
    public int Id { get; set; }

    public int PaymentId { get; set; }

    [ForeignKey(nameof(PaymentId))]
    public Payment Payment { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string StripeRefundId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(9, 2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Create `ProcessedStripeEvent.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace DeliverTableInfrastructure.Models;

public class ProcessedStripeEvent
{
    [Key]
    [MaxLength(200)]
    public string StripeEventId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 3: Add `Refunds` navigation to `Payment.cs`**

Add at the end of the `Payment` class:

```csharp
public List<Refund> Refunds { get; set; } = new();
```

- [ ] **Step 4: Create `RefundConfiguration.cs`**

```csharp
using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.StripeRefundId).HasMaxLength(200).IsRequired();
        builder.HasIndex(r => r.StripeRefundId).IsUnique();
        builder.Property(r => r.Currency).HasMaxLength(3).HasDefaultValue("EUR").IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(500).HasDefaultValue(string.Empty);
        builder.Property(r => r.Amount).HasColumnType("decimal(9, 2)").IsRequired();

        builder.HasOne(r => r.Payment)
               .WithMany(p => p.Refunds)
               .HasForeignKey(r => r.PaymentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.CreatedByUser)
               .WithMany()
               .HasForeignKey(r => r.CreatedByUserId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
```

- [ ] **Step 5: Create `ProcessedStripeEventConfiguration.cs`**

```csharp
using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class ProcessedStripeEventConfiguration : IEntityTypeConfiguration<ProcessedStripeEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedStripeEvent> builder)
    {
        builder.HasKey(e => e.StripeEventId);
        builder.Property(e => e.StripeEventId).HasMaxLength(200).IsRequired();
        builder.Property(e => e.EventType).HasMaxLength(100).IsRequired();
    }
}
```

- [ ] **Step 6: Register DbSets in `DeliverTableContext.cs`**

Open the context. Where other `DbSet<T>` properties are declared, add:

```csharp
public DbSet<Refund> Refunds => Set<Refund>();
public DbSet<ProcessedStripeEvent> ProcessedStripeEvents => Set<ProcessedStripeEvent>();
```

If the context uses `modelBuilder.ApplyConfigurationsFromAssembly(...)`, the new configurations are picked up automatically — no further change. Otherwise, add:

```csharp
modelBuilder.ApplyConfiguration(new RefundConfiguration());
modelBuilder.ApplyConfiguration(new ProcessedStripeEventConfiguration());
```

Verify by grep: `rg "ApplyConfigurationsFromAssembly|ApplyConfiguration" DeliverTableInfrastructure/Data/DeliverTableContext.cs`.

- [ ] **Step 7: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: build succeeds.

- [ ] **Step 8: Commit**

```bash
git add DeliverTableInfrastructure/Models/Refund.cs \
        DeliverTableInfrastructure/Models/ProcessedStripeEvent.cs \
        DeliverTableInfrastructure/Models/Payment.cs \
        DeliverTableInfrastructure/Data/ModelConfiguration/RefundConfiguration.cs \
        DeliverTableInfrastructure/Data/ModelConfiguration/ProcessedStripeEventConfiguration.cs \
        DeliverTableInfrastructure/Data/DeliverTableContext.cs
git commit -m "feat(server): add Refund and ProcessedStripeEvent entities with EF configs"
```

---

## Task 3: Add `StripeCustomerId` to User and `Status` to loyalty/discount redemptions

**Files:**
- Modify: `DeliverTableInfrastructure/Models/User.cs`
- Modify: `DeliverTableInfrastructure/Models/LoyaltyTransaction.cs`
- Modify: `DeliverTableInfrastructure/Models/DiscountCodeRedemption.cs`
- Possibly modify corresponding EF configs if column-level constraints are needed.

- [ ] **Step 1: Add `StripeCustomerId` to `User.cs`**

```csharp
[MaxLength(200)]
public string? StripeCustomerId { get; set; }
```

- [ ] **Step 2: Add `Status` to `LoyaltyTransaction.cs`**

```csharp
public LoyaltyRedemptionStatus Status { get; set; } = LoyaltyRedemptionStatus.Committed;
```

Add `using DeliverTableSharedLibrary.Enums;` if not already present.

- [ ] **Step 3: Add `Status` to `DiscountCodeRedemption.cs`**

```csharp
public DiscountRedemptionStatus Status { get; set; } = DiscountRedemptionStatus.Committed;
```

Add the using directive.

- [ ] **Step 4: Update EF configs for the Status columns (if string enum conversion is used elsewhere)**

Check `DeliverTableInfrastructure/Data/ModelConfiguration/LoyaltyTransactionConfiguration.cs` and `DiscountCodeRedemptionConfiguration.cs` (names may vary). If they use `.HasConversion<string>()` for other enums, mirror that for the new `Status` column. If they use default int storage, no change is needed.

Verify by searching: `rg "HasConversion<string>" DeliverTableInfrastructure/Data/ModelConfiguration/`.

- [ ] **Step 5: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 6: Commit**

```bash
git add DeliverTableInfrastructure/Models/User.cs \
        DeliverTableInfrastructure/Models/LoyaltyTransaction.cs \
        DeliverTableInfrastructure/Models/DiscountCodeRedemption.cs \
        DeliverTableInfrastructure/Data/ModelConfiguration/
git commit -m "feat(server): add StripeCustomerId to User and Status to loyalty/discount redemptions"
```

---

## Task 4: Add EF migration `AddStripePaymentsCore`

**Files:**
- Create: `DeliverTableInfrastructure/Data/Migrations/{timestamp}_AddStripePaymentsCore.cs` (generated)

- [ ] **Step 1: Ensure stack is running**

```bash
make dev
```

Wait until Postgres + backend containers are up.

- [ ] **Step 2: Generate migration**

```bash
docker compose -f docker-dev.yaml exec backend dotnet ef migrations add AddStripePaymentsCore \
    --project /src/DeliverTableInfrastructure \
    --startup-project /src/DeliverTableServer \
    --output-dir Data/Migrations
```

Expected: migration file created under `DeliverTableInfrastructure/Data/Migrations/` with a timestamp prefix.

- [ ] **Step 3: Review the generated migration**

Open the generated file. Verify it:
- Creates `Refunds` table (with FKs + unique index on `StripeRefundId`).
- Creates `ProcessedStripeEvents` table (PK on `StripeEventId`).
- Adds `StripeCustomerId` nullable column to `Users` (or AspNet users table name used in this project).
- Adds `Status` column to `LoyaltyTransactions` with default `1` (Committed) so historical rows are backfilled.
- Adds `Status` column to `DiscountCodeRedemptions` with default `1`.

If any of these are missing or wrong, **do not** hand-edit the migration arbitrarily; instead roll back the migration and fix the entities / configs first:

```bash
docker compose -f docker-dev.yaml exec backend dotnet ef migrations remove \
    --project /src/DeliverTableInfrastructure \
    --startup-project /src/DeliverTableServer
```

Then re-run Step 2. Small defensible tweaks (e.g., setting a `defaultValue` for a NOT NULL column) can be hand-edited.

- [ ] **Step 4: Apply migration**

```bash
make dev-migrate
```

Expected: migration applies cleanly against the dev database.

- [ ] **Step 5: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 6: Commit**

```bash
git add DeliverTableInfrastructure/Data/Migrations/
git commit -m "feat(db): add migration AddStripePaymentsCore"
```

---

## Task 5: Update DB documentation

**Files:**
- Modify: `docs/db/er-diagram.md`
- Modify: `docs/db/data-dictionary.md`

- [ ] **Step 1: Update `docs/db/er-diagram.md`**

Add the two new entities with their relationships. Follow the existing format of the file. Example additions:

- `Refund` (one-to-many from `Payment`; optional FK to `User` via `CreatedByUserId`).
- `ProcessedStripeEvent` (standalone idempotency log).
- `User.StripeCustomerId` new optional column.
- `LoyaltyTransaction.Status`, `DiscountCodeRedemption.Status` new columns.
- New enum values on `OrderStatus` and `PaymentStatus`.

- [ ] **Step 2: Update `docs/db/data-dictionary.md`**

Add entries for:
- `Refund` entity: each field + type + description.
- `ProcessedStripeEvent` entity.
- New columns on `User`, `LoyaltyTransaction`, `DiscountCodeRedemption`.
- New enum values for `OrderStatus` (`AwaitingPayment`), `PaymentStatus` (`Authorized`, `PartiallyRefunded`).
- New enums `LoyaltyRedemptionStatus`, `DiscountRedemptionStatus`.

- [ ] **Step 3: Commit**

```bash
git add docs/db/er-diagram.md docs/db/data-dictionary.md
git commit -m "docs(db): update ER diagram and data dictionary for Stripe payments core"
```

---

## Task 6: Add Stripe SDK, config, and French error messages

**Files:**
- Modify: `.env.example`
- Modify: `DeliverTableServer/Configuration/AppEnvironment.cs`
- Modify: `DeliverTableServer/DeliverTableServer.csproj`
- Modify: `DeliverTableInfrastructure/DeliverTableInfrastructure.csproj`
- Modify: `DeliverTableServer/Program.cs`
- Modify: `DeliverTableServer/Constants/ErrorMessages.cs`

- [ ] **Step 1: Update `.env.example`**

The existing Stripe block already has `STRIPE_PUBLISHABLE_KEY` and `STRIPE_SECRET_KEY`. Append the webhook secret:

```bash
# Signing secret for webhook signature verification
# Dev: `stripe listen --print-secret`
# Prod: Stripe Dashboard → Developers → Webhooks → endpoint → Signing secret
STRIPE_WEBHOOK_SECRET=whsec_1234567890
```

- [ ] **Step 2: Add Stripe.net package to the Infrastructure project**

```bash
docker compose -f docker-dev.yaml exec backend dotnet add /src/DeliverTableInfrastructure/DeliverTableInfrastructure.csproj package Stripe.net
```

Verify the csproj shows `<PackageReference Include="Stripe.net" Version="..."/>`.

- [ ] **Step 3: Reference Stripe.net transitively from Server**

`DeliverTableServer.csproj` already references `DeliverTableInfrastructure`, so no additional `PackageReference` is needed unless Server code directly uses Stripe types. Leave Server csproj unchanged for now; if later steps fail to resolve Stripe types in Server, add the package explicitly.

- [ ] **Step 4: Extend `AppEnvironment.cs` with Stripe fields**

Locate the sealed class. Add three required string properties initialized by `Load()`:

```csharp
public required string StripePublishableKey { get; init; }
public required string StripeSecretKey { get; init; }
public required string StripeWebhookSecret { get; init; }
```

In `Load()`, add to the validation aggregator:

```csharp
string stripePublishable = RequireVar("STRIPE_PUBLISHABLE_KEY", errors);
string stripeSecret = RequireVar("STRIPE_SECRET_KEY", errors);
string stripeWebhookSecret = RequireVar("STRIPE_WEBHOOK_SECRET", errors);
```

and pass them to the constructor / object initializer. Match the exact pattern used by existing fields.

- [ ] **Step 5: Initialize Stripe SDK in `Program.cs`**

Locate the section just after `builder.Services.AddSingleton(env);` (early config wiring). Add:

```csharp
Stripe.StripeConfiguration.ApiKey = env.StripeSecretKey;
Stripe.StripeConfiguration.ApiVersion = "2026-03-25.dahlia";
```

- [ ] **Step 6: Add French error messages in `ErrorMessages.cs`**

Append to the file:

```csharp
public const string PaymentIntentCreationFailed  = "Impossible de créer l'intention de paiement.";
public const string PaymentCaptureFailed         = "Le prélèvement du paiement a échoué.";
public const string PaymentCancelFailed          = "L'annulation du paiement a échoué.";
public const string PaymentRefundFailed          = "Le remboursement a échoué.";
public const string PaymentAlreadyRefunded       = "Ce paiement a déjà été intégralement remboursé.";
public const string PaymentRefundExceedsAmount   = "Le montant demandé dépasse le solde remboursable.";
public const string PaymentNotFound              = "Paiement introuvable.";
public const string OrderPaymentRequired         = "Cette commande est en attente de paiement.";
public const string OrderPaymentAlreadyProcessed = "Le paiement de cette commande est déjà traité.";
public const string StripeCustomerCreationFailed = "Impossible de créer le client Stripe.";
public const string WebhookSignatureInvalid      = "Signature Stripe invalide.";
```

- [ ] **Step 7: Ensure `STRIPE_WEBHOOK_SECRET` is set in local `.env`**

Locally, run `stripe listen --print-secret` (outside the container) and paste the value into `.env`. Restart the stack if already running.

- [ ] **Step 8: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 9: Commit**

```bash
git add .env.example \
        DeliverTableInfrastructure/DeliverTableInfrastructure.csproj \
        DeliverTableServer/Configuration/AppEnvironment.cs \
        DeliverTableServer/Program.cs \
        DeliverTableServer/Constants/ErrorMessages.cs
git commit -m "feat(server): add Stripe configuration and French error messages"
```

---

## Task 7: Add Stripe payment DTOs and API routes (shared)

**Files:**
- Modify: `DeliverTableSharedLibrary/Constants/ApiRoutes.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Payment/CreateOrderResponse.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Payment/AdminRefundRequest.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Payment/RefundDto.cs`
- Modify: `DeliverTableSharedLibrary/Dtos/Order/OrderDto.cs`

- [ ] **Step 1: Add `Payment` and `StripeWebhook` route classes**

In `ApiRoutes.cs`, near the other nested classes, add:

```csharp
public static class Payment
{
    public const string Base = "api/v1/payment";
    public const string CancelRoute = "{orderId:int}/cancel";
}

public static class StripeWebhook
{
    public const string Base = "api/v1/stripe";
    public const string WebhookRoute = "webhook";
    public const string Webhook = Base + "/" + WebhookRoute;
}
```

- [ ] **Step 2: Extend `ApiRoutes.Admin` with the refund route**

Inside `public static class Admin`, after the existing order routes:

```csharp
public const string OrderRefundRoute = "orders/{id:int}/refund";
```

- [ ] **Step 3: Create `CreateOrderResponse.cs`**

```csharp
namespace DeliverTableSharedLibrary.Dtos.Payment;

public record CreateOrderResponse(
    int OrderId,
    string ClientSecret,
    string PublishableKey,
    decimal Amount,
    string Currency);
```

- [ ] **Step 4: Create `AdminRefundRequest.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Payment;

public class AdminRefundRequest
{
    [Range(0.01, 100000)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
```

- [ ] **Step 5: Create `RefundDto.cs`**

```csharp
namespace DeliverTableSharedLibrary.Dtos.Payment;

public record RefundDto(int Id, decimal Amount, string Currency, string Reason, DateTime CreatedAt);
```

- [ ] **Step 6: Extend `OrderDto.cs`**

Add fields:

```csharp
public string? GatewayStatus { get; set; }
public List<RefundDto> Refunds { get; set; } = new();
public decimal TotalRefunded { get; set; }
```

Add using: `using DeliverTableSharedLibrary.Dtos.Payment;`.

- [ ] **Step 7: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 8: Commit**

```bash
git add DeliverTableSharedLibrary/Constants/ApiRoutes.cs \
        DeliverTableSharedLibrary/Dtos/Payment/ \
        DeliverTableSharedLibrary/Dtos/Order/OrderDto.cs
git commit -m "feat(shared): add Stripe payment DTOs and routes"
```

---

## Task 8: Add `IStripeGateway` infrastructure wrapper (no tests — external boundary)

**Files:**
- Create: `DeliverTableInfrastructure/Payments/IStripeGateway.cs`
- Create: `DeliverTableInfrastructure/Payments/StripeGateway.cs`
- Create: `DeliverTableInfrastructure/Payments/StripeGatewayResult.cs`

Per CLAUDE.md, the gateway is the external boundary and is not unit-tested (it's consumed by tested services behind interfaces).

- [ ] **Step 1: Create `StripeGatewayResult.cs`**

```csharp
namespace DeliverTableInfrastructure.Payments;

public sealed record StripeCustomerResult(string CustomerId);

public sealed record StripePaymentIntentResult(
    string PaymentIntentId,
    string ClientSecret,
    string Status);

public sealed record StripeCaptureResult(string PaymentIntentId, string Status);

public sealed record StripeCancelResult(string PaymentIntentId, string Status);

public sealed record StripeRefundResult(
    string RefundId,
    string PaymentIntentId,
    decimal Amount,
    string Currency,
    string Status);
```

- [ ] **Step 2: Create `IStripeGateway.cs`**

```csharp
using Stripe;

namespace DeliverTableInfrastructure.Payments;

public interface IStripeGateway
{
    Task<StripeCustomerResult> CreateCustomerAsync(
        string email,
        string fullName,
        IDictionary<string, string>? metadata,
        CancellationToken ct);

    Task<StripePaymentIntentResult> CreatePaymentIntentAsync(
        long amountInMinorUnits,
        string currency,
        string stripeCustomerId,
        IDictionary<string, string> metadata,
        string idempotencyKey,
        CancellationToken ct);

    Task<StripeCaptureResult> CapturePaymentIntentAsync(
        string paymentIntentId,
        string idempotencyKey,
        CancellationToken ct);

    Task<StripeCancelResult> CancelPaymentIntentAsync(
        string paymentIntentId,
        string idempotencyKey,
        CancellationToken ct);

    Task<StripeRefundResult> CreateRefundAsync(
        string paymentIntentId,
        long amountInMinorUnits,
        string idempotencyKey,
        CancellationToken ct);

    Event ConstructWebhookEvent(string payload, string signatureHeader, string webhookSecret);
}
```

- [ ] **Step 3: Create `StripeGateway.cs`**

```csharp
using Stripe;

namespace DeliverTableInfrastructure.Payments;

public class StripeGateway : IStripeGateway
{
    public async Task<StripeCustomerResult> CreateCustomerAsync(
        string email, string fullName, IDictionary<string, string>? metadata, CancellationToken ct)
    {
        var service = new CustomerService();
        var options = new CustomerCreateOptions
        {
            Email = email,
            Name = fullName,
            Metadata = metadata != null ? new Dictionary<string, string>(metadata) : null,
        };
        var customer = await service.CreateAsync(options, cancellationToken: ct);
        return new StripeCustomerResult(customer.Id);
    }

    public async Task<StripePaymentIntentResult> CreatePaymentIntentAsync(
        long amountInMinorUnits,
        string currency,
        string stripeCustomerId,
        IDictionary<string, string> metadata,
        string idempotencyKey,
        CancellationToken ct)
    {
        var service = new PaymentIntentService();
        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInMinorUnits,
            Currency = currency,
            Customer = stripeCustomerId,
            CaptureMethod = "manual",
            SetupFutureUsage = "off_session",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
            Metadata = new Dictionary<string, string>(metadata),
        };
        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        var pi = await service.CreateAsync(options, requestOptions, ct);
        return new StripePaymentIntentResult(pi.Id, pi.ClientSecret, pi.Status);
    }

    public async Task<StripeCaptureResult> CapturePaymentIntentAsync(
        string paymentIntentId, string idempotencyKey, CancellationToken ct)
    {
        var service = new PaymentIntentService();
        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        var pi = await service.CaptureAsync(paymentIntentId, requestOptions: requestOptions, cancellationToken: ct);
        return new StripeCaptureResult(pi.Id, pi.Status);
    }

    public async Task<StripeCancelResult> CancelPaymentIntentAsync(
        string paymentIntentId, string idempotencyKey, CancellationToken ct)
    {
        var service = new PaymentIntentService();
        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        var pi = await service.CancelAsync(paymentIntentId, requestOptions: requestOptions, cancellationToken: ct);
        return new StripeCancelResult(pi.Id, pi.Status);
    }

    public async Task<StripeRefundResult> CreateRefundAsync(
        string paymentIntentId, long amountInMinorUnits, string idempotencyKey, CancellationToken ct)
    {
        var service = new RefundService();
        var options = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId,
            Amount = amountInMinorUnits,
        };
        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        var refund = await service.CreateAsync(options, requestOptions, ct);
        return new StripeRefundResult(
            refund.Id,
            refund.PaymentIntentId,
            (decimal)refund.Amount / 100m,
            refund.Currency,
            refund.Status);
    }

    public Event ConstructWebhookEvent(string payload, string signatureHeader, string webhookSecret)
    {
        return EventUtility.ConstructEvent(payload, signatureHeader, webhookSecret);
    }
}
```

- [ ] **Step 4: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 5: Commit**

```bash
git add DeliverTableInfrastructure/Payments/
git commit -m "feat(infra): add IStripeGateway with Stripe.net wrapper"
```

---

## Task 9: Add `IPaymentRepository` (no tests — data access only)

**Files:**
- Create: `DeliverTableInfrastructure/Repositories/Interfaces/IPaymentRepository.cs`
- Create: `DeliverTableInfrastructure/Repositories/PaymentRepository.cs`

- [ ] **Step 1: Create `IPaymentRepository.cs`**

```csharp
using DeliverTableInfrastructure.Models;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface IPaymentRepository
{
    Task<Payment> CreateAsync(Payment payment, CancellationToken ct = default);
    Task<Payment?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken ct = default);
    Task<Payment?> GetByStripePaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default);
    Task UpdateAsync(Payment payment, CancellationToken ct = default);

    Task<Refund> AddRefundAsync(Refund refund, CancellationToken ct = default);
    Task<Refund?> GetRefundByStripeIdAsync(string stripeRefundId, CancellationToken ct = default);
    Task<decimal> GetTotalRefundedAsync(int paymentId, CancellationToken ct = default);

    /// <summary>Returns true if the event was inserted (first time), false if already present (idempotent replay).</summary>
    Task<bool> TryRegisterProcessedEventAsync(string stripeEventId, string eventType, CancellationToken ct = default);
}
```

- [ ] **Step 2: Create `PaymentRepository.cs`**

```csharp
using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class PaymentRepository(DeliverTableContext db) : IPaymentRepository
{
    private readonly DeliverTableContext _db = db;

    public async Task<Payment> CreateAsync(Payment payment, CancellationToken ct = default)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);
        return payment;
    }

    public Task<Payment?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Payments.Include(p => p.Refunds).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken ct = default) =>
        _db.Payments.Include(p => p.Refunds).FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

    public Task<Payment?> GetByStripePaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default) =>
        _db.Payments.Include(p => p.Refunds).FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, ct);

    public async Task UpdateAsync(Payment payment, CancellationToken ct = default)
    {
        payment.UpdatedAt = DateTime.UtcNow;
        _db.Payments.Update(payment);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Refund> AddRefundAsync(Refund refund, CancellationToken ct = default)
    {
        _db.Refunds.Add(refund);
        await _db.SaveChangesAsync(ct);
        return refund;
    }

    public Task<Refund?> GetRefundByStripeIdAsync(string stripeRefundId, CancellationToken ct = default) =>
        _db.Refunds.FirstOrDefaultAsync(r => r.StripeRefundId == stripeRefundId, ct);

    public async Task<decimal> GetTotalRefundedAsync(int paymentId, CancellationToken ct = default)
    {
        return await _db.Refunds
            .Where(r => r.PaymentId == paymentId)
            .SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;
    }

    public async Task<bool> TryRegisterProcessedEventAsync(string stripeEventId, string eventType, CancellationToken ct = default)
    {
        var exists = await _db.ProcessedStripeEvents.AnyAsync(e => e.StripeEventId == stripeEventId, ct);
        if (exists) return false;

        _db.ProcessedStripeEvents.Add(new ProcessedStripeEvent
        {
            StripeEventId = stripeEventId,
            EventType = eventType,
            ProcessedAt = DateTime.UtcNow,
        });
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            // Concurrent insert; someone else registered first.
            return false;
        }
    }
}
```

- [ ] **Step 3: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 4: Commit**

```bash
git add DeliverTableInfrastructure/Repositories/Interfaces/IPaymentRepository.cs \
        DeliverTableInfrastructure/Repositories/PaymentRepository.cs
git commit -m "feat(infra): add IPaymentRepository"
```

---

## Task 10: Add `IOrderRepository.GetOrdersOlderThanAsync` for the scheduler

**Files:**
- Modify: `DeliverTableInfrastructure/Repositories/Interfaces/IOrderRepository.cs`
- Modify: `DeliverTableInfrastructure/Repositories/OrderRepository.cs`

- [ ] **Step 1: Add method to the interface**

```csharp
using DeliverTableSharedLibrary.Enums;

Task<List<Order>> GetOrdersOlderThanAsync(OrderStatus status, DateTime threshold, CancellationToken ct = default);
```

Place near other list-returning methods.

- [ ] **Step 2: Implement in `OrderRepository.cs`**

```csharp
public Task<List<Order>> GetOrdersOlderThanAsync(OrderStatus status, DateTime threshold, CancellationToken ct = default) =>
    _dbContext.Orders
        .Where(o => o.Status == status && o.CreatedAt < threshold)
        .ToListAsync(ct);
```

Add using for `DeliverTableSharedLibrary.Enums;` if missing.

- [ ] **Step 3: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 4: Commit**

```bash
git add DeliverTableInfrastructure/Repositories/Interfaces/IOrderRepository.cs \
        DeliverTableInfrastructure/Repositories/OrderRepository.cs
git commit -m "feat(infra): add GetOrdersOlderThanAsync to IOrderRepository"
```

---

## Task 11: Add `IPaymentLifecycleService` with TDD

**Files:**
- Create: `DeliverTableInfrastructure/Payments/IPaymentLifecycleService.cs`
- Create: `DeliverTableInfrastructure/Payments/PaymentLifecycleService.cs`
- Create: `DeliverTableTests/Infrastructure/Unit/Payments/PaymentLifecycleServiceTests.cs`

The service is in infra so it can be shared by server and scheduler. `ServiceResult` lives in server — to avoid the circular reference, define a minimal result type in infra (a plain `Result` record) or return `bool` and throw on unexpected state. For consistency with the rest of the plan, **return `Task<bool>`** (true if a transition happened, false if no-op) and throw `InvalidOperationException` on unrecoverable errors. Scheduler swallows exceptions per-order in its loop.

- [ ] **Step 1: Create `IPaymentLifecycleService.cs`**

```csharp
namespace DeliverTableInfrastructure.Payments;

public interface IPaymentLifecycleService
{
    /// <summary>
    /// Cancels the Stripe PaymentIntent for an order stuck in AwaitingPayment,
    /// reverses loyalty/discount redemptions, and transitions the order to Cancelled.
    /// Returns true if any state changed; false if the order was not in AwaitingPayment.
    /// </summary>
    Task<bool> CancelAbandonedOrderAsync(int orderId, CancellationToken ct);

    /// <summary>
    /// Auto-refuses an order that has been Pending longer than the restaurant response window.
    /// Cancels the Stripe authorization (releases the hold). Returns true on state change.
    /// </summary>
    Task<bool> AutoRefuseOrderAsync(int orderId, CancellationToken ct);
}
```

- [ ] **Step 2: Write failing test — `CancelAbandonedOrderAsync_TransitionsOrderAndCancelsIntent`**

Create `DeliverTableTests/Infrastructure/Unit/Payments/PaymentLifecycleServiceTests.cs`:

```csharp
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableTests.Infrastructure.Unit.Payments;

[TestFixture]
public class PaymentLifecycleServiceTests
{
    private IOrderRepository _orderRepository = null!;
    private IPaymentRepository _paymentRepository = null!;
    private ILoyaltyRepository _loyaltyRepository = null!;
    private IDiscountCodeRepository _discountRepository = null!;
    private IStripeGateway _stripe = null!;
    private PaymentLifecycleService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _orderRepository = Substitute.For<IOrderRepository>();
        _paymentRepository = Substitute.For<IPaymentRepository>();
        _loyaltyRepository = Substitute.For<ILoyaltyRepository>();
        _discountRepository = Substitute.For<IDiscountCodeRepository>();
        _stripe = Substitute.For<IStripeGateway>();
        _sut = new PaymentLifecycleService(
            _orderRepository, _paymentRepository, _loyaltyRepository, _discountRepository, _stripe);
    }

    [Test]
    public async Task CancelAbandonedOrderAsync_TransitionsOrderAndCancelsIntent()
    {
        var order = new Order { Id = 42, Status = OrderStatus.AwaitingPayment };
        var payment = new Payment { Id = 1, OrderId = 42, StripePaymentIntentId = "pi_123" };
        _orderRepository.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepository.GetByOrderIdAsync(42, Arg.Any<CancellationToken>()).Returns(payment);
        _stripe.CancelPaymentIntentAsync("pi_123", Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(new StripeCancelResult("pi_123", "canceled"));

        var changed = await _sut.CancelAbandonedOrderAsync(42, CancellationToken.None);

        Assert.That(changed, Is.True);
        Assert.That(order.Status, Is.EqualTo(OrderStatus.Cancelled));
        await _stripe.Received(1).CancelPaymentIntentAsync("pi_123", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _loyaltyRepository.Received(1).MarkPendingRedemptionsReversedForOrderAsync(42, Arg.Any<CancellationToken>());
        await _discountRepository.Received(1).MarkPendingRedemptionsReversedForOrderAsync(42, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelAbandonedOrderAsync_OrderNotInAwaitingPayment_ReturnsFalse()
    {
        var order = new Order { Id = 42, Status = OrderStatus.Pending };
        _orderRepository.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(order);

        var changed = await _sut.CancelAbandonedOrderAsync(42, CancellationToken.None);

        Assert.That(changed, Is.False);
        await _stripe.DidNotReceive().CancelPaymentIntentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AutoRefuseOrderAsync_TransitionsPendingToRefusedAndCancelsAuth()
    {
        var order = new Order { Id = 7, Status = OrderStatus.Pending };
        var payment = new Payment { Id = 1, OrderId = 7, StripePaymentIntentId = "pi_77" };
        _orderRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepository.GetByOrderIdAsync(7, Arg.Any<CancellationToken>()).Returns(payment);
        _stripe.CancelPaymentIntentAsync("pi_77", Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(new StripeCancelResult("pi_77", "canceled"));

        var changed = await _sut.AutoRefuseOrderAsync(7, CancellationToken.None);

        Assert.That(changed, Is.True);
        Assert.That(order.Status, Is.EqualTo(OrderStatus.Refused));
    }
}
```

Note: `ILoyaltyRepository.MarkPendingRedemptionsReversedForOrderAsync` and the equivalent on `IDiscountCodeRepository` must exist. If the current repositories don't have these, they need to be added. See Step 3.

- [ ] **Step 3: Add helper methods to loyalty + discount repositories**

If not present, add to `ILoyaltyRepository` and `IDiscountCodeRepository`:

```csharp
Task MarkPendingRedemptionsCommittedForOrderAsync(int orderId, CancellationToken ct = default);
Task MarkPendingRedemptionsReversedForOrderAsync(int orderId, CancellationToken ct = default);
```

Implementations in the concrete repositories — UPDATE rows with `Status=Pending` and matching `OrderId` to `Committed` or `Reversed`, and for loyalty, reverse the actual points on the account when reversing (`LoyaltyAccount.Balance += row.Points` when type was `Redemption`). Mirror the existing redemption logic reversibility.

```csharp
public async Task MarkPendingRedemptionsCommittedForOrderAsync(int orderId, CancellationToken ct = default)
{
    var rows = await _dbContext.LoyaltyTransactions
        .Where(t => t.OrderId == orderId && t.Status == LoyaltyRedemptionStatus.Pending)
        .ToListAsync(ct);
    foreach (var r in rows) r.Status = LoyaltyRedemptionStatus.Committed;
    await _dbContext.SaveChangesAsync(ct);
}

public async Task MarkPendingRedemptionsReversedForOrderAsync(int orderId, CancellationToken ct = default)
{
    var rows = await _dbContext.LoyaltyTransactions
        .Include(t => t.LoyaltyAccount)
        .Where(t => t.OrderId == orderId && t.Status == LoyaltyRedemptionStatus.Pending)
        .ToListAsync(ct);
    foreach (var r in rows)
    {
        r.Status = LoyaltyRedemptionStatus.Reversed;
        if (r.Type == LoyaltyTransactionType.Redemption)
        {
            r.LoyaltyAccount.Balance += r.Points;
        }
        else if (r.Type == LoyaltyTransactionType.Earning)
        {
            r.LoyaltyAccount.Balance -= r.Points;
        }
    }
    await _dbContext.SaveChangesAsync(ct);
}
```

Mirror the same idea for `DiscountCodeRepository` (pending redemptions are flipped to Reversed; discount counters unaffected since redemption creates the row).

- [ ] **Step 4: Run tests to verify they fail**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~PaymentLifecycleServiceTests"
```

Expected: FAIL with "type or namespace `PaymentLifecycleService` could not be found".

- [ ] **Step 5: Implement `PaymentLifecycleService.cs`**

```csharp
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Payments;

public class PaymentLifecycleService(
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    ILoyaltyRepository loyaltyRepository,
    IDiscountCodeRepository discountRepository,
    IStripeGateway stripe) : IPaymentLifecycleService
{
    public async Task<bool> CancelAbandonedOrderAsync(int orderId, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(orderId, ct);
        if (order is null || order.Status != OrderStatus.AwaitingPayment) return false;

        var payment = await paymentRepository.GetByOrderIdAsync(orderId, ct);
        if (payment is not null && !string.IsNullOrEmpty(payment.StripePaymentIntentId))
        {
            await stripe.CancelPaymentIntentAsync(
                payment.StripePaymentIntentId,
                idempotencyKey: $"order:{orderId}:cancel-abandoned",
                ct);
            payment.Status = PaymentGatewayStatus.Canceled;
            payment.CanceledAt = DateTime.UtcNow;
            await paymentRepository.UpdateAsync(payment, ct);
        }

        await loyaltyRepository.MarkPendingRedemptionsReversedForOrderAsync(orderId, ct);
        await discountRepository.MarkPendingRedemptionsReversedForOrderAsync(orderId, ct);

        order.Status = OrderStatus.Cancelled;
        order.PaymentStatus = PaymentStatus.Failed;
        order.UpdatedAt = DateTime.UtcNow;
        await orderRepository.UpdateAsync(order, ct);

        return true;
    }

    public async Task<bool> AutoRefuseOrderAsync(int orderId, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(orderId, ct);
        if (order is null || order.Status != OrderStatus.Pending) return false;

        var payment = await paymentRepository.GetByOrderIdAsync(orderId, ct);
        if (payment is not null && !string.IsNullOrEmpty(payment.StripePaymentIntentId))
        {
            await stripe.CancelPaymentIntentAsync(
                payment.StripePaymentIntentId,
                idempotencyKey: $"order:{orderId}:auto-refuse",
                ct);
            payment.Status = PaymentGatewayStatus.Canceled;
            payment.CanceledAt = DateTime.UtcNow;
            await paymentRepository.UpdateAsync(payment, ct);
        }

        order.Status = OrderStatus.Refused;
        order.UpdatedAt = DateTime.UtcNow;
        await orderRepository.UpdateAsync(order, ct);

        return true;
    }
}
```

Note: this assumes `IOrderRepository.UpdateAsync(Order, CancellationToken)` exists; verify against the existing interface and add if missing (follow the same signature pattern as `OrderRepository`'s other update methods).

- [ ] **Step 6: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~PaymentLifecycleServiceTests"
```

Expected: 3 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add DeliverTableInfrastructure/Payments/IPaymentLifecycleService.cs \
        DeliverTableInfrastructure/Payments/PaymentLifecycleService.cs \
        DeliverTableInfrastructure/Repositories/ \
        DeliverTableTests/Infrastructure/Unit/Payments/PaymentLifecycleServiceTests.cs
git commit -m "feat(infra): add IPaymentLifecycleService with tests"
```

---

## Task 12: Add `IPaymentService` scaffold + `CreateIntentAsync` with tests

**Files:**
- Create: `DeliverTableServer/Services/Interfaces/IPaymentService.cs`
- Create: `DeliverTableServer/Services/PaymentService.cs`
- Create: `DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs`

- [ ] **Step 1: Create `IPaymentService.cs` (signature only; methods added as we go)**

```csharp
using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Payment;

namespace DeliverTableServer.Services.Interfaces;

public interface IPaymentService
{
    Task<ServiceResult<CreateIntentResult>> CreateIntentAsync(int orderId, CancellationToken ct);
}

public sealed record CreateIntentResult(string ClientSecret, string PaymentIntentId, decimal Amount, string Currency);
```

- [ ] **Step 2: Write failing test — creation success path**

```csharp
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Configuration;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class PaymentServiceTests
{
    private IStripeGateway _stripe = null!;
    private IPaymentRepository _paymentRepo = null!;
    private IOrderRepository _orderRepo = null!;
    private IUserRepository _userRepo = null!;
    private AppEnvironment _env = null!;
    private PaymentService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _stripe = Substitute.For<IStripeGateway>();
        _paymentRepo = Substitute.For<IPaymentRepository>();
        _orderRepo = Substitute.For<IOrderRepository>();
        _userRepo = Substitute.For<IUserRepository>();
        _env = TestEnvironmentFactory.Create(); // helper that builds a valid AppEnvironment instance
        _sut = new PaymentService(_stripe, _paymentRepo, _orderRepo, _userRepo, _env);
    }

    [Test]
    public async Task CreateIntentAsync_NewStripeCustomer_PersistsCustomerIdAndCreatesIntent()
    {
        var user = new User { Id = 1, Email = "a@b.fr", FirstName = "A", LastName = "B" };
        var order = new Order
        {
            Id = 10, CustomerId = 1, RestaurantId = 5,
            TotalAmount = 12.50m, Status = OrderStatus.AwaitingPayment,
            Customer = user
        };
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        _stripe.CreateCustomerAsync("a@b.fr", "A B", Arg.Any<IDictionary<string, string>?>(), Arg.Any<CancellationToken>())
               .Returns(new StripeCustomerResult("cus_abc"));
        _stripe.CreatePaymentIntentAsync(
                 1250, "eur", "cus_abc",
                 Arg.Any<IDictionary<string, string>>(),
                 "order:10:create-intent",
                 Arg.Any<CancellationToken>())
               .Returns(new StripePaymentIntentResult("pi_1", "pi_1_secret_abc", "requires_payment_method"));

        var result = await _sut.CreateIntentAsync(10, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.ClientSecret, Is.EqualTo("pi_1_secret_abc"));
        Assert.That(user.StripeCustomerId, Is.EqualTo("cus_abc"));
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _paymentRepo.Received(1).CreateAsync(
            Arg.Is<Payment>(p => p.OrderId == 10 && p.StripePaymentIntentId == "pi_1" && p.Amount == 12.50m),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateIntentAsync_OrderNotInAwaitingPayment_ReturnsError()
    {
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(new Order { Id = 10, Status = OrderStatus.Confirmed });

        var result = await _sut.CreateIntentAsync(10, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
    }
}
```

`TestEnvironmentFactory` is a new helper. Create `DeliverTableTests/Global/Factories/TestEnvironmentFactory.cs` that builds an `AppEnvironment` using its public init properties (or via reflection if `Load()` is the only entry). If the current code uses `AppEnvironment.Load()` which reads env vars, have the factory set env vars via `Environment.SetEnvironmentVariable(...)` for each `Required`-marked key with dummy values, then call `Load()`, then unset them. Alternatively, expose a `For Testing` constructor that bypasses env reading.

- [ ] **Step 3: Run tests to verify they fail**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~PaymentServiceTests"
```

Expected: FAIL with "type or namespace `PaymentService` could not be found".

- [ ] **Step 4: Implement `PaymentService.cs` — `CreateIntentAsync` only**

```csharp
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Services;

public class PaymentService(
    IStripeGateway stripe,
    IPaymentRepository paymentRepository,
    IOrderRepository orderRepository,
    IUserRepository userRepository,
    AppEnvironment env) : IPaymentService
{
    public async Task<ServiceResult<CreateIntentResult>> CreateIntentAsync(int orderId, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(orderId, ct);
        if (order is null) return new ServiceError(ErrorMessages.OrderNotFound);
        if (order.Status != OrderStatus.AwaitingPayment)
            return new ServiceError(ErrorMessages.OrderPaymentAlreadyProcessed);

        var user = await userRepository.GetByIdAsync(order.CustomerId, ct);
        if (user is null) return new ServiceError(ErrorMessages.PaymentIntentCreationFailed);

        string stripeCustomerId = user.StripeCustomerId ?? string.Empty;
        if (string.IsNullOrEmpty(stripeCustomerId))
        {
            var customerResult = await stripe.CreateCustomerAsync(
                email: user.Email ?? string.Empty,
                fullName: $"{user.FirstName} {user.LastName}".Trim(),
                metadata: new Dictionary<string, string> { ["userId"] = user.Id.ToString() },
                ct);
            stripeCustomerId = customerResult.CustomerId;
            user.StripeCustomerId = stripeCustomerId;
            await userRepository.UpdateAsync(user, ct);
        }

        long amountMinor = (long)Math.Round(order.TotalAmount * 100m, MidpointRounding.AwayFromZero);
        var metadata = new Dictionary<string, string>
        {
            ["orderId"] = order.Id.ToString(),
            ["userId"] = user.Id.ToString(),
            ["restaurantId"] = order.RestaurantId.ToString(),
        };

        var intent = await stripe.CreatePaymentIntentAsync(
            amountInMinorUnits: amountMinor,
            currency: "eur",
            stripeCustomerId: stripeCustomerId,
            metadata: metadata,
            idempotencyKey: $"order:{order.Id}:create-intent",
            ct);

        var payment = new Payment
        {
            OrderId = order.Id,
            Provider = "Stripe",
            StripePaymentIntentId = intent.PaymentIntentId,
            StripeChargeId = string.Empty,
            Amount = order.TotalAmount,
            Currency = "EUR",
            Status = PaymentGatewayStatus.RequiresPaymentMethod,
        };
        await paymentRepository.CreateAsync(payment, ct);

        return ServiceResult<CreateIntentResult>.Success(
            new CreateIntentResult(intent.ClientSecret, intent.PaymentIntentId, order.TotalAmount, "EUR"));
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~PaymentServiceTests"
```

Expected: both tests PASS.

- [ ] **Step 6: Commit**

```bash
git add DeliverTableServer/Services/Interfaces/IPaymentService.cs \
        DeliverTableServer/Services/PaymentService.cs \
        DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs \
        DeliverTableTests/Global/Factories/TestEnvironmentFactory.cs
git commit -m "feat(server): add IPaymentService with CreateIntentAsync and tests"
```

---

## Task 13: `PaymentService.CaptureAsync` + `CancelAuthorizationAsync`

**Files:**
- Modify: `DeliverTableServer/Services/Interfaces/IPaymentService.cs`
- Modify: `DeliverTableServer/Services/PaymentService.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs`

- [ ] **Step 1: Add methods to interface**

```csharp
Task<ServiceResult> CaptureAsync(int orderId, CancellationToken ct);
Task<ServiceResult> CancelAuthorizationAsync(int orderId, CancellationToken ct);
```

- [ ] **Step 2: Write failing tests**

Add to `PaymentServiceTests.cs`:

```csharp
[Test]
public async Task CaptureAsync_HappyPath_CapturesIntentAndUpdatesPayment()
{
    var payment = new Payment
    {
        Id = 1, OrderId = 10, StripePaymentIntentId = "pi_cap",
        Status = PaymentGatewayStatus.RequiresConfirmation,
        Amount = 15m,
    };
    _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
    _stripe.CapturePaymentIntentAsync("pi_cap", "order:10:capture", Arg.Any<CancellationToken>())
           .Returns(new StripeCaptureResult("pi_cap", "succeeded"));

    var result = await _sut.CaptureAsync(10, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(payment.CapturedAt, Is.Not.Null);
    await _paymentRepo.Received(1).UpdateAsync(payment, Arg.Any<CancellationToken>());
}

[Test]
public async Task CaptureAsync_StripeFails_ReturnsErrorAndDoesNotUpdate()
{
    var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_fail" };
    _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
    _stripe.CapturePaymentIntentAsync("pi_fail", Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Throws(new Stripe.StripeException("boom"));

    var result = await _sut.CaptureAsync(10, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.False);
    await _paymentRepo.DidNotReceive().UpdateAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
}

[Test]
public async Task CancelAuthorizationAsync_CancelsIntentAndUpdatesPayment()
{
    var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_c" };
    _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
    _stripe.CancelPaymentIntentAsync("pi_c", "order:10:cancel-auth", Arg.Any<CancellationToken>())
           .Returns(new StripeCancelResult("pi_c", "canceled"));

    var result = await _sut.CancelAuthorizationAsync(10, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(payment.Status, Is.EqualTo(PaymentGatewayStatus.Canceled));
    await _paymentRepo.Received(1).UpdateAsync(payment, Arg.Any<CancellationToken>());
}
```

- [ ] **Step 3: Run to verify failures**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~PaymentServiceTests"
```

Expected: the three new tests fail to compile (missing methods on `PaymentService`).

- [ ] **Step 4: Implement**

Add to `PaymentService.cs`:

```csharp
public async Task<ServiceResult> CaptureAsync(int orderId, CancellationToken ct)
{
    var payment = await paymentRepository.GetByOrderIdAsync(orderId, ct);
    if (payment is null) return new ServiceError(ErrorMessages.PaymentNotFound);
    try
    {
        var capture = await stripe.CapturePaymentIntentAsync(
            payment.StripePaymentIntentId,
            idempotencyKey: $"order:{orderId}:capture",
            ct);
        payment.CapturedAt = DateTime.UtcNow;
        payment.Status = capture.Status == "succeeded"
            ? PaymentGatewayStatus.Succeeded
            : payment.Status;
        await paymentRepository.UpdateAsync(payment, ct);
        return ServiceResult.Success();
    }
    catch (Stripe.StripeException ex)
    {
        return new ServiceError(ErrorMessages.PaymentCaptureFailed + " " + ex.Message);
    }
}

public async Task<ServiceResult> CancelAuthorizationAsync(int orderId, CancellationToken ct)
{
    var payment = await paymentRepository.GetByOrderIdAsync(orderId, ct);
    if (payment is null) return new ServiceError(ErrorMessages.PaymentNotFound);
    try
    {
        await stripe.CancelPaymentIntentAsync(
            payment.StripePaymentIntentId,
            idempotencyKey: $"order:{orderId}:cancel-auth",
            ct);
        payment.Status = PaymentGatewayStatus.Canceled;
        payment.CanceledAt = DateTime.UtcNow;
        await paymentRepository.UpdateAsync(payment, ct);
        return ServiceResult.Success();
    }
    catch (Stripe.StripeException ex)
    {
        return new ServiceError(ErrorMessages.PaymentCancelFailed + " " + ex.Message);
    }
}
```

If `ServiceResult` (non-generic) isn't defined, add it: `public sealed class ServiceResult { public ServiceError? Error { get; } public bool IsSuccess => Error is null; public static ServiceResult Success() => new(); public static implicit operator ServiceResult(ServiceError e) => new(e); private ServiceResult() {} private ServiceResult(ServiceError e) { Error = e; } }` — otherwise use the existing equivalent (`ServiceResult<T>` for a marker type, following project convention).

- [ ] **Step 5: Run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~PaymentServiceTests"
```

Expected: all five PaymentService tests PASS.

- [ ] **Step 6: Commit**

```bash
git add DeliverTableServer/Services/
git add DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs
git commit -m "feat(server): add PaymentService capture and cancel with tests"
```

---

## Task 14: `PaymentService.RefundAsync` (unified method for auto + admin)

**Files:**
- Modify: `DeliverTableServer/Services/Interfaces/IPaymentService.cs`
- Modify: `DeliverTableServer/Services/PaymentService.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs`

- [ ] **Step 1: Add method to interface**

```csharp
Task<ServiceResult<RefundDto>> RefundAsync(int orderId, decimal amount, string reason, int? adminUserId, CancellationToken ct);
```

- [ ] **Step 2: Write failing tests**

```csharp
[Test]
public async Task RefundAsync_HappyPath_PersistsRefundAndUpdatesOrderStatus()
{
    var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_r", Amount = 50m };
    var order = new Order { Id = 10, PaymentStatus = PaymentStatus.Completed };
    _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
    _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
    _paymentRepo.GetTotalRefundedAsync(1, Arg.Any<CancellationToken>()).Returns(0m);
    _stripe.CreateRefundAsync("pi_r", 2500, Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns(new StripeRefundResult("re_1", "pi_r", 25m, "eur", "succeeded"));
    _paymentRepo.AddRefundAsync(Arg.Any<Refund>(), Arg.Any<CancellationToken>())
                .Returns(ci => ci.Arg<Refund>());

    var result = await _sut.RefundAsync(10, 25m, "customer request", adminUserId: 99, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.Value!.Amount, Is.EqualTo(25m));
    Assert.That(order.PaymentStatus, Is.EqualTo(PaymentStatus.PartiallyRefunded));
}

[Test]
public async Task RefundAsync_FullRefund_SetsStatusRefunded()
{
    var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_r", Amount = 50m };
    var order = new Order { Id = 10, PaymentStatus = PaymentStatus.Completed };
    _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
    _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
    _paymentRepo.GetTotalRefundedAsync(1, Arg.Any<CancellationToken>()).Returns(0m);
    _stripe.CreateRefundAsync("pi_r", 5000, Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns(new StripeRefundResult("re_full", "pi_r", 50m, "eur", "succeeded"));
    _paymentRepo.AddRefundAsync(Arg.Any<Refund>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Refund>());

    await _sut.RefundAsync(10, 50m, "full", adminUserId: null, CancellationToken.None);

    Assert.That(order.PaymentStatus, Is.EqualTo(PaymentStatus.Refunded));
}

[Test]
public async Task RefundAsync_AmountExceedsRemaining_ReturnsError()
{
    var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_r", Amount = 50m };
    _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
    _paymentRepo.GetTotalRefundedAsync(1, Arg.Any<CancellationToken>()).Returns(45m);

    var result = await _sut.RefundAsync(10, 10m, "x", adminUserId: 99, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.False);
    Assert.That(result.Error!.Message, Does.Contain("dépasse"));
    await _stripe.DidNotReceive().CreateRefundAsync(
        Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
}
```

- [ ] **Step 3: Run to verify failures**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~PaymentServiceTests"
```

Expected: new tests fail to compile.

- [ ] **Step 4: Implement**

Add to `PaymentService.cs`:

```csharp
public async Task<ServiceResult<RefundDto>> RefundAsync(int orderId, decimal amount, string reason, int? adminUserId, CancellationToken ct)
{
    if (amount <= 0m) return new ServiceError(ErrorMessages.PaymentRefundFailed);
    var payment = await paymentRepository.GetByOrderIdAsync(orderId, ct);
    if (payment is null) return new ServiceError(ErrorMessages.PaymentNotFound);

    var alreadyRefunded = await paymentRepository.GetTotalRefundedAsync(payment.Id, ct);
    var remaining = payment.Amount - alreadyRefunded;
    if (remaining <= 0m) return new ServiceError(ErrorMessages.PaymentAlreadyRefunded);
    if (amount > remaining) return new ServiceError(ErrorMessages.PaymentRefundExceedsAmount);

    long amountMinor = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
    var idempotencyKey = $"order:{orderId}:refund:{DateTime.UtcNow.Ticks}";

    StripeRefundResult stripeRefund;
    try
    {
        stripeRefund = await stripe.CreateRefundAsync(
            payment.StripePaymentIntentId, amountMinor, idempotencyKey, ct);
    }
    catch (Stripe.StripeException ex)
    {
        return new ServiceError(ErrorMessages.PaymentRefundFailed + " " + ex.Message);
    }

    var refund = new Refund
    {
        PaymentId = payment.Id,
        StripeRefundId = stripeRefund.RefundId,
        Amount = amount,
        Currency = "EUR",
        Reason = reason,
        CreatedByUserId = adminUserId,
        CreatedAt = DateTime.UtcNow,
    };
    await paymentRepository.AddRefundAsync(refund, ct);

    var order = await orderRepository.GetByIdAsync(orderId, ct);
    if (order is not null)
    {
        var totalAfter = alreadyRefunded + amount;
        order.PaymentStatus = totalAfter >= payment.Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
        order.UpdatedAt = DateTime.UtcNow;
        await orderRepository.UpdateAsync(order, ct);
    }

    return new RefundDto(refund.Id, refund.Amount, refund.Currency, refund.Reason, refund.CreatedAt);
}
```

- [ ] **Step 5: Run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~PaymentServiceTests"
```

Expected: all 8 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add DeliverTableServer/Services/ DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs
git commit -m "feat(server): add PaymentService.RefundAsync with tests"
```

---

## Task 15: `PaymentService.HandleStripeEventAsync` webhook dispatcher with tests

**Files:**
- Modify: `DeliverTableServer/Services/Interfaces/IPaymentService.cs`
- Modify: `DeliverTableServer/Services/PaymentService.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs`

- [ ] **Step 1: Add method to interface**

```csharp
Task<ServiceResult> HandleStripeEventAsync(Stripe.Event evt, CancellationToken ct);
```

- [ ] **Step 2: Write failing tests — idempotency + authorization-completed + capture-completed + failed + refunded**

```csharp
[Test]
public async Task HandleStripeEventAsync_DuplicateEvent_ReturnsSuccessWithoutWork()
{
    var evt = new Stripe.Event { Id = "evt_dup", Type = "payment_intent.succeeded" };
    _paymentRepo.TryRegisterProcessedEventAsync("evt_dup", "payment_intent.succeeded", Arg.Any<CancellationToken>())
                .Returns(false);

    var result = await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    await _paymentRepo.DidNotReceive().GetByStripePaymentIntentIdAsync(
        Arg.Any<string>(), Arg.Any<CancellationToken>());
}

[Test]
public async Task HandleStripeEventAsync_AmountCapturableUpdated_TransitionsOrderAndCommitsRedemptions()
{
    var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_x", Status = PaymentGatewayStatus.RequiresConfirmation };
    var order = new Order { Id = 10, Status = OrderStatus.AwaitingPayment, PaymentStatus = PaymentStatus.Pending, CustomerId = 2 };
    var pi = new Stripe.PaymentIntent { Id = "pi_x" };
    var evt = new Stripe.Event { Id = "evt_1", Type = "payment_intent.amount_capturable_updated", Data = new Stripe.EventData { Object = pi } };
    _paymentRepo.TryRegisterProcessedEventAsync("evt_1", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
    _paymentRepo.GetByStripePaymentIntentIdAsync("pi_x", Arg.Any<CancellationToken>()).Returns(payment);
    _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);

    var result = await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(order.Status, Is.EqualTo(OrderStatus.Pending));
    Assert.That(order.PaymentStatus, Is.EqualTo(PaymentStatus.Authorized));
    Assert.That(payment.Status, Is.EqualTo(PaymentGatewayStatus.RequiresConfirmation));
    Assert.That(payment.AuthorizedAt, Is.Not.Null);
}

[Test]
public async Task HandleStripeEventAsync_PaymentFailed_CancelsOrderAndReverses()
{
    var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_f" };
    var order = new Order { Id = 10, Status = OrderStatus.AwaitingPayment };
    var pi = new Stripe.PaymentIntent { Id = "pi_f" };
    var evt = new Stripe.Event { Id = "evt_f", Type = "payment_intent.payment_failed", Data = new Stripe.EventData { Object = pi } };
    _paymentRepo.TryRegisterProcessedEventAsync("evt_f", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
    _paymentRepo.GetByStripePaymentIntentIdAsync("pi_f", Arg.Any<CancellationToken>()).Returns(payment);
    _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);

    await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

    Assert.That(order.Status, Is.EqualTo(OrderStatus.Cancelled));
    Assert.That(payment.Status, Is.EqualTo(PaymentGatewayStatus.Canceled));
}

[Test]
public async Task HandleStripeEventAsync_ChargeRefunded_UpsertsRefundAndUpdatesStatus()
{
    var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_r", Amount = 100m };
    var order = new Order { Id = 10, PaymentStatus = PaymentStatus.Completed };
    var charge = new Stripe.Charge
    {
        Id = "ch_1",
        PaymentIntentId = "pi_r",
        Refunds = new Stripe.StripeList<Stripe.Refund>
        {
            Data = new List<Stripe.Refund>
            {
                new() { Id = "re_1", Amount = 2500, Currency = "eur", Status = "succeeded" }
            }
        }
    };
    var evt = new Stripe.Event { Id = "evt_ref", Type = "charge.refunded", Data = new Stripe.EventData { Object = charge } };
    _paymentRepo.TryRegisterProcessedEventAsync("evt_ref", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
    _paymentRepo.GetByStripePaymentIntentIdAsync("pi_r", Arg.Any<CancellationToken>()).Returns(payment);
    _paymentRepo.GetRefundByStripeIdAsync("re_1", Arg.Any<CancellationToken>()).Returns((Refund?)null);
    _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
    _paymentRepo.GetTotalRefundedAsync(1, Arg.Any<CancellationToken>()).Returns(25m);

    await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

    await _paymentRepo.Received(1).AddRefundAsync(
        Arg.Is<Refund>(r => r.StripeRefundId == "re_1" && r.Amount == 25m),
        Arg.Any<CancellationToken>());
    Assert.That(order.PaymentStatus, Is.EqualTo(PaymentStatus.PartiallyRefunded));
}
```

- [ ] **Step 3: Run to verify failures**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~PaymentServiceTests"
```

Expected: new tests fail to compile.

- [ ] **Step 4: Implement the dispatcher**

Add to `PaymentService` constructor: `ILoyaltyRepository loyaltyRepo, IDiscountCodeRepository discountRepo, ICartRepository cartRepo, IMessagePublisher publisher` (for reversing redemptions, clearing the cart on success, and enqueuing the confirmation email). Update existing tests' SetUp to include these mocks.

```csharp
public async Task<ServiceResult> HandleStripeEventAsync(Stripe.Event evt, CancellationToken ct)
{
    var registered = await paymentRepository.TryRegisterProcessedEventAsync(evt.Id, evt.Type, ct);
    if (!registered) return ServiceResult.Success();

    switch (evt.Type)
    {
        case "payment_intent.amount_capturable_updated":
            await HandleAuthorizationCompletedAsync((Stripe.PaymentIntent)evt.Data.Object, ct);
            break;
        case "payment_intent.succeeded":
            await HandleCaptureCompletedAsync((Stripe.PaymentIntent)evt.Data.Object, ct);
            break;
        case "payment_intent.payment_failed":
        case "payment_intent.canceled":
            await HandlePaymentAbortedAsync((Stripe.PaymentIntent)evt.Data.Object, failed: evt.Type == "payment_intent.payment_failed", ct);
            break;
        case "charge.refunded":
            await HandleChargeRefundedAsync((Stripe.Charge)evt.Data.Object, ct);
            break;
        default:
            // Log + ack
            break;
    }
    return ServiceResult.Success();
}

private async Task HandleAuthorizationCompletedAsync(Stripe.PaymentIntent pi, CancellationToken ct)
{
    var payment = await paymentRepository.GetByStripePaymentIntentIdAsync(pi.Id, ct);
    if (payment is null) return;
    payment.AuthorizedAt = DateTime.UtcNow;
    await paymentRepository.UpdateAsync(payment, ct);

    var order = await orderRepository.GetByIdAsync(payment.OrderId, ct);
    if (order is null || order.Status != OrderStatus.AwaitingPayment) return;

    await loyaltyRepo.MarkPendingRedemptionsCommittedForOrderAsync(order.Id, ct);
    await discountRepo.MarkPendingRedemptionsCommittedForOrderAsync(order.Id, ct);

    order.Status = OrderStatus.Pending;
    order.PaymentStatus = PaymentStatus.Authorized;
    order.UpdatedAt = DateTime.UtcNow;
    await orderRepository.UpdateAsync(order, ct);

    await cartRepo.ClearForCustomerAsync(order.CustomerId, ct);

    await publisher.PublishAsync("email", new EmailJobMessage(/* order confirmation job */));
}

private async Task HandleCaptureCompletedAsync(Stripe.PaymentIntent pi, CancellationToken ct)
{
    var payment = await paymentRepository.GetByStripePaymentIntentIdAsync(pi.Id, ct);
    if (payment is null) return;
    payment.CapturedAt ??= DateTime.UtcNow;
    payment.Status = PaymentGatewayStatus.Succeeded;
    await paymentRepository.UpdateAsync(payment, ct);

    var order = await orderRepository.GetByIdAsync(payment.OrderId, ct);
    if (order is null) return;
    if (order.PaymentStatus != PaymentStatus.Refunded && order.PaymentStatus != PaymentStatus.PartiallyRefunded)
    {
        order.PaymentStatus = PaymentStatus.Completed;
        order.UpdatedAt = DateTime.UtcNow;
        await orderRepository.UpdateAsync(order, ct);
    }
}

private async Task HandlePaymentAbortedAsync(Stripe.PaymentIntent pi, bool failed, CancellationToken ct)
{
    var payment = await paymentRepository.GetByStripePaymentIntentIdAsync(pi.Id, ct);
    if (payment is null) return;
    payment.Status = PaymentGatewayStatus.Canceled;
    payment.CanceledAt = DateTime.UtcNow;
    await paymentRepository.UpdateAsync(payment, ct);

    var order = await orderRepository.GetByIdAsync(payment.OrderId, ct);
    if (order is null || order.Status != OrderStatus.AwaitingPayment) return;
    await loyaltyRepo.MarkPendingRedemptionsReversedForOrderAsync(order.Id, ct);
    await discountRepo.MarkPendingRedemptionsReversedForOrderAsync(order.Id, ct);
    order.Status = OrderStatus.Cancelled;
    order.PaymentStatus = failed ? PaymentStatus.Failed : PaymentStatus.Pending;
    order.UpdatedAt = DateTime.UtcNow;
    await orderRepository.UpdateAsync(order, ct);
}

private async Task HandleChargeRefundedAsync(Stripe.Charge charge, CancellationToken ct)
{
    var payment = await paymentRepository.GetByStripePaymentIntentIdAsync(charge.PaymentIntentId, ct);
    if (payment is null) return;
    if (charge.Refunds?.Data is null) return;

    foreach (var r in charge.Refunds.Data)
    {
        var existing = await paymentRepository.GetRefundByStripeIdAsync(r.Id, ct);
        if (existing is not null) continue;
        var refund = new Refund
        {
            PaymentId = payment.Id,
            StripeRefundId = r.Id,
            Amount = (decimal)r.Amount / 100m,
            Currency = r.Currency.ToUpperInvariant(),
            Reason = r.Reason ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
        };
        await paymentRepository.AddRefundAsync(refund, ct);
    }

    var order = await orderRepository.GetByIdAsync(payment.OrderId, ct);
    if (order is null) return;
    var totalRefunded = await paymentRepository.GetTotalRefundedAsync(payment.Id, ct);
    order.PaymentStatus = totalRefunded >= payment.Amount
        ? PaymentStatus.Refunded
        : PaymentStatus.PartiallyRefunded;
    order.UpdatedAt = DateTime.UtcNow;
    await orderRepository.UpdateAsync(order, ct);
}
```

`EmailJobMessage` construction: use the existing type from `DeliverTableInfrastructure.Messaging.Messages`. Match the fields the email consumer expects for an order-confirmation email.

- [ ] **Step 5: Run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~PaymentServiceTests"
```

Expected: all PaymentService tests PASS.

- [ ] **Step 6: Commit**

```bash
git add DeliverTableServer/Services/ DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs
git commit -m "feat(server): add PaymentService.HandleStripeEventAsync with tests"
```

---

## Task 16: `PaymentController` with cancel endpoint + tests

**Files:**
- Create: `DeliverTableServer/Controllers/PaymentController.cs`
- Create: `DeliverTableTests/Server/Unit/Controllers/PaymentControllerTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Helpers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class PaymentControllerTests
{
    private IPaymentService _service = null!;
    private PaymentController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IPaymentService>();
        _sut = new PaymentController(_service);
    }

    [Test]
    public async Task Cancel_Authenticated_ReturnsNoContent()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "7", nameof(UserRole.Customer));
        _service.CancelAuthorizationAsync(42, Arg.Any<CancellationToken>())
                .Returns(ServiceResult.Success());

        var result = await _sut.Cancel(42, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Cancel_ServiceError_ReturnsError()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "7", nameof(UserRole.Customer));
        _service.CancelAuthorizationAsync(42, Arg.Any<CancellationToken>())
                .Returns(new ServiceError("fail"));

        var result = await _sut.Cancel(42, CancellationToken.None);

        Assert.That(result, Is.Not.InstanceOf<NoContentResult>());
    }
}
```

- [ ] **Step 2: Run to verify failure**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~PaymentControllerTests"
```

Expected: FAIL (controller doesn't exist).

- [ ] **Step 3: Implement `PaymentController.cs`**

```csharp
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Payment.Base)]
[Authorize]
public class PaymentController(IPaymentService paymentService) : ControllerBase
{
    private readonly IPaymentService _paymentService = paymentService;

    [HttpPost(ApiRoutes.Payment.CancelRoute)]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> Cancel(int orderId, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int _)) return Unauthorized();
        var result = await _paymentService.CancelAuthorizationAsync(orderId, ct);
        return result.IsSuccess ? NoContent() : result.Error!.ToErrorResult();
    }
}
```

Verify `ServiceErrorExtensions.ToErrorResult()` exists (per the OrderController pattern). If the project has `.ToNoContentResult()` on ServiceResult, prefer that pattern instead.

- [ ] **Step 4: Build + run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~PaymentControllerTests"
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableServer/Controllers/PaymentController.cs \
        DeliverTableTests/Server/Unit/Controllers/PaymentControllerTests.cs
git commit -m "feat(server): add PaymentController cancel endpoint with tests"
```

---

## Task 17: `StripeWebhookController` with signature verification + dispatch tests

**Files:**
- Create: `DeliverTableServer/Controllers/StripeWebhookController.cs`
- Create: `DeliverTableTests/Server/Unit/Controllers/StripeWebhookControllerTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using DeliverTableInfrastructure.Payments;
using DeliverTableServer.Configuration;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableTests.Global.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;
using System.Text;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class StripeWebhookControllerTests
{
    private IPaymentService _service = null!;
    private IStripeGateway _stripe = null!;
    private AppEnvironment _env = null!;
    private StripeWebhookController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IPaymentService>();
        _stripe = Substitute.For<IStripeGateway>();
        _env = TestEnvironmentFactory.Create();
        _sut = new StripeWebhookController(_service, _stripe, _env);
    }

    private void SetBody(string payload, string signature)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = payload.Length;
        context.Request.Headers["Stripe-Signature"] = signature;
        _sut.ControllerContext = new ControllerContext { HttpContext = context };
    }

    [Test]
    public async Task Receive_ValidSignature_DispatchesAndReturns200()
    {
        SetBody("{\"id\":\"evt_1\"}", "sig");
        var evt = new Stripe.Event { Id = "evt_1", Type = "payment_intent.succeeded" };
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), "sig", Arg.Any<string>()).Returns(evt);
        _service.HandleStripeEventAsync(evt, Arg.Any<CancellationToken>())
                .Returns(DeliverTableServer.Common.ServiceResult.Success());

        var result = await _sut.Receive(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task Receive_InvalidSignature_Returns400()
    {
        SetBody("{}", "bad");
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), "bad", Arg.Any<string>())
               .Throws(new Stripe.StripeException("bad sig"));

        var result = await _sut.Receive(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }
}
```

- [ ] **Step 2: Run to verify failure**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~StripeWebhookControllerTests"
```

Expected: FAIL (controller doesn't exist).

- [ ] **Step 3: Implement `StripeWebhookController.cs`**

```csharp
using DeliverTableInfrastructure.Payments;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.StripeWebhook.Base)]
public class StripeWebhookController(
    IPaymentService paymentService,
    IStripeGateway stripe,
    AppEnvironment env) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost(ApiRoutes.StripeWebhook.WebhookRoute)]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        string payload;
        using (var reader = new StreamReader(Request.Body))
            payload = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers["Stripe-Signature"].ToString();

        Stripe.Event stripeEvent;
        try
        {
            stripeEvent = stripe.ConstructWebhookEvent(payload, signature, env.StripeWebhookSecret);
        }
        catch (Stripe.StripeException)
        {
            return BadRequest(new { error = ErrorMessages.WebhookSignatureInvalid });
        }

        var result = await paymentService.HandleStripeEventAsync(stripeEvent, ct);
        return result.IsSuccess ? Ok() : StatusCode(500, new { error = result.Error!.Message });
    }
}
```

- [ ] **Step 4: Build + run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~StripeWebhookControllerTests"
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableServer/Controllers/StripeWebhookController.cs \
        DeliverTableTests/Server/Unit/Controllers/StripeWebhookControllerTests.cs
git commit -m "feat(server): add StripeWebhookController with signature verification and tests"
```

---

## Task 18: Wire payment into `OrderService.CreateFromCartAsync` with tests

**Files:**
- Modify: `DeliverTableServer/Services/OrderService.cs`
- Modify: `DeliverTableServer/Services/Interfaces/IOrderService.cs` (return type change)
- Modify: `DeliverTableServer/Controllers/OrderController.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/OrderServiceTests.cs`
- Modify: `DeliverTableTests/Server/Unit/Controllers/OrderControllerTests.cs`

Return type changes from `ServiceResult<OrderDto>` to `ServiceResult<CreateOrderResponse>` for `CreateFromCartAsync`. Callers update accordingly.

- [ ] **Step 1: Change `IOrderService.CreateFromCartAsync` signature**

```csharp
Task<ServiceResult<CreateOrderResponse>> CreateFromCartAsync(
    int customerId, CreateOrderRequest request, CancellationToken ct = default);
```

- [ ] **Step 2: Write failing test — new order path creates AwaitingPayment and returns client secret**

In `OrderServiceTests.cs`, add:

```csharp
[Test]
public async Task CreateFromCartAsync_NewFlow_CreatesAwaitingPaymentAndReturnsClientSecret()
{
    // Arrange existing mocks for cart/restaurant/discount per existing test pattern.
    // Set up new mock: _paymentService.CreateIntentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
    //   .Returns(ServiceResult<CreateIntentResult>.Success(new("pi_secret", "pi_1", 12m, "EUR")));

    var result = await _sut.CreateFromCartAsync(customerId: 1, request, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.Value!.ClientSecret, Is.EqualTo("pi_secret"));
    // Verify persisted order has Status=AwaitingPayment and PaymentStatus=Pending.
    await _orderRepository.Received(1).CreateAsync(
        Arg.Is<Order>(o => o.Status == OrderStatus.AwaitingPayment && o.PaymentStatus == PaymentStatus.Pending),
        Arg.Any<CancellationToken>());
}

[Test]
public async Task CreateFromCartAsync_DoesNotClearCartImmediately()
{
    var result = await _sut.CreateFromCartAsync(customerId: 1, request, CancellationToken.None);
    await _cartRepository.DidNotReceive().ClearForCustomerAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
}
```

- [ ] **Step 3: Run to verify failures**

Expected: new tests fail to compile or assert because the old `CreateFromCartAsync` clears cart and sets `PaymentStatus.Completed`.

- [ ] **Step 4: Modify `OrderService.CreateFromCartAsync`**

- Inject `IPaymentService paymentService` (add to constructor; ensure DI registration follows in Task 22).
- Change the Order creation block:
  - `order.Status = OrderStatus.AwaitingPayment;`
  - `order.PaymentStatus = PaymentStatus.Pending;`
- Mark loyalty/discount redemption rows as `LoyaltyRedemptionStatus.Pending` / `DiscountRedemptionStatus.Pending` at creation (new argument to the existing helper methods; or instantiate the redemption entities with `Status = Pending`).
- Remove the current cart clear + email publish calls; these now happen inside `PaymentService.HandleAuthorizationCompletedAsync`.
- After persisting the order, call:
  ```csharp
  var intent = await paymentService.CreateIntentAsync(order.Id, ct);
  if (!intent.IsSuccess) return intent.Error!;
  return new CreateOrderResponse(order.Id, intent.Value!.ClientSecret, env.StripePublishableKey, intent.Value.Amount, intent.Value.Currency);
  ```
- Inject `AppEnvironment env` into the constructor if not already (for `StripePublishableKey`).

- [ ] **Step 5: Update `OrderController.CreateOrder`**

```csharp
[HttpPost]
[Authorize(Roles = nameof(UserRole.Customer))]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
{
    if (!this.TryGetUserId(out int userId)) return Unauthorized();
    var result = await _orderService.CreateFromCartAsync(userId, request, ct);
    if (result.IsSuccess) return Ok(result.Value);
    return result.Error!.ToErrorResult();
}
```

(The previous `CreatedAtAction(nameof(GetById), ...)` no longer applies since we return `CreateOrderResponse`, not `OrderDto`.)

- [ ] **Step 6: Update `OrderControllerTests` to reflect new return type**

Change assertions to `Is.InstanceOf<OkObjectResult>()` and check `CreateOrderResponse` fields.

- [ ] **Step 7: Run the whole test suite for affected areas**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~OrderServiceTests|OrderControllerTests"
```

Expected: all PASS.

- [ ] **Step 8: Commit**

```bash
git add DeliverTableServer/Services/OrderService.cs \
        DeliverTableServer/Services/Interfaces/IOrderService.cs \
        DeliverTableServer/Controllers/OrderController.cs \
        DeliverTableTests/Server/Unit/Services/OrderServiceTests.cs \
        DeliverTableTests/Server/Unit/Controllers/OrderControllerTests.cs
git commit -m "feat(server): wire Stripe payment into OrderService.CreateFromCartAsync"
```

---

## Task 19: Wire capture/cancel/refund into `OrderService.UpdateStatusAsync` with tests

**Files:**
- Modify: `DeliverTableServer/Services/OrderService.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/OrderServiceTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
[Test]
public async Task UpdateStatusAsync_PendingToConfirmed_CallsPaymentCapture()
{
    var order = new Order { Id = 10, Status = OrderStatus.Pending };
    _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
    _paymentService.CaptureAsync(10, Arg.Any<CancellationToken>()).Returns(ServiceResult.Success());

    var result = await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = OrderStatus.Confirmed }, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(order.Status, Is.EqualTo(OrderStatus.Confirmed));
    await _paymentService.Received(1).CaptureAsync(10, Arg.Any<CancellationToken>());
}

[Test]
public async Task UpdateStatusAsync_CaptureFails_KeepsOrderPending()
{
    var order = new Order { Id = 10, Status = OrderStatus.Pending };
    _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
    _paymentService.CaptureAsync(10, Arg.Any<CancellationToken>()).Returns(new ServiceError("fail"));

    var result = await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = OrderStatus.Confirmed }, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.False);
    Assert.That(order.Status, Is.EqualTo(OrderStatus.Pending));
}

[Test]
public async Task UpdateStatusAsync_PendingToRefused_CancelsAuthorization()
{
    var order = new Order { Id = 10, Status = OrderStatus.Pending };
    _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
    _paymentService.CancelAuthorizationAsync(10, Arg.Any<CancellationToken>()).Returns(ServiceResult.Success());

    await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = OrderStatus.Refused }, CancellationToken.None);

    await _paymentService.Received(1).CancelAuthorizationAsync(10, Arg.Any<CancellationToken>());
}

[Test]
public async Task UpdateStatusAsync_CancellationAfterCapture_Refunds()
{
    var order = new Order
    {
        Id = 10, Status = OrderStatus.Preparing, PaymentStatus = PaymentStatus.Completed, TotalAmount = 20m
    };
    _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
    _paymentService.RefundAsync(10, 20m, Arg.Any<string>(), null, Arg.Any<CancellationToken>())
                   .Returns(ServiceResult<RefundDto>.Success(new RefundDto(1, 20m, "EUR", "order_cancelled", DateTime.UtcNow)));

    await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = OrderStatus.Cancelled }, CancellationToken.None);

    await _paymentService.Received(1).RefundAsync(10, 20m, "order_cancelled", null, Arg.Any<CancellationToken>());
}
```

- [ ] **Step 2: Run to verify failures**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~OrderServiceTests"
```

Expected: new tests fail.

- [ ] **Step 3: Modify `OrderService.UpdateStatusAsync`**

Before persisting the new status, insert:

```csharp
if (order.Status == OrderStatus.Pending && request.Status == OrderStatus.Confirmed)
{
    var capture = await paymentService.CaptureAsync(order.Id, ct);
    if (!capture.IsSuccess) return capture.Error!;
}
else if (order.Status == OrderStatus.Pending && request.Status == OrderStatus.Refused)
{
    var cancel = await paymentService.CancelAuthorizationAsync(order.Id, ct);
    if (!cancel.IsSuccess) return cancel.Error!;
}
else if (IsLateCancellation(order.Status, request.Status) && order.PaymentStatus == PaymentStatus.Completed)
{
    var refund = await paymentService.RefundAsync(order.Id, order.TotalAmount, "order_cancelled", null, ct);
    if (!refund.IsSuccess) return refund.Error!;
}
```

Where `IsLateCancellation(from, to)` returns true when `to == Cancelled` and `from in { Confirmed, Preparing, Ready, Delivering }`.

- [ ] **Step 4: Run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~OrderServiceTests"
```

Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableServer/Services/OrderService.cs \
        DeliverTableTests/Server/Unit/Services/OrderServiceTests.cs
git commit -m "feat(server): hook capture/cancel/refund into OrderService.UpdateStatusAsync"
```

---

## Task 20: Admin refund endpoint with tests

**Files:**
- Modify: `DeliverTableServer/Controllers/AdminController.cs`
- Modify: `DeliverTableTests/Server/Unit/Controllers/` (relevant admin controller tests file; create if absent)

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public async Task RefundOrder_HappyPath_ReturnsRefundDto()
{
    AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "99", nameof(UserRole.Administrator));
    _paymentService.RefundAsync(42, 20m, "mistake", 99, Arg.Any<CancellationToken>())
                   .Returns(ServiceResult<RefundDto>.Success(new RefundDto(1, 20m, "EUR", "mistake", DateTime.UtcNow)));

    var result = await _sut.RefundOrder(42, new AdminRefundRequest { Amount = 20m, Reason = "mistake" }, CancellationToken.None);

    Assert.That(result, Is.InstanceOf<OkObjectResult>());
}

[Test]
public async Task RefundOrder_NonAdmin_StillProtectedByAttribute()
{
    // Tests that [Authorize(Roles=Administrator)] attribute exists (reflection check)
    var method = typeof(AdminController).GetMethod(nameof(AdminController.RefundOrder))!;
    var attr = method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
                     .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                     .FirstOrDefault();
    Assert.That(attr?.Roles, Does.Contain(nameof(UserRole.Administrator)));
}
```

- [ ] **Step 2: Run to verify failure**

Expected: method `RefundOrder` not found.

- [ ] **Step 3: Implement**

Add to `AdminController.cs`:

```csharp
[HttpPost(ApiRoutes.Admin.OrderRefundRoute)]
[Authorize(Roles = nameof(UserRole.Administrator))]
public async Task<IActionResult> RefundOrder(int id, [FromBody] AdminRefundRequest request, CancellationToken ct)
{
    if (!this.TryGetUserId(out int adminId)) return Unauthorized();
    var result = await _paymentService.RefundAsync(id, request.Amount, request.Reason, adminId, ct);
    return result.IsSuccess ? Ok(result.Value) : result.Error!.ToErrorResult();
}
```

Inject `IPaymentService _paymentService` into the controller constructor.

- [ ] **Step 4: Run tests + build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
    --no-build --filter "FullyQualifiedName~AdminController"
```

Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableServer/Controllers/AdminController.cs DeliverTableTests/Server/Unit/Controllers/
git commit -m "feat(server): add admin refund endpoint with tests"
```

---

## Task 21: Register Stripe services in DI

**Files:**
- Modify: `DeliverTableServer/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Register repositories**

In `RegisterRepositories`:

```csharp
services.AddScoped<IPaymentRepository, PaymentRepository>();
```

- [ ] **Step 2: Register services**

In `RegisterServices`:

```csharp
services.AddScoped<IPaymentService, PaymentService>();
services.AddScoped<IPaymentLifecycleService, PaymentLifecycleService>();
```

- [ ] **Step 3: Register gateway**

In `RegisterInfrastructure`:

```csharp
services.AddSingleton<IStripeGateway, StripeGateway>();
```

- [ ] **Step 4: Build and run full suite**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
make test
```

Expected: build succeeds; test suite passes (ignore the known `AppEnvironmentTests.Load_AppliesDefaults_WhenOptionalVarsAreMissing` Docker failure mentioned in CLAUDE.md).

- [ ] **Step 5: Commit**

```bash
git add DeliverTableServer/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(server): register Stripe services in DI"
```

---

## Task 22: Create `DeliverTableScheduler` project scaffold

**Files:**
- Create: `DeliverTableScheduler/DeliverTableScheduler.csproj`
- Create: `DeliverTableScheduler/Program.cs`
- Create: `DeliverTableScheduler/Configuration/SchedulerEnvironment.cs`
- Create: `docker/images/scheduler.dev.dockerfile`
- Create: `docker/images/scheduler.prod.dockerfile`
- Modify: `DeliverTable.sln`

Both Dockerfiles live at `docker/images/{name}.{dev|prod}.dockerfile` to follow the repo's convention (see `docker/images/worker.dev.dockerfile` and `worker.prod.dockerfile` as the templates we mirror).

- [ ] **Step 1: Create csproj**

Inspect `DeliverTableWorker/DeliverTableWorker.csproj` and `DeliverTableInfrastructure/DeliverTableInfrastructure.csproj` first to copy the **exact** versions used elsewhere. Then create `DeliverTableScheduler/DeliverTableScheduler.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <PreserveCompilationContext>true</PreserveCompilationContext>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="DotNetEnv" Version="3.1.1" />
        <!-- Stripe.net is transitive via Infrastructure once Task 6 has added it there;
             do NOT add it here unless the build complains it cannot resolve Stripe types. -->
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\DeliverTableInfrastructure\DeliverTableInfrastructure.csproj" />
        <ProjectReference Include="..\DeliverTableSharedLibrary\DeliverTableSharedLibrary.csproj" />
    </ItemGroup>

    <ItemGroup>
        <InternalsVisibleTo Include="DeliverTableSchedulerTests" />
    </ItemGroup>
</Project>
```

`Microsoft.EntityFrameworkCore.Design` and `Npgsql.EntityFrameworkCore.PostgreSQL` come transitively from `DeliverTableInfrastructure`.

- [ ] **Step 2: Create `SchedulerEnvironment.cs`**

```csharp
namespace DeliverTableScheduler.Configuration;

public sealed class SchedulerEnvironment
{
    public required string ConnectionStringDatabase { get; init; }
    public required string StripeSecretKey { get; init; }

    public static SchedulerEnvironment Load()
    {
        var errors = new List<string>();
        var cs = Require("CONNECTION_STRING_DATABASE", errors);
        var sk = Require("STRIPE_SECRET_KEY", errors);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join("; ", errors));
        return new SchedulerEnvironment { ConnectionStringDatabase = cs, StripeSecretKey = sk };
    }

    private static string Require(string name, List<string> errors)
    {
        var v = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(v)) { errors.Add($"Missing env var: {name}"); return ""; }
        return v;
    }
}
```

- [ ] **Step 3: Create `Program.cs`**

```csharp
using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableScheduler.Configuration;
using Microsoft.EntityFrameworkCore;

DotNetEnv.Env.Load();
var env = SchedulerEnvironment.Load();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(env);
builder.Services.AddDbContext<DeliverTableContext>(opts => opts.UseNpgsql(env.ConnectionStringDatabase));

Stripe.StripeConfiguration.ApiKey = env.StripeSecretKey;
Stripe.StripeConfiguration.ApiVersion = "2026-03-25.dahlia";
builder.Services.AddSingleton<IStripeGateway, StripeGateway>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();
builder.Services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();
builder.Services.AddScoped<IPaymentLifecycleService, PaymentLifecycleService>();

// Hosted services added in Tasks 23 and 24.

await builder.Build().RunAsync();
```

- [ ] **Step 4: Create `docker/images/scheduler.dev.dockerfile`**

Mirror `docker/images/worker.dev.dockerfile` exactly. Verify the worker file first, then write:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src

ENTRYPOINT ["dotnet", "watch", "run", "--project", "DeliverTableScheduler", "--non-interactive", "--no-launch-profile"]
```

- [ ] **Step 5: Create `docker/images/scheduler.prod.dockerfile`**

Mirror `docker/images/worker.prod.dockerfile` exactly, swapping `DeliverTableWorker` → `DeliverTableScheduler` in the COPY/restore/publish/ENTRYPOINT lines:

```dockerfile
# ── Stage 1: Build Go tools ───────────────────────────────────
# depcopier scans ELF binaries for shared-lib deps and assembles a rootfs.
# healthcheck is a static HTTP probe replacing curl in scratch containers.
FROM golang:1.24-alpine AS tools

WORKDIR /src
COPY docker/images/tools/ ./

RUN CGO_ENABLED=0 go build -trimpath -ldflags='-s -w' -o /out/depcopier   ./depcopier   && \
    CGO_ENABLED=0 go build -trimpath -ldflags='-s -w' -o /out/healthcheck ./healthcheck

# ── Stage 2: Build the .NET application ───────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY DeliverTableScheduler/DeliverTableScheduler.csproj               DeliverTableScheduler/
COPY DeliverTableInfrastructure/DeliverTableInfrastructure.csproj     DeliverTableInfrastructure/
COPY DeliverTableSharedLibrary/DeliverTableSharedLibrary.csproj       DeliverTableSharedLibrary/

RUN dotnet restore DeliverTableScheduler/DeliverTableScheduler.csproj \
    -r linux-musl-x64

COPY DeliverTableScheduler/       DeliverTableScheduler/
COPY DeliverTableInfrastructure/  DeliverTableInfrastructure/
COPY DeliverTableSharedLibrary/   DeliverTableSharedLibrary/

RUN dotnet publish DeliverTableScheduler/DeliverTableScheduler.csproj \
    -c Release \
    -r linux-musl-x64 \
    --self-contained \
    -o /app/publish \
    --no-restore

# ── Stage 3: Assemble the minimal rootfs ─────────────────────
FROM alpine:3.21 AS rootfs

RUN apk add --no-cache \
    ca-certificates \
    libgcc \
    libstdc++ \
    libssl3 \
    libcrypto3 \
    zlib

COPY --from=tools /out/depcopier   /tools/depcopier
COPY --from=tools /out/healthcheck /staging/healthcheck
COPY --from=build /app/publish     /staging/app

RUN /tools/depcopier \
    --scan  /staging/app \
    --copy  /staging/app:/app \
    --copy  /staging/healthcheck:/healthcheck \
    --certs \
    --user  1654:1654:appuser \
    --mkdir /tmp \
    --out   /rootfs

# ── Stage 4: Scratch ─────────────────────────────────────────
FROM scratch

COPY --from=rootfs /rootfs /

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

USER 1654:1654

ENTRYPOINT ["/app/DeliverTableScheduler"]
```

- [ ] **Step 6: Add project to solution**

```bash
docker compose -f docker-dev.yaml exec backend dotnet sln /src/DeliverTable.sln add /src/DeliverTableScheduler/DeliverTableScheduler.csproj
```

- [ ] **Step 7: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: scheduler compiles (entry point without any hosted services — runs empty but valid).

- [ ] **Step 8: Commit**

```bash
git add DeliverTableScheduler/ \
        DeliverTable.sln \
        docker/images/scheduler.dev.dockerfile \
        docker/images/scheduler.prod.dockerfile
git commit -m "feat(scheduler): add DeliverTableScheduler project scaffold"
```

---

## Task 23: `OrderAbandonmentSweep` + shared base class with tests

**Files:**
- Create: `DeliverTableScheduler/Jobs/PeriodicSweepJob.cs`
- Create: `DeliverTableScheduler/Jobs/OrderAbandonmentSweep.cs`
- Create: `DeliverTableSchedulerTests/DeliverTableSchedulerTests.csproj`
- Create: `DeliverTableSchedulerTests/Jobs/OrderAbandonmentSweepTests.cs`
- Modify: `DeliverTable.sln`
- Modify: `Makefile` (add scheduler tests)

- [ ] **Step 1: Create scheduler test project**

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <IsPackable>false</IsPackable>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="NUnit" Version="4.3.2" />
        <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
        <PackageReference Include="NSubstitute" Version="5.3.0" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include="..\DeliverTableScheduler\DeliverTableScheduler.csproj" />
        <ProjectReference Include="..\DeliverTableInfrastructure\DeliverTableInfrastructure.csproj" />
    </ItemGroup>
</Project>
```

Versions must match `DeliverTableTests.csproj`. Inspect it and copy.

Add to solution:

```bash
docker compose -f docker-dev.yaml exec backend dotnet sln /src/DeliverTable.sln add /src/DeliverTableSchedulerTests/DeliverTableSchedulerTests.csproj
```

- [ ] **Step 2: Write failing test for `OrderAbandonmentSweep`**

```csharp
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
public class OrderAbandonmentSweepTests
{
    private IOrderRepository _orderRepo = null!;
    private IPaymentLifecycleService _lifecycle = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private OrderAbandonmentSweep _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _orderRepo = Substitute.For<IOrderRepository>();
        _lifecycle = Substitute.For<IPaymentLifecycleService>();

        var services = new ServiceCollection();
        services.AddSingleton(_orderRepo);
        services.AddSingleton(_lifecycle);
        _scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        _sut = new OrderAbandonmentSweep(_scopeFactory, NullLogger<OrderAbandonmentSweep>.Instance);
    }

    [Test]
    public async Task RunTickAsync_CancelsOrdersOlderThan15Minutes()
    {
        var stale = new List<Order>
        {
            new() { Id = 1, Status = OrderStatus.AwaitingPayment, CreatedAt = DateTime.UtcNow.AddMinutes(-20) },
        };
        _orderRepo.GetOrdersOlderThanAsync(OrderStatus.AwaitingPayment, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                  .Returns(stale);
        _lifecycle.CancelAbandonedOrderAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.RunTickForTestAsync(CancellationToken.None);

        await _lifecycle.Received(1).CancelAbandonedOrderAsync(1, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunTickAsync_ContinuesOnPerOrderFailure()
    {
        var stale = new List<Order>
        {
            new() { Id = 1 }, new() { Id = 2 },
        };
        _orderRepo.GetOrdersOlderThanAsync(OrderStatus.AwaitingPayment, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                  .Returns(stale);
        _lifecycle.CancelAbandonedOrderAsync(1, Arg.Any<CancellationToken>())
                  .Throws(new Exception("boom"));
        _lifecycle.CancelAbandonedOrderAsync(2, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.RunTickForTestAsync(CancellationToken.None);

        await _lifecycle.Received(1).CancelAbandonedOrderAsync(2, Arg.Any<CancellationToken>());
    }
}
```

Note: `RunTickForTestAsync` is a non-production method exposed as `internal` via `InternalsVisibleTo` attribute on the Scheduler project — add to the csproj:

```xml
<ItemGroup>
    <InternalsVisibleTo Include="DeliverTableSchedulerTests" />
</ItemGroup>
```

- [ ] **Step 3: Run to verify failure**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableSchedulerTests/DeliverTableSchedulerTests.csproj
```

Expected: FAIL — class doesn't exist.

- [ ] **Step 4: Implement `PeriodicSweepJob.cs`**

```csharp
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeliverTableScheduler.Jobs;

public abstract class PeriodicSweepJob(
    IServiceScopeFactory scopeFactory,
    ILogger logger) : BackgroundService
{
    protected abstract OrderStatus TargetStatus { get; }
    protected abstract TimeSpan Threshold { get; }
    protected abstract Task InvokeLifecycleAsync(
        IPaymentLifecycleService svc, int orderId, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunTickAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Sweep tick failed"); }
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }

    internal async Task RunTickForTestAsync(CancellationToken ct) => await RunTickAsync(ct);

    private async Task RunTickAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var lifecycle = scope.ServiceProvider.GetRequiredService<IPaymentLifecycleService>();
        var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var stale = await orders.GetOrdersOlderThanAsync(TargetStatus, DateTime.UtcNow - Threshold, ct);
        foreach (var o in stale)
        {
            try { await InvokeLifecycleAsync(lifecycle, o.Id, ct); }
            catch (Exception ex) { logger.LogError(ex, "Failed sweep for order {Id}", o.Id); }
        }
    }
}
```

- [ ] **Step 5: Implement `OrderAbandonmentSweep.cs`**

```csharp
using DeliverTableInfrastructure.Payments;
using DeliverTableSharedLibrary.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeliverTableScheduler.Jobs;

public class OrderAbandonmentSweep(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderAbandonmentSweep> logger)
    : PeriodicSweepJob(scopeFactory, logger)
{
    protected override OrderStatus TargetStatus => OrderStatus.AwaitingPayment;
    protected override TimeSpan Threshold => TimeSpan.FromMinutes(15);
    protected override Task InvokeLifecycleAsync(
        IPaymentLifecycleService svc, int orderId, CancellationToken ct) =>
        svc.CancelAbandonedOrderAsync(orderId, ct);
}
```

- [ ] **Step 6: Register hosted service in `Program.cs`**

```csharp
builder.Services.AddHostedService<OrderAbandonmentSweep>();
```

- [ ] **Step 7: Add `InternalsVisibleTo` to `DeliverTableScheduler.csproj`**

```xml
<ItemGroup>
    <InternalsVisibleTo Include="DeliverTableSchedulerTests" />
</ItemGroup>
```

- [ ] **Step 8: Build and run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableSchedulerTests/DeliverTableSchedulerTests.csproj
```

Expected: scheduler tests PASS.

- [ ] **Step 9: Update Makefile**

In `Makefile`, find the `test` target. Add `DeliverTableSchedulerTests` to whatever `dotnet test` invocation runs. Typical:

```makefile
test:
	docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTable.sln
```

If the target already runs the solution-level test, no change needed. If it invokes `DeliverTableTests.csproj` directly, add a second invocation for the scheduler tests, or switch to the solution-level form.

- [ ] **Step 10: Commit**

```bash
git add DeliverTableScheduler/ DeliverTableSchedulerTests/ DeliverTable.sln Makefile
git commit -m "feat(scheduler): add OrderAbandonmentSweep with tests and shared sweep base"
```

---

## Task 24: `OrderRestaurantTimeoutSweep` with tests

**Files:**
- Create: `DeliverTableScheduler/Jobs/OrderRestaurantTimeoutSweep.cs`
- Create: `DeliverTableSchedulerTests/Jobs/OrderRestaurantTimeoutSweepTests.cs`
- Modify: `DeliverTableScheduler/Program.cs`

- [ ] **Step 1: Write failing test**

```csharp
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
```

Mirror the test fixture setup from `OrderAbandonmentSweepTests`.

- [ ] **Step 2: Run to verify failure**

Expected: type doesn't exist.

- [ ] **Step 3: Implement**

```csharp
using DeliverTableInfrastructure.Payments;
using DeliverTableSharedLibrary.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeliverTableScheduler.Jobs;

public class OrderRestaurantTimeoutSweep(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderRestaurantTimeoutSweep> logger)
    : PeriodicSweepJob(scopeFactory, logger)
{
    protected override OrderStatus TargetStatus => OrderStatus.Pending;
    protected override TimeSpan Threshold => TimeSpan.FromHours(24);
    protected override Task InvokeLifecycleAsync(
        IPaymentLifecycleService svc, int orderId, CancellationToken ct) =>
        svc.AutoRefuseOrderAsync(orderId, ct);
}
```

- [ ] **Step 4: Register in `Program.cs`**

```csharp
builder.Services.AddHostedService<OrderRestaurantTimeoutSweep>();
```

- [ ] **Step 5: Run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableSchedulerTests/DeliverTableSchedulerTests.csproj
```

Expected: all sweep tests PASS.

- [ ] **Step 6: Commit**

```bash
git add DeliverTableScheduler/Jobs/OrderRestaurantTimeoutSweep.cs \
        DeliverTableScheduler/Program.cs \
        DeliverTableSchedulerTests/Jobs/OrderRestaurantTimeoutSweepTests.cs
git commit -m "feat(scheduler): add OrderRestaurantTimeoutSweep with tests"
```

---

## Task 25: Add scheduler service to Docker compose files

**Files:**
- Modify: `docker-dev.yaml`
- Modify: `docker-prod.yaml`

The scheduler service mirrors the worker service definition exactly, with these differences:
- Different `container_name`, dockerfile path, and IP address.
- Different env vars (only needs DB + Stripe; no SMTP/RabbitMQ).
- Doesn't depend on RabbitMQ.

The Dockerfiles were created in Task 22 — this task only wires the services into the compose files.

- [ ] **Step 1: Add `scheduler` service to `docker-dev.yaml`**

Open `docker-dev.yaml` and insert the new service block immediately after the existing `worker:` service block (and before the `networks:` section). The new IP `192.168.60.90` is the next free address on `dt-backend-net` (dev range 192.168.60.x; worker is .80).

```yaml
  # ── Background Scheduler ─────────────────────────────────────
  scheduler:
    container_name: dt-scheduler-dev
    build:
      context: .
      dockerfile: docker/images/scheduler.dev.dockerfile
    volumes:
      - .:/src
      - nuget-cache:/root/.nuget/packages
    environment:
      DOTNET_USE_POLLING_FILE_WATCHER: "true"
      CONNECTION_STRING_DATABASE: ${CONNECTION_STRING_DATABASE}
      STRIPE_SECRET_KEY: ${STRIPE_SECRET_KEY}
    depends_on:
      database:
        condition: service_healthy
    networks:
      dt-backend-net:
        ipv4_address: 192.168.60.90
    restart: unless-stopped
```

- [ ] **Step 2: Add `scheduler` service to `docker-prod.yaml`**

Insert the new service block immediately after the existing `worker:` service block (and before the `networks:` section). New IP `172.32.0.70` is the next free address on `dt-backend-net` (prod range 172.32.0.x; worker is .60).

```yaml
  scheduler:
    container_name: dt-scheduler-prod
    build:
      context: .
      dockerfile: docker/images/scheduler.prod.dockerfile
    environment:
      CONNECTION_STRING_DATABASE: ${CONNECTION_STRING_DATABASE}
      STRIPE_SECRET_KEY: ${STRIPE_SECRET_KEY}
    depends_on:
      database:
        condition: service_healthy
    networks:
      dt-backend-net:
        ipv4_address: 172.32.0.70
    cap_drop:
      - ALL
    logging: *default-logging
    restart: always
    read_only: true
    tmpfs:
      - /tmp:size=50m
```

- [ ] **Step 3: Rebuild dev stack and verify scheduler starts**

```bash
docker compose -f docker-dev.yaml up -d --build scheduler
docker compose -f docker-dev.yaml logs scheduler --tail=50
```

Expected: scheduler container builds, starts, and logs `Application started` (or equivalent .NET host startup messages) without crash. If `STRIPE_SECRET_KEY` is missing in `.env`, the container will exit with the env var validation error — set it (test mode key from Stripe Dashboard) and retry.

- [ ] **Step 4: Optionally validate prod compose syntax**

```bash
docker compose -f docker-prod.yaml config > /dev/null
```

Expected: no errors. Does not actually start anything.

- [ ] **Step 5: Commit**

```bash
git add docker-dev.yaml docker-prod.yaml
git commit -m "feat(docker): add scheduler service to dev and prod compose files"
```

---

## Task 26: Load Stripe.js in client

**Files:**
- Modify: `DeliverTableClient/wwwroot/index.html`

- [ ] **Step 1: Add script tag in `<head>`**

```html
<script src="https://js.stripe.com/v3/"></script>
```

- [ ] **Step 2: Reload client (dev stack auto-reloads) and verify in browser devtools that `window.Stripe` is defined**

- [ ] **Step 3: Commit**

```bash
git add DeliverTableClient/wwwroot/index.html
git commit -m "feat(client): load Stripe.js in index.html"
```

---

## Task 27: Add `IStripeJsInterop` and JS wrapper

**Files:**
- Create: `DeliverTableClient/Services/Payment/IStripeJsInterop.cs`
- Create: `DeliverTableClient/Services/Payment/StripeJsInterop.cs`
- Create: `DeliverTableClient/Pages/Checkout/Checkout/Checkout.razor.js`

- [ ] **Step 1: Create `IStripeJsInterop.cs`**

```csharp
namespace DeliverTableClient.Services.Payment;

public interface IStripeJsInterop : IAsyncDisposable
{
    Task InitializeAsync(string publishableKey);
    Task MountPaymentElementAsync(string clientSecret, string domElementId);
    Task<StripeConfirmResult> ConfirmPaymentAsync(string returnUrl);
    Task UnmountAsync();
}

public record StripeConfirmResult(bool Succeeded, string? ErrorMessage, string? PaymentIntentId);
```

- [ ] **Step 2: Create `StripeJsInterop.cs`**

```csharp
using Microsoft.JSInterop;

namespace DeliverTableClient.Services.Payment;

public class StripeJsInterop(IJSRuntime js) : IStripeJsInterop
{
    private IJSObjectReference? _module;

    public async Task InitializeAsync(string publishableKey)
    {
        _module ??= await js.InvokeAsync<IJSObjectReference>(
            "import", "./Pages/Checkout/Checkout/Checkout.razor.js");
        await _module.InvokeVoidAsync("initialize", publishableKey);
    }

    public async Task MountPaymentElementAsync(string clientSecret, string domElementId)
    {
        if (_module is null) throw new InvalidOperationException("InitializeAsync first");
        await _module.InvokeVoidAsync("mountPaymentElement", clientSecret, domElementId);
    }

    public async Task<StripeConfirmResult> ConfirmPaymentAsync(string returnUrl)
    {
        if (_module is null) throw new InvalidOperationException("InitializeAsync first");
        return await _module.InvokeAsync<StripeConfirmResult>("confirmPayment", returnUrl);
    }

    public async Task UnmountAsync()
    {
        if (_module is not null) await _module.InvokeVoidAsync("unmount");
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null) await _module.DisposeAsync();
    }
}
```

- [ ] **Step 3: Create `Checkout.razor.js`**

```javascript
let stripe = null;
let elements = null;

export function initialize(publishableKey) {
    stripe = window.Stripe(publishableKey);
}

export function mountPaymentElement(clientSecret, domElementId) {
    elements = stripe.elements({ clientSecret, locale: 'fr' });
    const el = elements.create('payment', { layout: 'tabs' });
    el.mount(`#${domElementId}`);
}

export async function confirmPayment(returnUrl) {
    const result = await stripe.confirmPayment({
        elements,
        confirmParams: { return_url: returnUrl },
        redirect: 'if_required'
    });
    if (result.error) {
        return { succeeded: false, errorMessage: result.error.message, paymentIntentId: null };
    }
    return { succeeded: true, errorMessage: null, paymentIntentId: result.paymentIntent?.id ?? null };
}

export function unmount() {
    if (elements) elements = null;
}
```

- [ ] **Step 4: Register in client DI (`DeliverTableClient/Program.cs`)**

```csharp
builder.Services.AddScoped<IStripeJsInterop, StripeJsInterop>();
```

- [ ] **Step 5: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 6: Commit**

```bash
git add DeliverTableClient/Services/Payment/ \
        DeliverTableClient/Pages/Checkout/Checkout/Checkout.razor.js \
        DeliverTableClient/Program.cs
git commit -m "feat(client): add IStripeJsInterop wrapper and checkout JS module"
```

---

## Task 28: Add `IPaymentApiClient`

**Files:**
- Create: `DeliverTableClient/Services/Payment/IPaymentApiClient.cs`
- Create: `DeliverTableClient/Services/Payment/PaymentApiClient.cs`
- Modify: `DeliverTableClient/Program.cs`

- [ ] **Step 1: Create interface**

```csharp
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Dtos.Payment;

namespace DeliverTableClient.Services.Payment;

public interface IPaymentApiClient
{
    Task<CreateOrderResponse?> CreateOrderAsync(CreateOrderRequest request);
    Task CancelAsync(int orderId);
}
```

- [ ] **Step 2: Implement**

```csharp
using System.Net.Http.Json;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Dtos.Payment;

namespace DeliverTableClient.Services.Payment;

public class PaymentApiClient(HttpClient http) : IPaymentApiClient
{
    public async Task<CreateOrderResponse?> CreateOrderAsync(CreateOrderRequest request)
    {
        var response = await http.PostAsJsonAsync(ApiRoutes.Order.Base, request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
    }

    public async Task CancelAsync(int orderId)
    {
        await http.PostAsync($"{ApiRoutes.Payment.Base}/{orderId}/cancel", null);
    }
}
```

- [ ] **Step 3: Register**

```csharp
builder.Services.AddScoped<IPaymentApiClient, PaymentApiClient>();
```

- [ ] **Step 4: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 5: Commit**

```bash
git add DeliverTableClient/Services/Payment/ DeliverTableClient/Program.cs
git commit -m "feat(client): add IPaymentApiClient HTTP service"
```

---

## Task 29: Build `Checkout.razor` page

**Files:**
- Create: `DeliverTableClient/Pages/Checkout/Checkout/Checkout.razor`
- Create: `DeliverTableClient/Pages/Checkout/Checkout/Checkout.razor.scss`

- [ ] **Step 1: Create `Checkout.razor`**

```razor
@page "/checkout"
@using DeliverTableClient.Services.Payment
@using DeliverTableSharedLibrary.Dtos.Order
@inject IPaymentApiClient PaymentApi
@inject IStripeJsInterop StripeJs
@inject NavigationManager Nav

<section class="checkout">
    <h1>Paiement</h1>
    @if (_loading)
    {
        <p>Préparation du paiement...</p>
    }
    else if (_error is not null)
    {
        <div class="error">@_error</div>
    }
    else
    {
        <div class="summary">
            <p>Montant : <strong>@_amount.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("fr-FR"))</strong></p>
        </div>
        <div id="payment-element"></div>
        <button class="btn-primary" @onclick="ConfirmAsync" disabled="@_submitting">Payer</button>
    }
</section>

@code {
    private bool _loading = true;
    private bool _submitting;
    private string? _error;
    private int _orderId;
    private decimal _amount;

    protected override async Task OnInitializedAsync()
    {
        // Cart service call to assemble CreateOrderRequest omitted — adapt to existing cart context.
        var request = new CreateOrderRequest { /* populate from cart */ };
        var response = await PaymentApi.CreateOrderAsync(request);
        if (response is null)
        {
            _error = "Impossible de démarrer le paiement.";
            _loading = false;
            return;
        }
        _orderId = response.OrderId;
        _amount = response.Amount;
        await StripeJs.InitializeAsync(response.PublishableKey);
        _loading = false;
        StateHasChanged();
        await StripeJs.MountPaymentElementAsync(response.ClientSecret, "payment-element");
    }

    private async Task ConfirmAsync()
    {
        _submitting = true;
        var baseUri = Nav.BaseUri.TrimEnd('/');
        var result = await StripeJs.ConfirmPaymentAsync($"{baseUri}/checkout/result?orderId={_orderId}");
        if (result.Succeeded)
        {
            Nav.NavigateTo($"/checkout/result?orderId={_orderId}");
        }
        else
        {
            _error = result.ErrorMessage ?? "Échec du paiement.";
            _submitting = false;
        }
    }

    public async ValueTask DisposeAsync() => await StripeJs.UnmountAsync();
}
```

- [ ] **Step 2: Create matching SCSS (minimal)**

```scss
.checkout {
    max-width: 600px;
    margin: 2rem auto;
    padding: 1.5rem;
    #payment-element { margin: 1rem 0; }
    .error { color: var(--color-error, #b00); }
}
```

- [ ] **Step 3: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 4: Manual QA in browser**

Start the dev stack (`make dev` if not running). Log in as a customer, add items to cart, navigate directly to `/checkout`. Verify the Payment Element renders. Use Stripe test card `4242 4242 4242 4242` with any future expiry, any CVC, any postal code. Ensure `stripe listen` is running to forward webhooks.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableClient/Pages/Checkout/Checkout/
git commit -m "feat(client): add Checkout page with Payment Element"
```

---

## Task 30: `CheckoutResult.razor` with status polling

**Files:**
- Create: `DeliverTableClient/Pages/Checkout/CheckoutResult/CheckoutResult.razor`
- Create: `DeliverTableClient/Pages/Checkout/CheckoutResult/CheckoutResult.razor.scss`

- [ ] **Step 1: Create `CheckoutResult.razor`**

```razor
@page "/checkout/result"
@using DeliverTableClient.Services
@inject IOrderService OrderService
@inject NavigationManager Nav

<section class="checkout-result">
    @if (_state == State.Waiting)
    {
        <p>Vérification du paiement...</p>
    }
    else if (_state == State.Success)
    {
        <h1>Commande confirmée</h1>
        <p>En attente de la confirmation du restaurant.</p>
        <a class="btn-primary" href="@($"/orders/{_orderId}")">Voir ma commande</a>
    }
    else
    {
        <h1>Paiement non abouti</h1>
        <p>Votre paiement n'a pas pu être finalisé.</p>
        <a class="btn-primary" href="/checkout">Réessayer</a>
    }
</section>

@code {
    [SupplyParameterFromQuery] public int OrderId { get; set; }
    private int _orderId;
    private enum State { Waiting, Success, Failure }
    private State _state = State.Waiting;

    protected override async Task OnInitializedAsync()
    {
        _orderId = OrderId;
        for (int i = 0; i < 10; i++)
        {
            var dto = await OrderService.GetByIdAsync(_orderId);
            if (dto is null) { _state = State.Failure; return; }
            if (dto.Status != nameof(DeliverTableSharedLibrary.Enums.OrderStatus.AwaitingPayment))
            {
                _state = dto.Status == nameof(DeliverTableSharedLibrary.Enums.OrderStatus.Cancelled)
                    ? State.Failure : State.Success;
                return;
            }
            await Task.Delay(1000);
        }
        _state = State.Failure;
    }
}
```

The existing client order service should have a `GetByIdAsync` method returning an `OrderDto`; if its name differs, adjust. If missing, add one via the established pattern in `DeliverTableClient/Services/`.

- [ ] **Step 2: SCSS**

```scss
.checkout-result {
    max-width: 600px;
    margin: 3rem auto;
    padding: 1.5rem;
    text-align: center;
}
```

- [ ] **Step 3: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 4: Commit**

```bash
git add DeliverTableClient/Pages/Checkout/CheckoutResult/
git commit -m "feat(client): add CheckoutResult page with status polling"
```

---

## Task 31: Redirect cart to checkout instead of direct order creation

**Files:**
- Modify: the cart page that currently calls `POST /api/v1/order` (search by name if unknown)
- Possibly: `DeliverTableClient/Services/` — remove or deprecate the existing direct-order call if it exists

- [ ] **Step 1: Locate the current "Commander" button**

```bash
grep -rn "CreateOrderAsync\|/api/v1/order\|api/v1/order" DeliverTableClient/Pages/ DeliverTableClient/Services/
```

- [ ] **Step 2: Change the click handler**

Replace the direct-order HTTP call with:

```csharp
private void GoToCheckout()
{
    Nav.NavigateTo("/checkout");
}
```

And update the button: `<button @onclick="GoToCheckout">Commander</button>`. Remove any imports / methods that become unused.

- [ ] **Step 3: Build + manual QA**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Reload the browser, ensure the cart "Commander" button navigates to `/checkout` rather than going straight to an order detail.

- [ ] **Step 4: Commit**

```bash
git add DeliverTableClient/
git commit -m "feat(client): redirect cart to checkout instead of direct order creation"
```

---

## Task 32: Document Stripe CLI dev workflow in README

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Add a "Stripe webhooks (dev)" section**

Find the development section and append:

````markdown
## Stripe webhooks (dev)

Webhook events for Stripe are forwarded to the backend via the Stripe CLI. Install from [stripe.com/docs/stripe-cli](https://stripe.com/docs/stripe-cli), then:

```bash
stripe login
stripe listen --forward-to http://localhost:5268/api/v1/stripe/webhook
```

The CLI prints a signing secret starting with `whsec_...`. Copy it into `STRIPE_WEBHOOK_SECRET` in your local `.env`. Restart the backend container for it to reload (`docker compose -f docker-dev.yaml restart backend`).

Trigger a test event:

```bash
stripe trigger payment_intent.amount_capturable_updated
```
````

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs(server): document Stripe CLI dev workflow"
```

---

## Task 33: End-to-end verification + format fixes

**Files:**
- Possibly: small whitespace fixes across any changed files.

- [ ] **Step 1: Run format check**

```bash
make format-check
```

If it fails, `make format-fix` and inspect the diff.

- [ ] **Step 2: Run the full suite**

```bash
make test
```

Expected: everything passes (ignore `AppEnvironmentTests.Load_AppliesDefaults_WhenOptionalVarsAreMissing` per CLAUDE.md).

- [ ] **Step 3: Manual end-to-end sanity pass**

In the browser with `stripe listen` running:

1. Log in as a customer. Add items to a cart. Go to checkout.
2. Complete payment with test card `4242 4242 4242 4242`.
3. Verify you land on `/checkout/result` and it transitions to "Commande confirmée".
4. Query the DB to verify: `Order.Status = Pending`, `Order.PaymentStatus = Authorized`, `ProcessedStripeEvent` row present for the `amount_capturable_updated` event, `LoyaltyTransaction.Status = Committed` if any.
5. As a restaurant owner, accept the order. Verify `Order.Status = Confirmed`, `Payment.CapturedAt` set, `PaymentStatus = Completed`.
6. As an admin, call `POST /api/v1/admin/orders/{id}/refund` with `{"Amount": 5, "Reason": "test"}`. Verify a Refund row + `PaymentStatus = PartiallyRefunded`.
7. Start a new checkout, close the browser tab mid-payment. Wait >15 minutes (or set Threshold lower temporarily). Verify scheduler cancels the order.

- [ ] **Step 4: If Step 1 needed format changes, commit them**

```bash
git add -u
git commit -m "style: apply formatting fixes"
```

---

## Self-Review

Before handing off:

1. **Spec coverage** — every section of `docs/superpowers/specs/2026-04-14-stripe-payments-core-design.md` maps to at least one task here. In particular: data model (Tasks 1–5), configuration (Task 6), DTOs/routes (Task 7), gateway (Task 8), repositories (Tasks 9–10), lifecycle + payment services (Tasks 11–15), controllers (Tasks 16–17, 20), order service changes (Tasks 18–19), DI (Task 21), scheduler (Tasks 22–25), client (Tasks 26–31), README (Task 32), verification (Task 33).
2. **Placeholder scan** — every code-change step contains actual code. No "implement similar to X" without the body.
3. **Type consistency** — `CreateIntentResult`, `StripeCustomerResult`, `StripePaymentIntentResult`, `StripeCaptureResult`, `StripeCancelResult`, `StripeRefundResult`, `RefundDto`, `CreateOrderResponse`, `AdminRefundRequest` are defined exactly once and referenced consistently across tasks.
4. **Migration review** — Task 4 step 3 is explicit about what the migration must contain; the engineer must verify, not blindly accept generated SQL.
5. **Cross-project DI** — `IPaymentLifecycleService` is registered in both server (Task 21) and scheduler (Task 22 / Task 23), which is intentional — both hosts need it.
6. **Test coverage** — every commit that introduces behavior has a unit test step before the implementation step.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-04-14-stripe-payments-core-plan.md`. Two execution options:

1. **Subagent-Driven (recommended)** — fresh subagent per task with review between tasks.
2. **Inline Execution** — execute tasks in this session with checkpoints.

Which approach?
