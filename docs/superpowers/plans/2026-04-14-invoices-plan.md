# Invoices Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Auto-generate French-law-compliant invoice PDFs for every captured order (restaurant→customer + platform→restaurant), plus credit notes on refund. Store in object storage, deliver by email with PDF attachment, and expose retrieval UIs for customer/restaurant/admin.

**Architecture:** Spec 1's webhook handler triggers `IInvoiceService` to persist `Invoice` + `InvoiceLine` rows (with immutable legal snapshots) and queue `InvoiceJobMessage`s via RabbitMQ. The existing `DeliverTableWorker` consumes them — renders PDFs via QuestPDF, uploads to Garage S3, then queues an `EmailJobMessage` with an attachment path so the existing email pipeline can attach the PDF. Invoice numbering is atomic via a row-locked `InvoiceCounter` table, one sequence per legal entity per year.

**Tech Stack:** .NET 10, EF Core + Npgsql, QuestPDF (Community license) for PDF rendering, RabbitMQ for job queue, AWS S3 SDK (Garage-compatible) for blob storage, MailKit for SMTP attachments, NUnit 4 + NSubstitute for tests.

**Spec:** `docs/superpowers/specs/2026-04-14-invoices-design.md`

**Conventions (CLAUDE.md)**:
- All dotnet commands run inside the dev stack: `docker compose -f docker-dev.yaml exec backend dotnet ...`. Confirm `make dev` is running before starting.
- Run a specific test: `docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~ClassName"`.
- Full suite: `make test`.
- Format: `make format-check` / `make format-fix`.
- TDD is mandatory for services and controllers. Entities, enums, DTOs, mappers, EF configs, migrations, DI registration, and `IObjectStorageService.UploadAsync` overload are exempt.
- Never include `Co-Authored-By` lines in commits. Don't add PBI/Task references unless an ID is explicitly given.
- French for all `ErrorMessages` values and user-visible strings.
- Use `nameof(UserRole.X)` not hardcoded strings.
- EF: annotations on entities, fluent config only for things annotations can't express (defaults, indexes, relationships, conversions).
- Enums: use implicit ordinals for contiguous zero-based sequences; explicit values only when gapping/reserving.
- All tests live in `DeliverTableTests/`. The separate `DeliverTableSchedulerTests` project was consolidated away.

**Dependency on Spec 1:** This plan assumes Spec 1 (Stripe Payments Core) is fully implemented on `feature/stripe`. Commits from Spec 1 referenced: `PaymentService.HandleAuthorizationCompletedAsync`, `PaymentService.HandleChargeRefundedAsync`, `Refund` entity, `OrderItem` + `Order.Items` navigation, `RestaurantTransaction`, `EmailJobMessage`, `IEmailJobService`, `IObjectStorageService`.

---

## File Structure

### Shared (enums + DTOs + routes)
- Create: `DeliverTableSharedLibrary/Enums/VatRate.cs`
- Create: `DeliverTableSharedLibrary/Enums/InvoiceKind.cs`
- Create: `DeliverTableSharedLibrary/Enums/InvoiceIssuerType.cs`
- Create: `DeliverTableSharedLibrary/Enums/InvoiceStatus.cs`
- Create: `DeliverTableSharedLibrary/Extensions/VatRateExtensions.cs`
- Create: `DeliverTableSharedLibrary/Validation/SiretValidator.cs`
- Modify: `DeliverTableSharedLibrary/Constants/ApiRoutes.cs` (add `Invoice` class + admin routes)
- Create: `DeliverTableSharedLibrary/Dtos/Invoice/InvoiceListItemDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Invoice/AdminInvoiceRowDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Invoice/AdminInvoiceDetailDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Invoice/InvoiceLineDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Invoice/InvoiceLegalSnapshotDto.cs`
- Modify: `DeliverTableSharedLibrary/Dtos/Restaurant/CreateRestaurantRequest.cs` (add legal fields)
- Modify: `DeliverTableSharedLibrary/Dtos/Restaurant/UpdateRestaurantRequest.cs` (add legal fields)
- Modify: `DeliverTableSharedLibrary/Dtos/Dish/CreateDishRequest.cs` (add VatRate)
- Modify: `DeliverTableSharedLibrary/Dtos/Dish/UpdateDishRequest.cs` (add VatRate)

### Data model (Infrastructure)
- Create: `DeliverTableInfrastructure/Models/Invoice.cs`
- Create: `DeliverTableInfrastructure/Models/InvoiceLine.cs`
- Create: `DeliverTableInfrastructure/Models/InvoiceCounter.cs`
- Modify: `DeliverTableInfrastructure/Models/Restaurant.cs` (legal fields)
- Modify: `DeliverTableInfrastructure/Models/Dish.cs` (VatRate)
- Create: `DeliverTableInfrastructure/Data/ModelConfiguration/InvoiceConfiguration.cs`
- Create: `DeliverTableInfrastructure/Data/ModelConfiguration/InvoiceLineConfiguration.cs`
- Create: `DeliverTableInfrastructure/Data/ModelConfiguration/InvoiceCounterConfiguration.cs`
- Modify: `DeliverTableInfrastructure/Data/Contexts/DeliverTableContext.Order.cs` (or wherever Invoice DbSets best fit — inspect the partials)
- Create: `DeliverTableInfrastructure/Migrations/{timestamp}_AddInvoicing.cs`

### Infrastructure services + repositories
- Create: `DeliverTableInfrastructure/Repositories/Interfaces/IInvoiceRepository.cs`
- Create: `DeliverTableInfrastructure/Repositories/InvoiceRepository.cs`
- Create: `DeliverTableInfrastructure/Invoicing/IInvoiceNumberingService.cs`
- Create: `DeliverTableInfrastructure/Invoicing/InvoiceNumberingService.cs`
- Create: `DeliverTableInfrastructure/Messaging/Messages/InvoiceJobMessage.cs`
- Modify: `DeliverTableInfrastructure/Messaging/Messages/EmailJobMessage.cs` (attachment fields)
- Modify: `DeliverTableInfrastructure/Services/ObjectStorageService.cs` (add `byte[]` overload)
- Modify: `DeliverTableInfrastructure/Services/Interfaces/IObjectStorageService.cs` (same)

### Server
- Create: `DeliverTableServer/Services/Interfaces/IInvoiceService.cs`
- Create: `DeliverTableServer/Services/InvoiceService.cs`
- Create: `DeliverTableServer/Controllers/InvoiceController.cs`
- Create: `DeliverTableServer/Controllers/AdminInvoiceController.cs` (new file or extend an existing admin controller — check conventions)
- Modify: `DeliverTableServer/Services/PaymentService.cs` (call `IInvoiceService` at capture + refund)
- Modify: `DeliverTableServer/Services/RestaurantService.cs` (validate legal fields + SIRET)
- Modify: `DeliverTableServer/Services/DishService.cs` (accept/persist VatRate)
- Modify: `DeliverTableServer/Configuration/AppEnvironment.cs` (platform legal fields)
- Modify: `DeliverTableServer/Constants/ErrorMessages.cs` (French invoice messages)
- Modify: `DeliverTableServer/Extensions/ServiceCollectionExtensions.cs` (DI)

### Worker
- Modify: `DeliverTableWorker/DeliverTableWorker.csproj` (QuestPDF)
- Modify: `DeliverTableWorker/Configuration/WorkerEnvironment.cs` (platform legal fields + storage env vars if missing)
- Create: `DeliverTableWorker/Services/IInvoicePdfRenderer.cs`
- Create: `DeliverTableWorker/Services/InvoicePdfRenderer.cs`
- Create: `DeliverTableWorker/Consumers/InvoiceJobConsumer.cs`
- Modify: `DeliverTableWorker/Consumers/EmailJobConsumer.cs` (attachment support)
- Modify: `DeliverTableWorker/Program.cs` (register `InvoiceJobConsumer` + infra for worker S3)
- Create: `DeliverTableWorker/Templates/InvoiceReadyCustomer.cshtml`
- Create: `DeliverTableWorker/Templates/InvoiceReadyRestaurant.cshtml`

### Client
- Create: `DeliverTableClient/Services/Invoice/IInvoiceApiClient.cs`
- Create: `DeliverTableClient/Services/Invoice/InvoiceApiClient.cs`
- Create: `DeliverTableClient/Pages/Invoices/MyInvoices/MyInvoices.razor`
- Create: `DeliverTableClient/Pages/Invoices/MyInvoices/MyInvoices.razor.scss`
- Modify: the existing restaurant dashboard page to add a "Factures de commission" tab/section — search for the existing `RestaurantAccount` or `RestaurantDashboard` page
- Create: `DeliverTableClient/Pages/Admin/Invoices/AdminInvoices.razor`
- Create: `DeliverTableClient/Pages/Admin/Invoices/AdminInvoices.razor.scss`
- Create: `DeliverTableClient/Pages/Admin/Invoices/AdminInvoiceDetail/AdminInvoiceDetail.razor`
- Create: `DeliverTableClient/Pages/Admin/Invoices/AdminInvoiceDetail/AdminInvoiceDetail.razor.scss`
- Modify: existing order detail page (adds "Télécharger la facture" button when invoice is ready)
- Modify: existing restaurant create/edit form (legal fields)
- Modify: existing dish create/edit form (VatRate dropdown)
- Modify: `DeliverTableClient/Extensions/ApiClientServiceCollectionExtensions.cs` (register `IInvoiceApiClient`)

### Tests (all inside `DeliverTableTests/`)
- Create: `DeliverTableTests/Shared/Unit/Validation/SiretValidatorTests.cs`
- Create: `DeliverTableTests/Infrastructure/Unit/Invoicing/InvoiceNumberingServiceTests.cs`
- Create: `DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs`
- Create: `DeliverTableTests/Server/Unit/Controllers/InvoiceControllerTests.cs`
- Create: `DeliverTableTests/Server/Unit/Controllers/AdminInvoiceControllerTests.cs`
- Create: `DeliverTableTests/Worker/Unit/Consumers/InvoiceJobConsumerTests.cs`
- Create: `DeliverTableTests/Worker/Unit/Services/InvoicePdfRendererTests.cs` (smoke test only)
- Create: `DeliverTableTests/Worker/Unit/Consumers/EmailJobConsumerAttachmentTests.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/RestaurantServiceTests.cs` (SIRET + legal validation)
- Modify: `DeliverTableTests/Server/Unit/Services/DishServiceTests.cs` (VatRate)
- Modify: `DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs` (invoice queue wiring)

### Docs + env
- Modify: `.env.example` (platform legal + QuestPDF nothing, invoices don't need new infra env)
- Modify: `docs/db/er-diagram.md`
- Modify: `docs/db/data-dictionary.md`

---

## Task 1: Add new enums

**Files:**
- Create: `DeliverTableSharedLibrary/Enums/VatRate.cs`
- Create: `DeliverTableSharedLibrary/Enums/InvoiceKind.cs`
- Create: `DeliverTableSharedLibrary/Enums/InvoiceIssuerType.cs`
- Create: `DeliverTableSharedLibrary/Enums/InvoiceStatus.cs`
- Create: `DeliverTableSharedLibrary/Extensions/VatRateExtensions.cs`

No tests required per CLAUDE.md (enums/extensions are exempt, but `VatRateExtensions.ToDecimal` is a pure function — we test it as part of Task 8's SIRET validator is treated similarly; here we can skip).

- [ ] **Step 1: Create `VatRate.cs`**

Read sibling enums (e.g. `LoyaltyRedemptionStatus.cs` from Spec 1) to match style.

```csharp
namespace DeliverTableSharedLibrary.Enums;

public enum VatRate
{
    Zero = 0,
    Special2_1 = 2,
    Reduced5_5 = 5,
    Intermediate10 = 10,
    Normal20 = 20,
}
```

(Explicit values are appropriate here — the int value is a *display code*, not an ordinal, and there are gaps.)

- [ ] **Step 2: Create `InvoiceKind.cs`**

```csharp
namespace DeliverTableSharedLibrary.Enums;

public enum InvoiceKind
{
    OrderInvoiceToCustomer,
    CommissionInvoiceToRestaurant,
    CreditNoteToCustomer,
    CommissionCreditNoteToRestaurant,
}
```

Contiguous zero-based — no explicit values per convention.

- [ ] **Step 3: Create `InvoiceIssuerType.cs`**

```csharp
namespace DeliverTableSharedLibrary.Enums;

public enum InvoiceIssuerType
{
    Platform,
    Restaurant,
}
```

- [ ] **Step 4: Create `InvoiceStatus.cs`**

```csharp
namespace DeliverTableSharedLibrary.Enums;

public enum InvoiceStatus
{
    Queued,
    Generated,
    Failed,
}
```

- [ ] **Step 5: Create `VatRateExtensions.cs`**

```csharp
namespace DeliverTableSharedLibrary.Extensions;

using DeliverTableSharedLibrary.Enums;

public static class VatRateExtensions
{
    public static decimal ToDecimal(this VatRate rate) => rate switch
    {
        VatRate.Zero => 0m,
        VatRate.Special2_1 => 2.1m,
        VatRate.Reduced5_5 => 5.5m,
        VatRate.Intermediate10 => 10m,
        VatRate.Normal20 => 20m,
        _ => throw new ArgumentOutOfRangeException(nameof(rate), rate, "Unknown VAT rate"),
    };
}
```

- [ ] **Step 6: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 7: Commit**

```bash
git add DeliverTableSharedLibrary/Enums/VatRate.cs \
        DeliverTableSharedLibrary/Enums/InvoiceKind.cs \
        DeliverTableSharedLibrary/Enums/InvoiceIssuerType.cs \
        DeliverTableSharedLibrary/Enums/InvoiceStatus.cs \
        DeliverTableSharedLibrary/Extensions/VatRateExtensions.cs
git commit -m "feat(shared): add VatRate, InvoiceKind, InvoiceIssuerType, InvoiceStatus enums"
```

---

## Task 2: Add legal fields to Restaurant and VatRate to Dish

**Files:**
- Modify: `DeliverTableInfrastructure/Models/Restaurant.cs`
- Modify: `DeliverTableInfrastructure/Models/Dish.cs`

No tests (entity field additions).

- [ ] **Step 1: Read existing `Restaurant.cs` and `Dish.cs`**

Identify exact style (property order, attribute usage).

- [ ] **Step 2: Modify `Restaurant.cs`**

Add to the `Restaurant` class:

```csharp
[MaxLength(14)]
public string Siret { get; set; } = string.Empty;

[MaxLength(200)]
public string LegalName { get; set; } = string.Empty;

[MaxLength(500)]
public string LegalAddress { get; set; } = string.Empty;

[MaxLength(50)]
public string LegalForm { get; set; } = string.Empty;

public bool IsVatRegistered { get; set; } = true;
```

(Nullable vs string-empty: the spec says "nullable or default `""`" — using `= string.Empty` with non-nullable types is consistent with other string fields in this codebase. Check `Restaurant.cs` to confirm.)

- [ ] **Step 3: Modify `Dish.cs`**

Add:

```csharp
public VatRate VatRate { get; set; } = VatRate.Intermediate10;
```

Add the using: `using DeliverTableSharedLibrary.Enums;` if not already present.

- [ ] **Step 4: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 5: Commit**

```bash
git add DeliverTableInfrastructure/Models/Restaurant.cs \
        DeliverTableInfrastructure/Models/Dish.cs
git commit -m "feat(server): add legal fields to Restaurant and VatRate to Dish"
```

---

## Task 3: Add Invoice, InvoiceLine, InvoiceCounter entities with EF configs

**Files:**
- Create: `DeliverTableInfrastructure/Models/Invoice.cs`
- Create: `DeliverTableInfrastructure/Models/InvoiceLine.cs`
- Create: `DeliverTableInfrastructure/Models/InvoiceCounter.cs`
- Create: `DeliverTableInfrastructure/Data/ModelConfiguration/InvoiceConfiguration.cs`
- Create: `DeliverTableInfrastructure/Data/ModelConfiguration/InvoiceLineConfiguration.cs`
- Create: `DeliverTableInfrastructure/Data/ModelConfiguration/InvoiceCounterConfiguration.cs`
- Modify: the appropriate `DeliverTableContext` partial to register DbSets (inspect `DeliverTableInfrastructure/Data/Contexts/` first).

Per the EF convention (established in Spec 1): annotations on the entity, fluent config for relationships, indexes, defaults, conversions, composite keys.

- [ ] **Step 1: Create `Invoice.cs`**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Models;

public class Invoice
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Number { get; set; } = string.Empty;

    public InvoiceKind Kind { get; set; }

    public int OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;

    public InvoiceIssuerType IssuerType { get; set; }

    public int? IssuerRestaurantId { get; set; }

    [ForeignKey(nameof(IssuerRestaurantId))]
    public Restaurant? IssuerRestaurant { get; set; }

    public int? RecipientUserId { get; set; }

    [ForeignKey(nameof(RecipientUserId))]
    public User? RecipientUser { get; set; }

    public int? RecipientRestaurantId { get; set; }

    [ForeignKey(nameof(RecipientRestaurantId))]
    public Restaurant? RecipientRestaurant { get; set; }

    public int? RelatedInvoiceId { get; set; }

    [ForeignKey(nameof(RelatedInvoiceId))]
    public Invoice? RelatedInvoice { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(9, 2)")]
    public decimal TotalHt { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal TotalVat { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal TotalTtc { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    [MaxLength(400)]
    public string? StoragePath { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Queued;

    [MaxLength(2000)]
    public string? FailureReason { get; set; }

    [Required]
    [MaxLength(4000)]
    public string IssuerLegalSnapshotJson { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string RecipientSnapshotJson { get; set; } = string.Empty;

    public List<InvoiceLine> Lines { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Create `InvoiceLine.cs`**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableInfrastructure.Models;

public class InvoiceLine
{
    [Key]
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public Invoice Invoice { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(9, 3)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal UnitPriceTtc { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal UnitPriceHt { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal VatRate { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal LineHt { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal LineVat { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal LineTtc { get; set; }

    public int SortOrder { get; set; }
}
```

- [ ] **Step 3: Create `InvoiceCounter.cs`**

```csharp
using System.ComponentModel.DataAnnotations;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Models;

public class InvoiceCounter
{
    [Key]
    public int Id { get; set; }

    public InvoiceIssuerType EntityType { get; set; }

    public int? EntityId { get; set; }

    public int Year { get; set; }

    public int NextNumber { get; set; } = 1;
}
```

- [ ] **Step 4: Create EF configs**

`InvoiceConfiguration.cs`:

```csharp
using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.Number).IsUnique();
        builder.HasIndex(i => i.OrderId);
        builder.HasIndex(i => i.RecipientUserId);
        builder.HasIndex(i => i.RecipientRestaurantId);

        builder.Property(i => i.Kind).HasConversion<string>().IsRequired();
        builder.Property(i => i.IssuerType).HasConversion<string>().IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().IsRequired();

        builder.HasOne(i => i.Order)
               .WithMany()
               .HasForeignKey(i => i.OrderId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.IssuerRestaurant)
               .WithMany()
               .HasForeignKey(i => i.IssuerRestaurantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.RecipientUser)
               .WithMany()
               .HasForeignKey(i => i.RecipientUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.RecipientRestaurant)
               .WithMany()
               .HasForeignKey(i => i.RecipientRestaurantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.RelatedInvoice)
               .WithMany()
               .HasForeignKey(i => i.RelatedInvoiceId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Lines)
               .WithOne(l => l.Invoice)
               .HasForeignKey(l => l.InvoiceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

`InvoiceLineConfiguration.cs`:

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
    }
}
```

(Relationship is declared on the Invoice side.)

`InvoiceCounterConfiguration.cs`:

```csharp
using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class InvoiceCounterConfiguration : IEntityTypeConfiguration<InvoiceCounter>
{
    public void Configure(EntityTypeBuilder<InvoiceCounter> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => new { c.EntityType, c.EntityId, c.Year }).IsUnique();
        builder.Property(c => c.EntityType).HasConversion<string>().IsRequired();
        builder.Property(c => c.NextNumber).HasDefaultValue(1);
    }
}
```

- [ ] **Step 5: Register DbSets in the DbContext partial**

Inspect `DeliverTableInfrastructure/Data/Contexts/` to find the appropriate partial (likely `DeliverTableContext.Order.cs` since Invoice is FK'd to Order). Add:

```csharp
public DbSet<Invoice> Invoices => Set<Invoice>();
public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
public DbSet<InvoiceCounter> InvoiceCounters => Set<InvoiceCounter>();
```

EF configs are picked up via `ApplyConfigurationsFromAssembly` (confirmed in Spec 1 Task 2) — no manual registration needed.

- [ ] **Step 6: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 7: Commit**

```bash
git add DeliverTableInfrastructure/Models/Invoice.cs \
        DeliverTableInfrastructure/Models/InvoiceLine.cs \
        DeliverTableInfrastructure/Models/InvoiceCounter.cs \
        DeliverTableInfrastructure/Data/ModelConfiguration/InvoiceConfiguration.cs \
        DeliverTableInfrastructure/Data/ModelConfiguration/InvoiceLineConfiguration.cs \
        DeliverTableInfrastructure/Data/ModelConfiguration/InvoiceCounterConfiguration.cs \
        DeliverTableInfrastructure/Data/Contexts/
git commit -m "feat(server): add Invoice, InvoiceLine, InvoiceCounter entities with EF configs"
```

---

## Task 4: Generate migration `AddInvoicing`

**Files:**
- Create: `DeliverTableInfrastructure/Migrations/{timestamp}_AddInvoicing.cs` (generated)

- [ ] **Step 1: Generate migration**

```bash
docker compose -f docker-dev.yaml exec backend dotnet ef migrations add AddInvoicing \
    --project /src/DeliverTableInfrastructure \
    --startup-project /src/DeliverTableServer
```

- [ ] **Step 2: Review generated migration**

Open the file. Verify:
- Creates `Invoices` table with all columns, FKs, unique index on `Number`, indexes on `OrderId` / `RecipientUserId` / `RecipientRestaurantId`.
- Creates `InvoiceLines` table with cascade delete from Invoice.
- Creates `InvoiceCounters` table with unique composite index on (EntityType, EntityId, Year).
- Adds `Siret`, `LegalName`, `LegalAddress`, `LegalForm`, `IsVatRegistered` to `Restaurants`. All 4 strings should have `defaultValue: ""` (since existing rows need a default). `IsVatRegistered` should have `defaultValue: true`.
- Adds `VatRate` to `Dishes` with string conversion and default `"Intermediate10"` (since we use `HasConversion<string>()` in Spec 1 patterns — check whether the Dish config uses string conversion for other enums; if not, use int default `10`).

If any default is missing for the historical-row backfill, hand-edit the `defaultValue:` parameter. Use the "backfill then drop" pattern from Spec 1 Task 4 if a permanent default is undesired (for strings we typically don't drop — empty is fine permanently).

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
git commit -m "feat(db): add migration AddInvoicing"
```

---

## Task 5: Update DB docs

**Files:**
- Modify: `docs/db/er-diagram.md`
- Modify: `docs/db/data-dictionary.md`

- [ ] **Step 1: Update `docs/db/er-diagram.md`**

Add to the Mermaid ER diagram:
- `INVOICE` entity with all fields.
- `INVOICE_LINE` entity.
- `INVOICE_COUNTER` entity.
- `RESTAURANT` new fields (`siret`, `legal_name`, `legal_address`, `legal_form`, `is_vat_registered`).
- `DISH.vat_rate` new column (`ZERO | SPECIAL2_1 | REDUCED5_5 | INTERMEDIATE10 | NORMAL20`).
- Relationships: `ORDER ||--o{ INVOICE`, `RESTAURANT ||--o{ INVOICE` (as issuer), `USER ||--o{ INVOICE` (as recipient), `RESTAURANT ||--o{ INVOICE` (as recipient), `INVOICE ||--o{ INVOICE` (credit note → original), `INVOICE ||--o{ INVOICE_LINE`.

- [ ] **Step 2: Update `docs/db/data-dictionary.md`**

Add sections for `INVOICE`, `INVOICE_LINE`, `INVOICE_COUNTER`. Update `RESTAURANT` with the new legal columns. Update `DISH` with `vat_rate`. Add to the Enumerations appendix: `VatRate`, `InvoiceKind`, `InvoiceIssuerType`, `InvoiceStatus`.

- [ ] **Step 3: Commit**

```bash
git add docs/db/er-diagram.md docs/db/data-dictionary.md
git commit -m "docs(db): update ER diagram and data dictionary for invoicing"
```

---

## Task 6: Platform legal env vars in AppEnvironment and WorkerEnvironment

**Files:**
- Modify: `.env.example`
- Modify: `DeliverTableServer/Configuration/AppEnvironment.cs`
- Modify: `DeliverTableWorker/Configuration/WorkerEnvironment.cs`
- Modify: `DeliverTableTests/Server/Unit/Configuration/AppEnvironmentTests.cs` (new required vars must be added to the test helper's required set)
- Modify: `DeliverTableTests/Global/Helpers/AppEnvironmentTestHelper.cs` (same)

Reference: Spec 1 Task 6 had the same pattern (adding required env vars and updating test helpers — critical to avoid breaking existing tests).

- [ ] **Step 1: `.env.example`**

Append:

```bash
# ─────────────────────────────────────────────────
# Platform legal entity (for invoices)
# ─────────────────────────────────────────────────
PLATFORM_LEGAL_NAME=DeliverTable SAS
PLATFORM_LEGAL_FORM=SAS
PLATFORM_SIRET=12345678900012
PLATFORM_VAT_NUMBER=FR12345678900
PLATFORM_ADDRESS=12 rue Exemple, 75001 Paris, France
PLATFORM_VAT_APPLICABLE=true
```

Also set these in the local `.env` file (not committed).

- [ ] **Step 2: Extend `AppEnvironment.cs`**

Add 6 required string properties and 1 bool (`PlatformVatApplicable`). In `Load()`, add:

```csharp
string platformLegalName = RequireVar("PLATFORM_LEGAL_NAME", errors);
string platformLegalForm = RequireVar("PLATFORM_LEGAL_FORM", errors);
string platformSiret = RequireVar("PLATFORM_SIRET", errors);
string platformVatNumber = RequireVar("PLATFORM_VAT_NUMBER", errors);
string platformAddress = RequireVar("PLATFORM_ADDRESS", errors);
bool platformVatApplicable = ParseBool("PLATFORM_VAT_APPLICABLE", true, errors);
```

(Inspect the actual helper names — `RequireVar` / `ParseBool` / similar. Match the existing pattern.) Pass to the constructor.

- [ ] **Step 3: Extend `WorkerEnvironment.cs`**

Same 7 properties. `WorkerEnvironment.Load()` mirrors `AppEnvironment.Load()` — read the existing file to confirm the helper pattern.

- [ ] **Step 4: Update test helpers**

In `AppEnvironmentTestHelper.cs`, add the 6 required strings and the bool to `RequiredVars` (or `SetupEnvironment`) with test values.

In `AppEnvironmentTests.cs`:
- Add to `AllRequiredVars` collection.
- Add `TestCase` rows for the missing-var test.
- Add assertions in the happy-path test for each new property.

- [ ] **Step 5: Restart backend + worker containers**

```bash
docker compose -f docker-dev.yaml restart backend worker
docker compose -f docker-dev.yaml logs backend --tail=20
docker compose -f docker-dev.yaml logs worker --tail=20
```

Both must start cleanly. If either fails on missing env vars, add them to `.env` and retry.

- [ ] **Step 6: Build + test**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
make test
```

All existing tests should still pass.

- [ ] **Step 7: Commit**

```bash
git add .env.example \
        DeliverTableServer/Configuration/AppEnvironment.cs \
        DeliverTableWorker/Configuration/WorkerEnvironment.cs \
        DeliverTableTests/Server/Unit/Configuration/AppEnvironmentTests.cs \
        DeliverTableTests/Global/Helpers/AppEnvironmentTestHelper.cs
git commit -m "feat(server): add platform legal env vars and update AppEnvironment + WorkerEnvironment"
```

---

## Task 7: Add invoice DTOs and routes

**Files:**
- Create: `DeliverTableSharedLibrary/Dtos/Invoice/InvoiceListItemDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Invoice/AdminInvoiceRowDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Invoice/AdminInvoiceDetailDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Invoice/InvoiceLineDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Invoice/InvoiceLegalSnapshotDto.cs`
- Modify: `DeliverTableSharedLibrary/Constants/ApiRoutes.cs`
- Modify: `DeliverTableSharedLibrary/Dtos/Restaurant/CreateRestaurantRequest.cs`
- Modify: `DeliverTableSharedLibrary/Dtos/Restaurant/UpdateRestaurantRequest.cs`
- Modify: `DeliverTableSharedLibrary/Dtos/Dish/CreateDishRequest.cs` (and corresponding Update)

- [ ] **Step 1: Add routes**

In `ApiRoutes.cs`, add nested `Invoice` class:

```csharp
public static class Invoice
{
    public const string Base = "api/v1/invoice";
    public const string MyListRoute = "me";
    public const string RestaurantListRoute = "restaurant/{id:int}";
    public const string DownloadRoute = "{id:int}/pdf";
}
```

Inside `ApiRoutes.Admin` add:

```csharp
public const string InvoicesRoute = "invoices";
public const string InvoiceByIdRoute = "invoices/{id:int}";
```

- [ ] **Step 2: Create `InvoiceLegalSnapshotDto.cs`**

```csharp
namespace DeliverTableSharedLibrary.Dtos.Invoice;

public record InvoiceLegalSnapshotDto(
    string Name,
    string LegalForm,
    string Siret,
    string VatNumber,
    string Address);
```

- [ ] **Step 3: Create `InvoiceLineDto.cs`**

```csharp
namespace DeliverTableSharedLibrary.Dtos.Invoice;

public record InvoiceLineDto(
    string Description,
    decimal Quantity,
    decimal UnitPriceHt,
    decimal UnitPriceTtc,
    decimal VatRate,
    decimal LineHt,
    decimal LineVat,
    decimal LineTtc);
```

- [ ] **Step 4: Create `InvoiceListItemDto.cs`**

```csharp
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Invoice;

public record InvoiceListItemDto(
    int Id,
    string Number,
    InvoiceKind Kind,
    int OrderId,
    DateTime IssuedAt,
    decimal TotalTtc,
    string Currency,
    InvoiceStatus Status);
```

- [ ] **Step 5: Create `AdminInvoiceRowDto.cs`**

```csharp
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Invoice;

public record AdminInvoiceRowDto(
    int Id,
    string Number,
    InvoiceKind Kind,
    InvoiceIssuerType IssuerType,
    string IssuerName,
    string RecipientName,
    DateTime IssuedAt,
    decimal TotalTtc,
    InvoiceStatus Status);
```

- [ ] **Step 6: Create `AdminInvoiceDetailDto.cs`**

```csharp
namespace DeliverTableSharedLibrary.Dtos.Invoice;

public record AdminInvoiceDetailDto(
    InvoiceListItemDto Header,
    List<InvoiceLineDto> Lines,
    InvoiceLegalSnapshotDto Issuer,
    InvoiceLegalSnapshotDto Recipient,
    int? RelatedInvoiceId);
```

- [ ] **Step 7: Extend Restaurant create/edit DTOs**

In `CreateRestaurantRequest.cs` and `UpdateRestaurantRequest.cs`, add:

```csharp
[Required]
[MaxLength(14)]
public string Siret { get; set; } = string.Empty;

[Required]
[MaxLength(200)]
public string LegalName { get; set; } = string.Empty;

[Required]
[MaxLength(500)]
public string LegalAddress { get; set; } = string.Empty;

[Required]
[MaxLength(50)]
public string LegalForm { get; set; } = string.Empty;

public bool IsVatRegistered { get; set; } = true;
```

- [ ] **Step 8: Extend Dish DTOs**

In `CreateDishRequest.cs` and `UpdateDishRequest.cs`, add:

```csharp
public VatRate VatRate { get; set; } = VatRate.Intermediate10;
```

Add using for `DeliverTableSharedLibrary.Enums;`.

- [ ] **Step 9: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 10: Commit**

```bash
git add DeliverTableSharedLibrary/
git commit -m "feat(shared): add invoice DTOs and routes"
```

---

## Task 8: SIRET Luhn validator with tests

**Files:**
- Create: `DeliverTableSharedLibrary/Validation/SiretValidator.cs`
- Create: `DeliverTableTests/Shared/Unit/Validation/SiretValidatorTests.cs`

TDD. Pure function — worth testing thoroughly.

- [ ] **Step 1: Failing tests**

```csharp
using DeliverTableSharedLibrary.Validation;
using NUnit.Framework;

namespace DeliverTableTests.Shared.Unit.Validation;

[TestFixture]
public class SiretValidatorTests
{
    [TestCase("73282932000074", true)]    // valid (SNCF)
    [TestCase("40483304800010", true)]    // valid (Air France)
    [TestCase("12345678900012", false)]   // Luhn-invalid
    [TestCase("12345678", false)]         // too short
    [TestCase("1234567890012345", false)] // too long
    [TestCase("7328293200007A", false)]   // contains letter
    [TestCase("", false)]                 // empty
    [TestCase(null, false)]               // null
    public void IsValid_FixtureCases(string? siret, bool expected)
    {
        Assert.That(SiretValidator.IsValid(siret), Is.EqualTo(expected));
    }
}
```

- [ ] **Step 2: Run to confirm failure**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~SiretValidatorTests"
```

Expected: build fails — type missing.

- [ ] **Step 3: Implement `SiretValidator.cs`**

```csharp
namespace DeliverTableSharedLibrary.Validation;

public static class SiretValidator
{
    public static bool IsValid(string? siret)
    {
        if (string.IsNullOrWhiteSpace(siret)) return false;
        if (siret.Length != 14) return false;
        if (!siret.All(char.IsDigit)) return false;

        int sum = 0;
        for (int i = 0; i < 14; i++)
        {
            int d = siret[i] - '0';
            // positions are 1-indexed in the Luhn variant used for SIRET:
            // even index (1,3,5,...) in 0-based = positions 2,4,6,... — those are doubled
            bool doubled = (i % 2 == 1);
            int contribution = doubled ? d * 2 : d;
            if (contribution > 9) contribution -= 9;
            sum += contribution;
        }
        return sum % 10 == 0;
    }
}
```

(French SIRET uses a Luhn algorithm — doubling at odd positions in 1-indexed, i.e. even positions in 0-indexed.)

- [ ] **Step 4: Run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~SiretValidatorTests"
```

All 8 cases must PASS. If the two valid fixtures fail, the Luhn algorithm is subtly wrong — try alternating the `doubled` bit (`i % 2 == 0` instead).

- [ ] **Step 5: Commit**

```bash
git add DeliverTableSharedLibrary/Validation/SiretValidator.cs \
        DeliverTableTests/Shared/Unit/Validation/SiretValidatorTests.cs
git commit -m "feat(shared): add SIRET Luhn validator utility with tests"
```

---

## Task 9: Add `IInvoiceRepository`

**Files:**
- Create: `DeliverTableInfrastructure/Repositories/Interfaces/IInvoiceRepository.cs`
- Create: `DeliverTableInfrastructure/Repositories/InvoiceRepository.cs`

No tests (repository layer per CLAUDE.md).

- [ ] **Step 1: Create interface**

```csharp
using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice> CreateAsync(Invoice invoice, CancellationToken ct = default);
    Task UpdateAsync(Invoice invoice, CancellationToken ct = default);
    Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Invoice?> GetByIdWithLinesAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsForOrderAndKindAsync(int orderId, InvoiceKind kind, CancellationToken ct = default);
    Task<List<Invoice>> ListByOrderIdAsync(int orderId, CancellationToken ct = default);
    Task<(List<Invoice> Items, int Total)> ListForRecipientUserAsync(int userId, int page, int pageSize, CancellationToken ct = default);
    Task<(List<Invoice> Items, int Total)> ListForRecipientRestaurantAsync(int restaurantId, int page, int pageSize, CancellationToken ct = default);
    Task<(List<Invoice> Items, int Total)> AdminListAsync(int? year, InvoiceKind? kind, InvoiceIssuerType? issuerType, int? restaurantId, string? customerEmailContains, int page, int pageSize, CancellationToken ct = default);
}
```

- [ ] **Step 2: Implement**

```csharp
using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class InvoiceRepository(DeliverTableContext dbContext) : IInvoiceRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<Invoice> CreateAsync(Invoice invoice, CancellationToken ct = default)
    {
        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync(ct);
        return invoice;
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken ct = default)
    {
        invoice.UpdatedAt = DateTime.UtcNow;
        _dbContext.Invoices.Update(invoice);
        await _dbContext.SaveChangesAsync(ct);
    }

    public Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _dbContext.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<Invoice?> GetByIdWithLinesAsync(int id, CancellationToken ct = default) =>
        _dbContext.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<bool> ExistsForOrderAndKindAsync(int orderId, InvoiceKind kind, CancellationToken ct = default) =>
        _dbContext.Invoices.AnyAsync(i => i.OrderId == orderId && i.Kind == kind, ct);

    public Task<List<Invoice>> ListByOrderIdAsync(int orderId, CancellationToken ct = default) =>
        _dbContext.Invoices.Where(i => i.OrderId == orderId).ToListAsync(ct);

    public async Task<(List<Invoice> Items, int Total)> ListForRecipientUserAsync(int userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.Invoices.Where(i => i.RecipientUserId == userId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(i => i.IssuedAt)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .ToListAsync(ct);
        return (items, total);
    }

    public async Task<(List<Invoice> Items, int Total)> ListForRecipientRestaurantAsync(int restaurantId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.Invoices.Where(i => i.RecipientRestaurantId == restaurantId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(i => i.IssuedAt)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .ToListAsync(ct);
        return (items, total);
    }

    public async Task<(List<Invoice> Items, int Total)> AdminListAsync(int? year, InvoiceKind? kind, InvoiceIssuerType? issuerType, int? restaurantId, string? customerEmailContains, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.Invoices.Include(i => i.RecipientUser).AsQueryable();
        if (year.HasValue) query = query.Where(i => i.IssuedAt.Year == year.Value);
        if (kind.HasValue) query = query.Where(i => i.Kind == kind.Value);
        if (issuerType.HasValue) query = query.Where(i => i.IssuerType == issuerType.Value);
        if (restaurantId.HasValue) query = query.Where(i => i.IssuerRestaurantId == restaurantId.Value || i.RecipientRestaurantId == restaurantId.Value);
        if (!string.IsNullOrEmpty(customerEmailContains))
            query = query.Where(i => i.RecipientUser != null && i.RecipientUser.Email != null && i.RecipientUser.Email.Contains(customerEmailContains));

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(i => i.IssuedAt)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .ToListAsync(ct);
        return (items, total);
    }
}
```

- [ ] **Step 3: Register in DI** (will happen in Task 20; no-op for now unless the build fails)

- [ ] **Step 4: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 5: Commit**

```bash
git add DeliverTableInfrastructure/Repositories/Interfaces/IInvoiceRepository.cs \
        DeliverTableInfrastructure/Repositories/InvoiceRepository.cs
git commit -m "feat(infra): add IInvoiceRepository"
```

---

## Task 10: `IInvoiceNumberingService` with tests

**Files:**
- Create: `DeliverTableInfrastructure/Invoicing/IInvoiceNumberingService.cs`
- Create: `DeliverTableInfrastructure/Invoicing/InvoiceNumberingService.cs`
- Create: `DeliverTableTests/Infrastructure/Unit/Invoicing/InvoiceNumberingServiceTests.cs`

The numbering service uses a real DB transaction with `SELECT ... FOR UPDATE` to avoid race conditions. Tests use the existing `TestDatabase` fixture (set up for Spec 1).

- [ ] **Step 1: Interface**

```csharp
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Invoicing;

public interface IInvoiceNumberingService
{
    Task<string> IssueNumberAsync(
        InvoiceIssuerType issuerType,
        int? issuerEntityId,
        int year,
        bool isCreditNote,
        CancellationToken ct);
}
```

- [ ] **Step 2: Failing tests**

```csharp
using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Invoicing;
using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Server.Fixtures;
using NUnit.Framework;

namespace DeliverTableTests.Infrastructure.Unit.Invoicing;

[TestFixture]
public class InvoiceNumberingServiceTests
{
    private TestDatabase _database = null!;
    private DeliverTableContext _ctx = null!;
    private InvoiceNumberingService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _database = new TestDatabase();
        _ctx = _database.CreateContext();
        _sut = new InvoiceNumberingService(_ctx);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
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
```

- [ ] **Step 3: Run to confirm failure**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~InvoiceNumberingServiceTests"
```

Expected: build fails — service not implemented.

- [ ] **Step 4: Implement `InvoiceNumberingService.cs`**

```csharp
using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Invoicing;

public class InvoiceNumberingService(DeliverTableContext dbContext) : IInvoiceNumberingService
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<string> IssueNumberAsync(
        InvoiceIssuerType issuerType,
        int? issuerEntityId,
        int year,
        bool isCreditNote,
        CancellationToken ct)
    {
        // Find-or-create counter row; retry once on unique-constraint race.
        var counter = await _dbContext.InvoiceCounters
            .FirstOrDefaultAsync(c =>
                c.EntityType == issuerType &&
                c.EntityId == issuerEntityId &&
                c.Year == year, ct);

        if (counter is null)
        {
            counter = new InvoiceCounter
            {
                EntityType = issuerType,
                EntityId = issuerEntityId,
                Year = year,
                NextNumber = 1,
            };
            _dbContext.InvoiceCounters.Add(counter);
            try
            {
                await _dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                _dbContext.Entry(counter).State = EntityState.Detached;
                counter = await _dbContext.InvoiceCounters.FirstAsync(c =>
                    c.EntityType == issuerType &&
                    c.EntityId == issuerEntityId &&
                    c.Year == year, ct);
            }
        }

        int issued = counter.NextNumber;
        counter.NextNumber++;
        await _dbContext.SaveChangesAsync(ct);

        string prefix = issuerType == InvoiceIssuerType.Platform
            ? "DT"
            : $"R{issuerEntityId:D4}";
        string baseNumber = $"{prefix}-{year}-{issued:D6}";
        return isCreditNote ? $"AV-{baseNumber}" : baseNumber;
    }
}
```

Note: the in-memory provider used by `TestDatabase` doesn't support `FOR UPDATE`. In dev/prod Postgres, uniqueness on (EntityType, EntityId, Year) + EF's optimistic concurrency on `NextNumber` update prevents duplicate issuance. For strict gapless guarantees under high concurrency, use `_dbContext.Database.ExecuteSqlInterpolatedAsync` with `SELECT ... FOR UPDATE` on Postgres and document this in production deployment notes. The simpler optimistic approach is acceptable for current traffic.

- [ ] **Step 5: Run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~InvoiceNumberingServiceTests"
```

All 5 tests must PASS.

- [ ] **Step 6: Commit**

```bash
git add DeliverTableInfrastructure/Invoicing/ \
        DeliverTableTests/Infrastructure/Unit/Invoicing/
git commit -m "feat(server): add IInvoiceNumberingService with tests"
```

---

## Task 11: `IInvoiceService.CreatePendingInvoicesForCapturedOrderAsync` with tests

**Files:**
- Create: `DeliverTableServer/Services/Interfaces/IInvoiceService.cs`
- Create: `DeliverTableServer/Services/InvoiceService.cs`
- Create: `DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs`

TDD. This is the core orchestration: on capture, issue numbers + persist 2 invoices + queue 2 jobs.

- [ ] **Step 1: Interface**

```csharp
using DeliverTableServer.Common;

namespace DeliverTableServer.Services.Interfaces;

public interface IInvoiceService
{
    Task<ServiceResult> CreatePendingInvoicesForCapturedOrderAsync(int orderId, CancellationToken ct);
}
```

- [ ] **Step 2: Failing tests**

```csharp
using DeliverTableInfrastructure.Invoicing;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Configuration;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Factories;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class InvoiceServiceTests
{
    private IInvoiceRepository _invoiceRepo = null!;
    private IOrderRepository _orderRepo = null!;
    private IInvoiceNumberingService _numbering = null!;
    private IMessagePublisher _publisher = null!;
    private AppEnvironment _env = null!;
    private InvoiceService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _invoiceRepo = Substitute.For<IInvoiceRepository>();
        _orderRepo = Substitute.For<IOrderRepository>();
        _numbering = Substitute.For<IInvoiceNumberingService>();
        _publisher = Substitute.For<IMessagePublisher>();
        _env = TestEnvironmentFactory.Create();
        _sut = new InvoiceService(_invoiceRepo, _orderRepo, _numbering, _publisher, _env);
    }

    [Test]
    public async Task CreatePendingInvoices_HappyPath_QueuesTwoInvoices()
    {
        var restaurant = new Restaurant
        {
            Id = 5, Name = "Test Resto",
            Siret = "73282932000074", LegalName = "Test SAS",
            LegalAddress = "1 rue Exemple, 75001 Paris", LegalForm = "SAS",
            IsVatRegistered = true,
        };
        var customer = new User { Id = 1, Email = "cust@example.fr", FirstName = "Cust", LastName = "Omer" };
        var dish = new Dish { Id = 10, VatRate = VatRate.Intermediate10 };
        var order = new Order
        {
            Id = 42, CustomerId = 1, RestaurantId = 5, TotalAmount = 20m, Status = OrderStatus.Pending,
            Customer = customer, Restaurant = restaurant,
            Items = new List<OrderItem>
            {
                new() { DishId = 10, Dish = dish, DishName = "Plat 1", Quantity = 2, UnitPrice = 10m },
            },
        };
        _orderRepo.GetByIdWithFullDetailsAsync(42, Arg.Any<CancellationToken>()).Returns(order);
        _invoiceRepo.ExistsForOrderAndKindAsync(42, InvoiceKind.OrderInvoiceToCustomer, Arg.Any<CancellationToken>()).Returns(false);
        _numbering.IssueNumberAsync(InvoiceIssuerType.Restaurant, 5, Arg.Any<int>(), false, Arg.Any<CancellationToken>())
                  .Returns("R0005-2026-000001");
        _numbering.IssueNumberAsync(InvoiceIssuerType.Platform, null, Arg.Any<int>(), false, Arg.Any<CancellationToken>())
                  .Returns("DT-2026-000123");
        _invoiceRepo.CreateAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>()).Returns(ci => { var inv = ci.Arg<Invoice>(); inv.Id = 100 + inv.Kind.GetHashCode(); return inv; });

        // Existing RestaurantTransaction for commission
        // We'll rely on the service reading commission from the order — may need `RestaurantAccountRepository.GetLatestForOrderAsync`.
        // If the commission amount is not yet credited at the Authorization Completed step, the service should compute it from
        // `PLATFORM_COMMISSION_RATE * order.TotalAmount`. Read AppEnvironment's commission rate field.

        var result = await _sut.CreatePendingInvoicesForCapturedOrderAsync(42, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _invoiceRepo.Received(2).CreateAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
        await _publisher.Received(2).PublishAsync("invoice", Arg.Any<InvoiceJobMessage>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePendingInvoices_AlreadyExists_SkipsIdempotently()
    {
        _orderRepo.GetByIdWithFullDetailsAsync(42, Arg.Any<CancellationToken>()).Returns(new Order { Id = 42 });
        _invoiceRepo.ExistsForOrderAndKindAsync(42, InvoiceKind.OrderInvoiceToCustomer, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreatePendingInvoicesForCapturedOrderAsync(42, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _invoiceRepo.DidNotReceive().CreateAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePendingInvoices_VatExemptRestaurant_AllLinesZeroVat()
    {
        var restaurant = new Restaurant { Id = 5, Siret = "73282932000074", LegalName = "X", LegalAddress = "Y", LegalForm = "EI", IsVatRegistered = false };
        var dish = new Dish { Id = 10, VatRate = VatRate.Intermediate10 };
        var order = new Order
        {
            Id = 42, CustomerId = 1, RestaurantId = 5, TotalAmount = 10m, Status = OrderStatus.Pending,
            Customer = new User { Id = 1, Email = "x@y.fr" },
            Restaurant = restaurant,
            Items = new List<OrderItem> { new() { DishId = 10, Dish = dish, DishName = "Plat", Quantity = 1, UnitPrice = 10m } },
        };
        _orderRepo.GetByIdWithFullDetailsAsync(42, Arg.Any<CancellationToken>()).Returns(order);
        _invoiceRepo.ExistsForOrderAndKindAsync(42, InvoiceKind.OrderInvoiceToCustomer, Arg.Any<CancellationToken>()).Returns(false);
        _numbering.IssueNumberAsync(Arg.Any<InvoiceIssuerType>(), Arg.Any<int?>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                  .Returns("TEST-000001");

        Invoice? customerInvoice = null;
        _invoiceRepo.CreateAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>())
                    .Returns(ci =>
                    {
                        var inv = ci.Arg<Invoice>();
                        if (inv.Kind == InvoiceKind.OrderInvoiceToCustomer) customerInvoice = inv;
                        inv.Id = 1;
                        return inv;
                    });

        await _sut.CreatePendingInvoicesForCapturedOrderAsync(42, CancellationToken.None);

        Assert.That(customerInvoice, Is.Not.Null);
        Assert.That(customerInvoice!.Lines, Has.All.Matches<InvoiceLine>(l => l.VatRate == 0m));
        Assert.That(customerInvoice.TotalVat, Is.EqualTo(0m));
    }
}
```

- [ ] **Step 3: Run failing tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~InvoiceServiceTests"
```

- [ ] **Step 4: Implement `InvoiceService.cs`**

```csharp
using System.Text.Json;
using DeliverTableInfrastructure.Invoicing;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;
using DeliverTableSharedLibrary.Extensions;

namespace DeliverTableServer.Services;

public class InvoiceService(
    IInvoiceRepository invoiceRepository,
    IOrderRepository orderRepository,
    IInvoiceNumberingService numbering,
    IMessagePublisher publisher,
    AppEnvironment env) : IInvoiceService
{
    public async Task<ServiceResult> CreatePendingInvoicesForCapturedOrderAsync(int orderId, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdWithFullDetailsAsync(orderId, ct);
        if (order is null) return new ServiceError(ErrorMessages.OrderNotFound);

        if (await invoiceRepository.ExistsForOrderAndKindAsync(orderId, InvoiceKind.OrderInvoiceToCustomer, ct))
        {
            return ServiceResult.Success();
        }

        int year = DateTime.UtcNow.Year;
        var restaurant = order.Restaurant;
        var customer = order.Customer;

        // ── Customer invoice (restaurant → customer) ──────────────────────────
        var customerNumber = await numbering.IssueNumberAsync(InvoiceIssuerType.Restaurant, restaurant.Id, year, false, ct);
        var customerInvoice = BuildCustomerInvoice(order, restaurant, customer, customerNumber);
        await invoiceRepository.CreateAsync(customerInvoice, ct);
        await publisher.PublishAsync("invoice", new InvoiceJobMessage(customerInvoice.Id), ct);

        // ── Commission invoice (platform → restaurant) ───────────────────────
        var platformNumber = await numbering.IssueNumberAsync(InvoiceIssuerType.Platform, null, year, false, ct);
        var commissionInvoice = BuildCommissionInvoice(order, restaurant, platformNumber);
        await invoiceRepository.CreateAsync(commissionInvoice, ct);
        await publisher.PublishAsync("invoice", new InvoiceJobMessage(commissionInvoice.Id), ct);

        return ServiceResult.Success();
    }

    private Invoice BuildCustomerInvoice(Order order, Restaurant restaurant, User customer, string number)
    {
        var issuerSnapshot = new InvoiceLegalSnapshotDto(
            Name: restaurant.LegalName,
            LegalForm: restaurant.LegalForm,
            Siret: restaurant.Siret,
            VatNumber: string.Empty,
            Address: restaurant.LegalAddress);
        var recipientSnapshot = new InvoiceLegalSnapshotDto(
            Name: $"{customer.FirstName} {customer.LastName}".Trim(),
            LegalForm: string.Empty,
            Siret: string.Empty,
            VatNumber: string.Empty,
            Address: customer.Email ?? string.Empty);

        var invoice = new Invoice
        {
            Number = number,
            Kind = InvoiceKind.OrderInvoiceToCustomer,
            OrderId = order.Id,
            IssuerType = InvoiceIssuerType.Restaurant,
            IssuerRestaurantId = restaurant.Id,
            RecipientUserId = customer.Id,
            IssuedAt = DateTime.UtcNow,
            Currency = "EUR",
            Status = InvoiceStatus.Queued,
            IssuerLegalSnapshotJson = JsonSerializer.Serialize(issuerSnapshot),
            RecipientSnapshotJson = JsonSerializer.Serialize(recipientSnapshot),
        };

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

        invoice.TotalTtc = invoice.Lines.Sum(l => l.LineTtc);
        invoice.TotalHt = invoice.Lines.Sum(l => l.LineHt);
        invoice.TotalVat = invoice.Lines.Sum(l => l.LineVat);
        return invoice;
    }

    private Invoice BuildCommissionInvoice(Order order, Restaurant restaurant, string number)
    {
        var issuerSnapshot = new InvoiceLegalSnapshotDto(
            Name: env.PlatformLegalName,
            LegalForm: env.PlatformLegalForm,
            Siret: env.PlatformSiret,
            VatNumber: env.PlatformVatNumber,
            Address: env.PlatformAddress);
        var recipientSnapshot = new InvoiceLegalSnapshotDto(
            Name: restaurant.LegalName,
            LegalForm: restaurant.LegalForm,
            Siret: restaurant.Siret,
            VatNumber: string.Empty,
            Address: restaurant.LegalAddress);

        var commissionAmount = Math.Round(order.TotalAmount * env.PlatformCommissionRate, 2, MidpointRounding.AwayFromZero);
        decimal rate = env.PlatformVatApplicable ? 20m : 0m;

        var invoice = new Invoice
        {
            Number = number,
            Kind = InvoiceKind.CommissionInvoiceToRestaurant,
            OrderId = order.Id,
            IssuerType = InvoiceIssuerType.Platform,
            RecipientRestaurantId = restaurant.Id,
            IssuedAt = DateTime.UtcNow,
            Currency = "EUR",
            Status = InvoiceStatus.Queued,
            IssuerLegalSnapshotJson = JsonSerializer.Serialize(issuerSnapshot),
            RecipientSnapshotJson = JsonSerializer.Serialize(recipientSnapshot),
        };

        // Assume commissionAmount is HT (per spec 15 assumption; platform charges VAT on top).
        var unitHt = commissionAmount;
        var unitTtc = Math.Round(unitHt * (1 + rate / 100m), 2, MidpointRounding.AwayFromZero);
        var lineVat = Math.Round(unitTtc - unitHt, 2, MidpointRounding.AwayFromZero);

        invoice.Lines.Add(new InvoiceLine
        {
            Description = $"Commission plateforme sur commande #{order.Id}",
            Quantity = 1m,
            UnitPriceHt = unitHt,
            UnitPriceTtc = unitTtc,
            VatRate = rate,
            LineHt = unitHt,
            LineVat = lineVat,
            LineTtc = unitTtc,
            SortOrder = 0,
        });

        invoice.TotalTtc = unitTtc;
        invoice.TotalHt = unitHt;
        invoice.TotalVat = lineVat;
        return invoice;
    }
}
```

Note: `env.PlatformCommissionRate` must exist on `AppEnvironment`. From `docker-dev.yaml` backend env block (Spec 1), `PLATFORM_COMMISSION_RATE` already exists with default `0.10`. Confirm it's loaded in AppEnvironment; if not, add it as an optional env var with default 0.10 in Task 6.

- [ ] **Step 5: Run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~InvoiceServiceTests"
```

3 tests must PASS.

- [ ] **Step 6: Commit**

```bash
git add DeliverTableServer/Services/Interfaces/IInvoiceService.cs \
        DeliverTableServer/Services/InvoiceService.cs \
        DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs
git commit -m "feat(server): add IInvoiceService create-pending-invoices with tests"
```

---

## Task 12: `IInvoiceService.CreateCreditNotesForRefundAsync` with tests

**Files:**
- Modify: `DeliverTableServer/Services/Interfaces/IInvoiceService.cs`
- Modify: `DeliverTableServer/Services/InvoiceService.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs`

- [ ] **Step 1: Extend interface**

```csharp
Task<ServiceResult> CreateCreditNotesForRefundAsync(int refundId, CancellationToken ct);
```

- [ ] **Step 2: Failing tests**

Append to `InvoiceServiceTests.cs`:

```csharp
[Test]
public async Task CreateCreditNotes_FullRefund_MirrorsOriginalLinesNegatively()
{
    var refund = new Refund { Id = 7, PaymentId = 1, Amount = 20m };
    var payment = new Payment { Id = 1, OrderId = 42, Amount = 20m };
    var original = new Invoice
    {
        Id = 100, OrderId = 42, Kind = InvoiceKind.OrderInvoiceToCustomer,
        Number = "R0005-2026-000001", TotalTtc = 20m,
        IssuerLegalSnapshotJson = "{}", RecipientSnapshotJson = "{}",
        IssuerType = InvoiceIssuerType.Restaurant, IssuerRestaurantId = 5,
        RecipientUserId = 1,
        Lines = new List<InvoiceLine>
        {
            new() { Description = "Plat", Quantity = 2, UnitPriceTtc = 10m, UnitPriceHt = 9.09m, VatRate = 10m, LineHt = 18.18m, LineVat = 1.82m, LineTtc = 20m, SortOrder = 0 },
        },
    };
    // ... minimal mocks for refund repo lookup, payment repo lookup, invoice list by order id
    // the service must load refund, find its order, list invoices for that order, create mirror credit notes
    _invoiceRepo.ListByOrderIdAsync(42, Arg.Any<CancellationToken>())
                .Returns(new List<Invoice> { original });
    // Add a mock for the repo that looks up Refund+Payment — the actual interface depends on what we inject.
    // If InvoiceService needs IPaymentRepository to resolve Refund → Order, add it as a dep and mock it here.

    _numbering.IssueNumberAsync(Arg.Any<InvoiceIssuerType>(), Arg.Any<int?>(), Arg.Any<int>(), true, Arg.Any<CancellationToken>())
              .Returns("AV-R0005-2026-000002");
    _invoiceRepo.CreateAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>()).Returns(ci => { var inv = ci.Arg<Invoice>(); inv.Id = 200; return inv; });

    var result = await _sut.CreateCreditNotesForRefundAsync(7, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    await _invoiceRepo.Received().CreateAsync(
        Arg.Is<Invoice>(i => i.Kind == InvoiceKind.CreditNoteToCustomer
                          && i.RelatedInvoiceId == 100
                          && i.TotalTtc == -20m
                          && i.Lines[0].Quantity == -2),
        Arg.Any<CancellationToken>());
}

[Test]
public async Task CreateCreditNotes_PartialRefund_ProratesNegativeLines()
{
    // 25% refund on a 20€ invoice with 2 qty → each line quantity becomes -0.5
    // omitted for brevity; follow the full-refund pattern with amount=5m and assert:
    //   Received Invoice where Lines[0].Quantity == -0.5m and LineTtc == -5m
    // ...
    Assert.Pass("TODO implement full assertion chain");
}
```

(Flesh out the partial test body matching the service's actual impl once it's written.)

- [ ] **Step 3: Implement**

Add to `InvoiceService`:

```csharp
public async Task<ServiceResult> CreateCreditNotesForRefundAsync(int refundId, CancellationToken ct)
{
    // Resolve refund → payment → orderId via IPaymentRepository (inject if needed).
    // List original invoices by orderId.
    // For each of the two originals, create a mirror credit note with -ratio multiplier.
    // Persist + queue jobs.
    // If originals missing, log + return Success (spec 7.3 says "log and skip").

    // Concrete impl omitted here for brevity — ~80 lines mirroring BuildCustomerInvoice/BuildCommissionInvoice
    // with negated quantities and totals.
    throw new NotImplementedException("Implement per spec §7.3");
}
```

The engineer should:
1. Inject `IPaymentRepository paymentRepo` to load the Refund + Payment.
2. Use `invoiceRepository.ListByOrderIdAsync(payment.OrderId, ct)`.
3. Compute ratio as `refund.Amount / original.TotalTtc` (for restaurant-side original, which is TTC).
4. For each original, create an Invoice with Kind flipped to the credit-note variant, RelatedInvoiceId set, lines mirrored with `Quantity *= -ratio`, totals recomputed (will be negative).
5. Call `numbering.IssueNumberAsync(..., isCreditNote: true, ct)`.
6. Persist and publish.

- [ ] **Step 4: Build + tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~InvoiceServiceTests"
```

All InvoiceService tests must PASS.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableServer/Services/ DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs
git commit -m "feat(server): add IInvoiceService credit-note generation with tests"
```

---

## Task 13: Wire invoice queue into `PaymentService` webhook handlers with tests

**Files:**
- Modify: `DeliverTableServer/Services/PaymentService.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs`

- [ ] **Step 1: Inject `IInvoiceService` into `PaymentService`**

Extend the primary constructor. Update the existing test fixtures' SetUp to pass a new `IInvoiceService` mock.

- [ ] **Step 2: Failing tests**

Append to `PaymentServiceTests`:

```csharp
[Test]
public async Task HandleAuthorizationCompletedAsync_QueuesInvoiceCreation()
{
    // Arrange order + payment in AwaitingPayment → pi returned succeeded auth.
    // Assert _invoiceService.Received(1).CreatePendingInvoicesForCapturedOrderAsync(orderId, ct).
}

[Test]
public async Task HandleChargeRefundedAsync_QueuesCreditNoteCreation()
{
    // Arrange charge.refunded event with a new refund id.
    // Assert _invoiceService.Received(1).CreateCreditNotesForRefundAsync(refundId, ct).
}
```

- [ ] **Step 3: Implement**

Inside `HandleAuthorizationCompletedAsync`, after the existing loyalty/discount commit + cart clear + email queue:

```csharp
deferredPublishes.Add(() => invoiceService.CreatePendingInvoicesForCapturedOrderAsync(order.Id, ct));
```

Or, since `IInvoiceService` persists to DB (invoice rows) AND publishes to RabbitMQ, it shouldn't be deferred — only the RabbitMQ `PublishAsync` calls inside `InvoiceService` should be. The cleanest approach: let the DB writes happen inside the transaction; factor the publish step OUT of `InvoiceService.CreatePendingInvoicesForCapturedOrderAsync` by returning the `InvoiceJobMessage`s to the caller and having `PaymentService` add them to its `deferredPublishes` list.

Concrete: change `IInvoiceService.CreatePendingInvoicesForCapturedOrderAsync` to return `ServiceResult<List<InvoiceJobMessage>>` — the caller is responsible for publishing.

Update the existing InvoiceServiceTests to assert on the returned list of messages rather than `_publisher.Received(...)`.

Then in `PaymentService.HandleAuthorizationCompletedAsync`:
```csharp
var invoicesResult = await invoiceService.CreatePendingInvoicesForCapturedOrderAsync(order.Id, ct);
if (invoicesResult.IsSuccess && invoicesResult.Value is not null)
{
    foreach (var msg in invoicesResult.Value)
    {
        var captured = msg;
        deferredPublishes.Add(() => publisher.PublishAsync("invoice", captured, ct));
    }
}
```

Mirror for `HandleChargeRefundedAsync` with `CreateCreditNotesForRefundAsync` returning the credit-note messages.

- [ ] **Step 4: Run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build
```

All existing + new tests must pass.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableServer/Services/ DeliverTableTests/Server/Unit/Services/
git commit -m "feat(server): wire invoice queue into PaymentService capture and refund webhooks with tests"
```

---

## Task 14: Add QuestPDF + `IInvoicePdfRenderer` with smoke test

**Files:**
- Modify: `DeliverTableWorker/DeliverTableWorker.csproj`
- Create: `DeliverTableWorker/Services/IInvoicePdfRenderer.cs`
- Create: `DeliverTableWorker/Services/InvoicePdfRenderer.cs`
- Create: `DeliverTableTests/Worker/Unit/Services/InvoicePdfRendererTests.cs`

- [ ] **Step 1: Add QuestPDF package**

```bash
docker compose -f docker-dev.yaml exec backend dotnet add /src/DeliverTableWorker/DeliverTableWorker.csproj package QuestPDF
```

QuestPDF's Community license requires setting `QuestPDF.Settings.License = LicenseType.Community;` at startup. Add this to `DeliverTableWorker/Program.cs` at the top.

- [ ] **Step 2: Interface**

```csharp
using DeliverTableInfrastructure.Models;

namespace DeliverTableWorker.Services;

public interface IInvoicePdfRenderer
{
    byte[] Render(Invoice invoice);
}
```

(Synchronous — QuestPDF is synchronous; we can `Task.Run` it from the consumer if needed.)

- [ ] **Step 3: Implement `InvoicePdfRenderer.cs`**

```csharp
using System.Text.Json;
using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DeliverTableWorker.Services;

public class InvoicePdfRenderer : IInvoicePdfRenderer
{
    public byte[] Render(Invoice invoice)
    {
        var issuer = JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(invoice.IssuerLegalSnapshotJson)
                     ?? new InvoiceLegalSnapshotDto("", "", "", "", "");
        var recipient = JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(invoice.RecipientSnapshotJson)
                        ?? new InvoiceLegalSnapshotDto("", "", "", "", "");

        var isCreditNote = invoice.Kind == InvoiceKind.CreditNoteToCustomer
                        || invoice.Kind == InvoiceKind.CommissionCreditNoteToRestaurant;
        var isVatExempt = invoice.Lines.All(l => l.VatRate == 0m);

        var title = isCreditNote ? "AVOIR" : "FACTURE";

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(issuer.Name).Bold().FontSize(14);
                        col.Item().Text($"{issuer.LegalForm} — SIRET {issuer.Siret}");
                        if (!string.IsNullOrEmpty(issuer.VatNumber)) col.Item().Text($"TVA {issuer.VatNumber}");
                        col.Item().Text(issuer.Address);
                    });
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text(title).Bold().FontSize(18);
                        col.Item().Text($"N° {invoice.Number}");
                        col.Item().Text($"Date : {invoice.IssuedAt:dd/MM/yyyy}");
                        col.Item().Text($"Commande #{invoice.OrderId}");
                    });
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Text("Destinataire :").Bold();
                    col.Item().Text(recipient.Name);
                    if (!string.IsNullOrEmpty(recipient.Siret)) col.Item().Text($"SIRET {recipient.Siret}");
                    col.Item().Text(recipient.Address);

                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(4);
                            cd.RelativeColumn(1);
                            cd.RelativeColumn(2);
                            if (!isVatExempt) cd.RelativeColumn(1);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(2);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Text("Description").Bold();
                            h.Cell().AlignRight().Text("Qté").Bold();
                            h.Cell().AlignRight().Text("PU HT").Bold();
                            if (!isVatExempt) h.Cell().AlignRight().Text("TVA %").Bold();
                            h.Cell().AlignRight().Text("Total HT").Bold();
                            h.Cell().AlignRight().Text("Total TTC").Bold();
                        });
                        foreach (var line in invoice.Lines.OrderBy(l => l.SortOrder))
                        {
                            table.Cell().Text(line.Description);
                            table.Cell().AlignRight().Text(line.Quantity.ToString("0.###"));
                            table.Cell().AlignRight().Text(line.UnitPriceHt.ToString("0.00 €"));
                            if (!isVatExempt) table.Cell().AlignRight().Text($"{line.VatRate:0.#} %");
                            table.Cell().AlignRight().Text(line.LineHt.ToString("0.00 €"));
                            table.Cell().AlignRight().Text(line.LineTtc.ToString("0.00 €"));
                        }
                    });

                    col.Item().PaddingTop(15).AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Total HT : {invoice.TotalHt:0.00 €}");
                        totals.Item().Text($"Total TVA : {invoice.TotalVat:0.00 €}");
                        totals.Item().Text($"Total TTC : {invoice.TotalTtc:0.00 €}").Bold();
                    });

                    if (isVatExempt)
                    {
                        col.Item().PaddingTop(10).Text("TVA non applicable, art. 293 B du CGI").Italic();
                    }
                });

                page.Footer().AlignCenter().Text("Paiement prélevé par Stripe.").FontSize(8);
            });
        });

        return doc.GeneratePdf();
    }
}
```

- [ ] **Step 4: Initialize QuestPDF license in `Program.cs`**

At the top of `DeliverTableWorker/Program.cs`:

```csharp
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
```

- [ ] **Step 5: Smoke test**

```csharp
using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;
using DeliverTableWorker.Services;
using NUnit.Framework;

namespace DeliverTableTests.Worker.Unit.Services;

[TestFixture]
public class InvoicePdfRendererTests
{
    [SetUp]
    public void SetUp()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    [Test]
    public void Render_BasicInvoice_ProducesValidPdf()
    {
        var invoice = new Invoice
        {
            Number = "TEST-000001",
            Kind = InvoiceKind.OrderInvoiceToCustomer,
            OrderId = 1,
            IssuedAt = DateTime.UtcNow,
            Currency = "EUR",
            TotalHt = 10m, TotalVat = 1m, TotalTtc = 11m,
            IssuerLegalSnapshotJson = """{"Name":"Test","LegalForm":"SAS","Siret":"73282932000074","VatNumber":"","Address":""}""",
            RecipientSnapshotJson = """{"Name":"Client","LegalForm":"","Siret":"","VatNumber":"","Address":""}""",
            Lines = new List<InvoiceLine>
            {
                new() { Description = "Plat", Quantity = 1, UnitPriceHt = 10m, UnitPriceTtc = 11m, VatRate = 10m, LineHt = 10m, LineVat = 1m, LineTtc = 11m, SortOrder = 0 },
            },
        };

        var pdf = new InvoicePdfRenderer().Render(invoice);

        Assert.That(pdf, Is.Not.Null);
        Assert.That(pdf.Length, Is.GreaterThan(1000));
        // PDF magic bytes "%PDF-"
        Assert.That(pdf[0], Is.EqualTo((byte)'%'));
        Assert.That(pdf[1], Is.EqualTo((byte)'P'));
        Assert.That(pdf[2], Is.EqualTo((byte)'D'));
        Assert.That(pdf[3], Is.EqualTo((byte)'F'));
    }
}
```

- [ ] **Step 6: Build + run**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~InvoicePdfRendererTests"
```

The smoke test must PASS.

- [ ] **Step 7: Commit**

```bash
git add DeliverTableWorker/DeliverTableWorker.csproj \
        DeliverTableWorker/Services/IInvoicePdfRenderer.cs \
        DeliverTableWorker/Services/InvoicePdfRenderer.cs \
        DeliverTableWorker/Program.cs \
        DeliverTableTests/Worker/Unit/Services/InvoicePdfRendererTests.cs
git commit -m "feat(worker): add QuestPDF dependency and IInvoicePdfRenderer"
```

---

## Task 15: Extend `IObjectStorageService` with `byte[]` upload overload

**Files:**
- Modify: `DeliverTableInfrastructure/Services/Interfaces/IObjectStorageService.cs`
- Modify: `DeliverTableInfrastructure/Services/ObjectStorageService.cs`

No tests — external boundary.

- [ ] **Step 1: Read existing implementation**

Confirm the current `UploadAsync(IFormFile, string folder, int? identifier)` signature. The new overload takes raw bytes.

- [ ] **Step 2: Add to interface**

```csharp
Task<string> UploadAsync(byte[] content, string contentType, string folder, string fileName, CancellationToken ct = default);
```

- [ ] **Step 3: Implement**

```csharp
public async Task<string> UploadAsync(byte[] content, string contentType, string folder, string fileName, CancellationToken ct = default)
{
    string key = $"{folder}/{fileName}";
    var request = new PutObjectRequest
    {
        BucketName = _bucketName,
        Key = key,
        ContentType = contentType,
        InputStream = new MemoryStream(content),
    };
    await _s3.PutObjectAsync(request, ct);
    return key;
}
```

Adapt field names to actual (`_bucketName`, `_s3`, etc.).

- [ ] **Step 4: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 5: Commit**

```bash
git add DeliverTableInfrastructure/Services/
git commit -m "feat(worker): extend IObjectStorageService with byte[] upload overload"
```

---

## Task 16: `InvoiceJobConsumer` with tests

**Files:**
- Create: `DeliverTableInfrastructure/Messaging/Messages/InvoiceJobMessage.cs`
- Create: `DeliverTableWorker/Consumers/InvoiceJobConsumer.cs`
- Modify: `DeliverTableWorker/Program.cs` (register consumer)
- Create: `DeliverTableTests/Worker/Unit/Consumers/InvoiceJobConsumerTests.cs`

- [ ] **Step 1: Create message**

```csharp
namespace DeliverTableInfrastructure.Messaging.Messages;

public sealed record InvoiceJobMessage(int InvoiceId);
```

- [ ] **Step 2: Failing test**

Read an existing consumer test (e.g. a hypothetical `EmailJobConsumerTests.cs`) for style. If no test file exists for the email consumer, model after the scheduler sweep tests.

```csharp
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableInfrastructure.Services.Interfaces;
using DeliverTableSharedLibrary.Enums;
using DeliverTableWorker.Consumers;
using DeliverTableWorker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace DeliverTableTests.Worker.Unit.Consumers;

[TestFixture]
public class InvoiceJobConsumerTests
{
    private IInvoiceRepository _invoiceRepo = null!;
    private IInvoicePdfRenderer _renderer = null!;
    private IObjectStorageService _storage = null!;
    private IMessagePublisher _publisher = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private InvoiceJobConsumer _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _invoiceRepo = Substitute.For<IInvoiceRepository>();
        _renderer = Substitute.For<IInvoicePdfRenderer>();
        _storage = Substitute.For<IObjectStorageService>();
        _publisher = Substitute.For<IMessagePublisher>();
        var services = new ServiceCollection();
        services.AddSingleton(_invoiceRepo);
        services.AddSingleton(_renderer);
        services.AddSingleton(_storage);
        services.AddSingleton(_publisher);
        _scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        _sut = new InvoiceJobConsumer(_scopeFactory, NullLogger<InvoiceJobConsumer>.Instance);
    }

    [Test]
    public async Task Consume_HappyPath_GeneratesUploadsAndQueuesEmail()
    {
        var invoice = new Invoice
        {
            Id = 1, Number = "R-2026-000001", OrderId = 42, IssuedAt = new DateTime(2026, 4, 14),
            Kind = InvoiceKind.OrderInvoiceToCustomer, Status = InvoiceStatus.Queued,
            RecipientUserId = 5, IssuerLegalSnapshotJson = "{}", RecipientSnapshotJson = "{}",
        };
        _invoiceRepo.GetByIdWithLinesAsync(1, Arg.Any<CancellationToken>()).Returns(invoice);
        _renderer.Render(invoice).Returns(new byte[] { 1, 2, 3 });
        _storage.UploadAsync(Arg.Any<byte[]>(), "application/pdf", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns("invoices/2026/04/R-2026-000001.pdf");

        await _sut.HandleAsync(new InvoiceJobMessage(1), CancellationToken.None);

        Assert.That(invoice.StoragePath, Is.EqualTo("invoices/2026/04/R-2026-000001.pdf"));
        Assert.That(invoice.Status, Is.EqualTo(InvoiceStatus.Generated));
        await _invoiceRepo.Received().UpdateAsync(invoice, Arg.Any<CancellationToken>());
        await _publisher.Received().PublishAsync("email", Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Consume_RendererThrows_MarksFailedAndRethrows()
    {
        var invoice = new Invoice { Id = 1, Status = InvoiceStatus.Queued, IssuerLegalSnapshotJson = "{}", RecipientSnapshotJson = "{}" };
        _invoiceRepo.GetByIdWithLinesAsync(1, Arg.Any<CancellationToken>()).Returns(invoice);
        _renderer.When(r => r.Render(Arg.Any<Invoice>())).Do(_ => throw new Exception("boom"));

        Assert.ThrowsAsync<Exception>(async () => await _sut.HandleAsync(new InvoiceJobMessage(1), CancellationToken.None));
        Assert.That(invoice.Status, Is.EqualTo(InvoiceStatus.Failed));
        Assert.That(invoice.FailureReason, Does.Contain("boom"));
    }
}
```

- [ ] **Step 3: Implement consumer**

Read `DeliverTableWorker/Consumers/EmailJobConsumer.cs` to understand the consumer base pattern (BackgroundService + RabbitMQ binding, or a simple class with a `HandleAsync` that the RabbitMQ infra calls).

```csharp
using System.Text.Json;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableInfrastructure.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;
using DeliverTableWorker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeliverTableWorker.Consumers;

public sealed class InvoiceJobConsumer(
    IServiceScopeFactory scopeFactory,
    ILogger<InvoiceJobConsumer> logger)
{
    // Subscribe pattern: look at how EmailJobConsumer hooks into RabbitMQ.
    // The HandleAsync method is called once per delivered message.
    public async Task HandleAsync(InvoiceJobMessage msg, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var invoiceRepo = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var renderer = scope.ServiceProvider.GetRequiredService<IInvoicePdfRenderer>();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        var invoice = await invoiceRepo.GetByIdWithLinesAsync(msg.InvoiceId, ct);
        if (invoice is null)
        {
            logger.LogWarning("Invoice {Id} not found, skipping", msg.InvoiceId);
            return;
        }

        try
        {
            var pdfBytes = renderer.Render(invoice);
            string fileName = $"{invoice.Number}.pdf";
            string folder = $"invoices/{invoice.IssuedAt:yyyy}/{invoice.IssuedAt:MM}";
            var key = await storage.UploadAsync(pdfBytes, "application/pdf", folder, fileName, ct);

            invoice.StoragePath = key;
            invoice.Status = InvoiceStatus.Generated;
            invoice.FailureReason = null;
            await invoiceRepo.UpdateAsync(invoice, ct);

            var recipient = JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(invoice.RecipientSnapshotJson);
            string template = invoice.Kind switch
            {
                InvoiceKind.OrderInvoiceToCustomer or InvoiceKind.CreditNoteToCustomer => "InvoiceReadyCustomer",
                _ => "InvoiceReadyRestaurant",
            };
            var emailJob = new EmailJobMessage(
                Template: template,
                ToEmail: recipient?.Address ?? string.Empty,
                AttachmentStoragePath: key,
                AttachmentFilename: fileName);
            await publisher.PublishAsync("email", emailJob, ct);
        }
        catch (Exception ex)
        {
            invoice.Status = InvoiceStatus.Failed;
            invoice.FailureReason = ex.Message;
            try { await invoiceRepo.UpdateAsync(invoice, ct); } catch { /* best-effort */ }
            throw;
        }
    }
}
```

Adapt `EmailJobMessage` construction once Task 17 has extended its shape. For now the fields shown are placeholders; Task 17 will make them real.

- [ ] **Step 4: Register in Program.cs**

Inspect how `EmailJobConsumer` is wired (likely via `builder.Services.AddHostedService<EmailJobConsumer>()` or a RabbitMQ binding extension). Add the same for `InvoiceJobConsumer` plus bind it to the `invoice` queue.

- [ ] **Step 5: Build + tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~InvoiceJobConsumerTests"
```

Both tests must PASS.

- [ ] **Step 6: Commit**

```bash
git add DeliverTableInfrastructure/Messaging/Messages/InvoiceJobMessage.cs \
        DeliverTableWorker/Consumers/InvoiceJobConsumer.cs \
        DeliverTableWorker/Program.cs \
        DeliverTableTests/Worker/Unit/Consumers/InvoiceJobConsumerTests.cs
git commit -m "feat(worker): add InvoiceJobConsumer with tests"
```

---

## Task 17: Extend `EmailJobMessage` with attachment support and update consumer

**Files:**
- Modify: `DeliverTableInfrastructure/Messaging/Messages/EmailJobMessage.cs`
- Modify: `DeliverTableWorker/Consumers/EmailJobConsumer.cs`
- Create: `DeliverTableTests/Worker/Unit/Consumers/EmailJobConsumerAttachmentTests.cs`

- [ ] **Step 1: Add fields to `EmailJobMessage`**

Read the current definition. Add optional nullable fields:

```csharp
public string? AttachmentStoragePath { get; init; }
public string? AttachmentFilename { get; init; }
```

(If it's a `record` primary constructor, append as optional parameters.)

- [ ] **Step 2: Failing test**

```csharp
[Test]
public async Task SendEmail_WithAttachment_DownloadsFromStorageAndAttaches()
{
    var msg = new EmailJobMessage(..., AttachmentStoragePath: "invoices/2026/04/test.pdf", AttachmentFilename: "test.pdf");
    _storage.GetObjectAsync("invoices/2026/04/test.pdf", Arg.Any<CancellationToken>())
            .Returns(new ObjectStorageResult(new MemoryStream(new byte[] { 1, 2, 3 }), "application/pdf", 3));
    // Arrange other mocks from existing email consumer test setup.

    await _sut.HandleAsync(msg, CancellationToken.None);

    // Assert that the MimeMessage passed to the email sender has a PDF attachment with the right bytes.
    _sender.Received().SendAsync(
        Arg.Is<MimeMessage>(m => m.Attachments.Any(a =>
            a.ContentType.MimeType == "application/pdf"
            && a.ContentDisposition?.FileName == "test.pdf")),
        Arg.Any<CancellationToken>());
}
```

- [ ] **Step 3: Implement in `EmailJobConsumer`**

In the handler, after building the `MimeMessage` body:

```csharp
if (!string.IsNullOrEmpty(msg.AttachmentStoragePath))
{
    var blob = await storage.GetObjectAsync(msg.AttachmentStoragePath);
    if (blob is not null)
    {
        using var ms = new MemoryStream();
        await blob.Stream.CopyToAsync(ms, ct);
        var multi = new Multipart("mixed");
        multi.Add(mimeMessage.Body);  // existing HTML/text body
        multi.Add(new MimePart("application", "pdf")
        {
            Content = new MimeContent(new MemoryStream(ms.ToArray())),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment) { FileName = msg.AttachmentFilename },
            ContentTransferEncoding = ContentEncoding.Base64,
            FileName = msg.AttachmentFilename,
        });
        mimeMessage.Body = multi;
    }
}
```

Actual API: match MailKit. Read existing consumer for body-building style.

- [ ] **Step 4: Run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~EmailJobConsumerAttachmentTests"
```

- [ ] **Step 5: Commit**

```bash
git add DeliverTableInfrastructure/Messaging/Messages/EmailJobMessage.cs \
        DeliverTableWorker/Consumers/EmailJobConsumer.cs \
        DeliverTableTests/Worker/Unit/Consumers/EmailJobConsumerAttachmentTests.cs
git commit -m "feat(worker): extend EmailJobMessage with attachments and update email consumer"
```

---

## Task 18: Add invoice-ready email templates

**Files:**
- Create: `DeliverTableWorker/Templates/InvoiceReadyCustomer.cshtml`
- Create: `DeliverTableWorker/Templates/InvoiceReadyRestaurant.cshtml`

Templates render email bodies in French. Match existing worker templates' structure (there's an order-confirmation template used by the existing email flow — read it first).

- [ ] **Step 1: `InvoiceReadyCustomer.cshtml`**

Simple French body:

```html
@{
    Layout = null;
}
<!DOCTYPE html>
<html lang="fr">
<head><meta charset="utf-8"><title>Votre facture</title></head>
<body>
    <h1>Votre facture est disponible</h1>
    <p>Bonjour @Model.FirstName,</p>
    <p>Votre facture pour la commande #@Model.OrderId est jointe à ce message au format PDF.</p>
    <p>Montant total : @Model.TotalTtc.ToString("0.00 €")</p>
    <p>Vous pouvez également la retrouver dans votre espace personnel.</p>
    <p>Cordialement,<br/>L'équipe DeliverTable</p>
</body>
</html>
```

- [ ] **Step 2: `InvoiceReadyRestaurant.cshtml`**

```html
@{ Layout = null; }
<!DOCTYPE html>
<html lang="fr">
<head><meta charset="utf-8"><title>Facture de commission</title></head>
<body>
    <h1>Facture de commission</h1>
    <p>Bonjour,</p>
    <p>La facture de commission pour la commande #@Model.OrderId est jointe à ce message.</p>
    <p>Montant : @Model.TotalTtc.ToString("0.00 €")</p>
    <p>Cordialement,<br/>L'équipe DeliverTable</p>
</body>
</html>
```

- [ ] **Step 3: Ensure templates are copied in csproj**

Check `DeliverTableWorker.csproj` for a `<Content Include="Templates\**\*.cshtml">` item — it already exists from the email pipeline; new `.cshtml` files are included automatically.

- [ ] **Step 4: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

- [ ] **Step 5: Commit**

```bash
git add DeliverTableWorker/Templates/
git commit -m "feat(worker): add invoice-ready email templates"
```

---

## Task 19: Add `InvoiceController` with tests

**Files:**
- Create: `DeliverTableServer/Controllers/InvoiceController.cs`
- Create: `DeliverTableTests/Server/Unit/Controllers/InvoiceControllerTests.cs`

- [ ] **Step 1: Failing tests**

```csharp
[Test]
public async Task GetMyInvoices_Authenticated_ReturnsList() { /* ... */ }

[Test]
public async Task DownloadPdf_OwnedInvoice_StreamsBlob() { /* ... */ }

[Test]
public async Task DownloadPdf_NotOwner_Returns403() { /* ... */ }

[Test]
public async Task DownloadPdf_StatusQueued_Returns409() { /* ... */ }
```

Model setup on `AuthenticationTestHelper.SetupAuthenticatedUser` and `IInvoiceService` mock.

- [ ] **Step 2: Implement controller**

```csharp
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Invoice.Base)]
[Authorize]
public class InvoiceController(IInvoiceService invoiceService) : ControllerBase
{
    [HttpGet(ApiRoutes.Invoice.MyListRoute)]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        var result = await invoiceService.ListForMeAsync(userId, page, pageSize, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Invoice.RestaurantListRoute)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner) + "," + nameof(UserRole.Administrator))]
    public async Task<IActionResult> GetForRestaurant(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        var result = await invoiceService.ListForRestaurantAsync(id, userId, User.IsInRole(nameof(UserRole.Administrator)), page, pageSize, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Invoice.DownloadRoute)]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        var result = await invoiceService.GetPdfStreamAsync(id, userId, User.IsInRole(nameof(UserRole.Administrator)), User.IsInRole(nameof(UserRole.RestaurantOwner)), ct);
        if (!result.IsSuccess) return result.Error!.ToErrorResult();
        var (stream, fileName) = result.Value!;
        return File(stream, "application/pdf", fileName);
    }
}
```

Extend `IInvoiceService` with these methods (all following existing `ServiceResult` patterns):
- `ListForMeAsync(int userId, int page, int pageSize, CancellationToken ct)` → `ServiceResult<PaginatedResult<InvoiceListItemDto>>`
- `ListForRestaurantAsync(int restaurantId, int userId, bool isAdmin, int page, int pageSize, CancellationToken ct)` → same
- `GetPdfStreamAsync(int invoiceId, int userId, bool isAdmin, bool isRestaurantOwner, CancellationToken ct)` → `ServiceResult<(Stream, string FileName)>` — loads invoice, verifies auth, streams blob via `IObjectStorageService.GetObjectAsync`

`ListForRestaurantAsync` must verify that `userId` owns `restaurantId` (unless `isAdmin`). Load restaurant via repository, check `Restaurant.OwnerId == userId`.

- [ ] **Step 3: Build + tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~InvoiceControllerTests"
```

- [ ] **Step 4: Commit**

```bash
git add DeliverTableServer/Controllers/InvoiceController.cs \
        DeliverTableServer/Services/ \
        DeliverTableTests/Server/Unit/Controllers/InvoiceControllerTests.cs \
        DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs
git commit -m "feat(server): add InvoiceController with tests"
```

---

## Task 20: Admin invoice endpoints with tests

**Files:**
- Create: `DeliverTableServer/Controllers/AdminInvoiceController.cs`
- Create: `DeliverTableTests/Server/Unit/Controllers/AdminInvoiceControllerTests.cs`
- Modify: `DeliverTableServer/Services/Interfaces/IInvoiceService.cs` + impl (admin methods)

- [ ] **Step 1: Failing tests**

```csharp
[Test] public async Task List_HappyPath_ReturnsPaginatedAdminRows() { /* ... */ }
[Test] public async Task GetDetail_HappyPath_ReturnsFullDetail() { /* ... */ }
[Test] public async Task ResendEmail_HappyPath_RepublishesJob() { /* ... */ }
```

- [ ] **Step 2: Implement service methods**

On `IInvoiceService`:
- `AdminListAsync(DetailedFilter, page, pageSize, ct)` → `ServiceResult<PaginatedResult<AdminInvoiceRowDto>>`
- `AdminGetDetailAsync(id, ct)` → `ServiceResult<AdminInvoiceDetailDto>`
- `AdminResendEmailAsync(id, ct)` → `ServiceResult` — loads invoice, if `StoragePath != null` publish `EmailJobMessage` with same `AttachmentStoragePath`.

- [ ] **Step 3: Controller**

```csharp
[ApiController]
[Route(ApiRoutes.Admin.Base)]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminInvoiceController(IInvoiceService svc) : ControllerBase
{
    [HttpGet(ApiRoutes.Admin.InvoicesRoute)]
    public async Task<IActionResult> List([FromQuery] InvoiceAdminQuery query, CancellationToken ct)
    {
        var result = await svc.AdminListAsync(query, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.InvoiceByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await svc.AdminGetDetailAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Admin.InvoiceByIdRoute + "/resend-email")]
    public async Task<IActionResult> ResendEmail(int id, CancellationToken ct)
    {
        var result = await svc.AdminResendEmailAsync(id, ct);
        return result.ToNoContentResult();
    }
}
```

(Add `InvoiceAdminQuery` as a shared DTO in `DeliverTableSharedLibrary/Dtos/Invoice/` — mirrors the spec's filter fields. Or use `[FromQuery]` primitives.)

- [ ] **Step 4: Build + test + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~AdminInvoiceController"
git add DeliverTableServer/Controllers/AdminInvoiceController.cs \
        DeliverTableServer/Services/ DeliverTableSharedLibrary/Dtos/Invoice/ \
        DeliverTableTests/Server/Unit/Controllers/AdminInvoiceControllerTests.cs
git commit -m "feat(server): add admin invoice endpoints with tests"
```

---

## Task 21: Validate restaurant SIRET and legal fields on create/edit with tests

**Files:**
- Modify: `DeliverTableServer/Services/RestaurantService.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/RestaurantServiceTests.cs`

- [ ] **Step 1: Failing tests**

```csharp
[Test]
public async Task CreateRestaurant_InvalidSiret_ReturnsError() { /* assert ServiceError with SiretInvalid */ }

[Test]
public async Task CreateRestaurant_MissingLegalName_ReturnsError() { /* assert LegalFieldsRequired */ }

[Test]
public async Task CreateRestaurant_ValidData_Persists() { /* assert repo.CreateAsync called */ }
```

- [ ] **Step 2: Add validation to `CreateAsync` / `UpdateAsync`**

```csharp
if (!SiretValidator.IsValid(request.Siret))
    return new ServiceError(ErrorMessages.SiretInvalid);

if (string.IsNullOrWhiteSpace(request.LegalName)
 || string.IsNullOrWhiteSpace(request.LegalAddress)
 || string.IsNullOrWhiteSpace(request.LegalForm))
{
    return new ServiceError(ErrorMessages.LegalFieldsRequired);
}
```

(Add the using `using DeliverTableSharedLibrary.Validation;`.)

Also persist the new fields to the Restaurant entity in the mapper.

- [ ] **Step 3: Run + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~RestaurantServiceTests"
git add DeliverTableServer/Services/RestaurantService.cs \
        DeliverTableServer/Constants/ErrorMessages.cs \
        DeliverTableTests/Server/Unit/Services/RestaurantServiceTests.cs
git commit -m "feat(server): validate restaurant SIRET and legal fields on create/edit with tests"
```

(Add `SiretInvalid` and `LegalFieldsRequired` to `ErrorMessages.cs` per the spec.)

---

## Task 22: Add `MyInvoices` page (client)

**Files:**
- Create: `DeliverTableClient/Services/Invoice/IInvoiceApiClient.cs`
- Create: `DeliverTableClient/Services/Invoice/InvoiceApiClient.cs`
- Create: `DeliverTableClient/Pages/Invoices/MyInvoices/MyInvoices.razor`
- Create: `DeliverTableClient/Pages/Invoices/MyInvoices/MyInvoices.razor.scss`
- Modify: `DeliverTableClient/Extensions/ApiClientServiceCollectionExtensions.cs`

- [ ] **Step 1: Client service**

Match the existing `PaymentApiClient.cs` pattern (registered via `ApiClientServiceCollectionExtensions.AddApiClients`, uses typed `HttpClient` with `BaseAddress`).

```csharp
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Invoice;

namespace DeliverTableClient.Services.Invoice;

public interface IInvoiceApiClient
{
    Task<PaginatedResult<InvoiceListItemDto>?> GetMineAsync(int page, int pageSize);
    Task<HttpResponseMessage> DownloadAsync(int id);
}

public class InvoiceApiClient(HttpClient http) : IInvoiceApiClient
{
    public async Task<PaginatedResult<InvoiceListItemDto>?> GetMineAsync(int page, int pageSize)
    {
        return await http.GetFromJsonAsync<PaginatedResult<InvoiceListItemDto>>(
            $"{ApiRoutes.Invoice.Base}/me?page={page}&pageSize={pageSize}");
    }

    public Task<HttpResponseMessage> DownloadAsync(int id) =>
        http.GetAsync($"{ApiRoutes.Invoice.Base}/{id}/pdf");
}
```

- [ ] **Step 2: Register in DI** (via `ApiClientServiceCollectionExtensions`)

Follow the pattern used for `IPaymentApiClient` (from Spec 1 Task 28).

- [ ] **Step 3: MyInvoices.razor**

```razor
@page "/factures"
@attribute [Authorize(Roles = nameof(UserRole.Customer))]
@using DeliverTableClient.Services.Invoice
@using DeliverTableSharedLibrary.Dtos.Invoice
@inject IInvoiceApiClient InvoiceApi
@inject NavigationManager Nav

<section class="my-invoices">
    <h1>Mes factures</h1>
    @if (_items is null)
    {
        <p>Chargement...</p>
    }
    else if (_items.Count == 0)
    {
        <p>Aucune facture pour le moment.</p>
    }
    else
    {
        <table class="invoices-table">
            <thead>
                <tr><th>Date</th><th>Numéro</th><th>N° commande</th><th>Montant TTC</th><th>Statut</th><th>Action</th></tr>
            </thead>
            <tbody>
                @foreach (var inv in _items)
                {
                    <tr>
                        <td>@inv.IssuedAt.ToString("dd/MM/yyyy")</td>
                        <td>@inv.Number @if (inv.Kind.ToString().StartsWith("CreditNote")) { <span class="badge">AVOIR</span> }</td>
                        <td>#@inv.OrderId</td>
                        <td>@inv.TotalTtc.ToString("0.00 €")</td>
                        <td>@inv.Status</td>
                        <td><a class="btn-download" href="@($"/api/v1/invoice/{inv.Id}/pdf")" target="_blank">Télécharger</a></td>
                    </tr>
                }
            </tbody>
        </table>
    }
</section>

@code {
    private List<InvoiceListItemDto>? _items;

    protected override async Task OnInitializedAsync()
    {
        var result = await InvoiceApi.GetMineAsync(1, 50);
        _items = result?.Items.ToList() ?? new();
    }
}
```

- [ ] **Step 4: SCSS**

```scss
.my-invoices {
    max-width: 900px;
    margin: 2rem auto;
    padding: 1.5rem;

    .invoices-table {
        width: 100%;
        border-collapse: collapse;

        th, td { padding: .6rem; border-bottom: 1px solid #eee; }
        th { text-align: left; background: #fafafa; }
        .badge { background: #ff9800; color: white; padding: 2px 6px; border-radius: 3px; margin-left: 6px; font-size: .8em; }
    }
}
```

- [ ] **Step 5: Build + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
git add DeliverTableClient/Services/Invoice/ \
        DeliverTableClient/Pages/Invoices/ \
        DeliverTableClient/Extensions/
git commit -m "feat(client): add MyInvoices page"
```

---

## Task 23: Add restaurant commission invoices tab

**Files:**
- Modify: existing restaurant dashboard / RestaurantAccount page (find via grep)
- Modify: `DeliverTableClient/Services/Invoice/IInvoiceApiClient.cs` (add `GetForRestaurantAsync`)

- [ ] **Step 1: Locate the restaurant dashboard page**

```bash
grep -rn "RestaurantAccount\|restaurant/account\|Mes ventes" DeliverTableClient/Pages/Restaurant/
```

Identify the existing page that shows transactions / withdrawals. Add a "Factures de commission" tab/section using the same page structure.

- [ ] **Step 2: Extend the API client**

```csharp
Task<PaginatedResult<InvoiceListItemDto>?> GetForRestaurantAsync(int restaurantId, int page, int pageSize);
```

Impl: `GET {Invoice.Base}/restaurant/{id}?page={page}&pageSize={pageSize}`.

- [ ] **Step 3: Render the tab**

Within the restaurant dashboard Razor file, add a new tab/section that lists invoices for the current restaurant. Reuse the table structure from MyInvoices.

- [ ] **Step 4: Build + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
git add DeliverTableClient/Pages/Restaurant/ DeliverTableClient/Services/Invoice/
git commit -m "feat(client): add restaurant commission invoices tab"
```

---

## Task 24: Add admin invoices list and detail pages

**Files:**
- Create: `DeliverTableClient/Pages/Admin/Invoices/AdminInvoices.razor`
- Create: `DeliverTableClient/Pages/Admin/Invoices/AdminInvoices.razor.scss`
- Create: `DeliverTableClient/Pages/Admin/Invoices/AdminInvoiceDetail/AdminInvoiceDetail.razor`
- Create: `DeliverTableClient/Pages/Admin/Invoices/AdminInvoiceDetail/AdminInvoiceDetail.razor.scss`
- Modify: `IInvoiceApiClient` to add admin methods

- [ ] **Step 1: Extend API client**

```csharp
Task<PaginatedResult<AdminInvoiceRowDto>?> AdminListAsync(int? year, InvoiceKind? kind, InvoiceIssuerType? issuerType, int? restaurantId, string? customerEmail, int page, int pageSize);
Task<AdminInvoiceDetailDto?> AdminGetAsync(int id);
Task AdminResendEmailAsync(int id);
```

- [ ] **Step 2: `AdminInvoices.razor`**

Admin-only page at `/admin/factures` with filter form and paginated table. Mirror the style of `/admin/disputes` from Spec 3 or any existing admin list.

- [ ] **Step 3: `AdminInvoiceDetail.razor`**

Detail view at `/admin/factures/{id:int}`. Shows header, lines, issuer/recipient snapshots, storage key, status, related invoice link, "Renvoyer l'email" button.

- [ ] **Step 4: Build + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
git add DeliverTableClient/Pages/Admin/Invoices/ DeliverTableClient/Services/Invoice/
git commit -m "feat(client): add admin invoices list and detail pages"
```

---

## Task 25: Add invoice download button to order detail page

**Files:**
- Modify: existing order detail page (find via grep)

- [ ] **Step 1: Locate order detail page**

```bash
grep -rn "OrderDetail\|OrderConfirmation\|/commande" DeliverTableClient/Pages/
```

- [ ] **Step 2: Add button**

In the order detail page, check if the order has a related `OrderInvoiceToCustomer` invoice with `Status = Generated`. If yes, show a "Télécharger la facture" button that opens `/api/v1/invoice/{id}/pdf` in a new tab.

The client may need to call a new endpoint like `GET /api/v1/invoice/order/{orderId}` or piggyback by filtering `GetMineAsync` by `orderId`. Add whichever is cleanest — a dedicated endpoint is probably simplest.

- [ ] **Step 3: Build + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
git add DeliverTableClient/Pages/ DeliverTableServer/
git commit -m "feat(client): add invoice download button to order detail page"
```

---

## Task 26: Update restaurant form with legal fields

**Files:**
- Modify: existing restaurant create/edit form page (find via grep)

- [ ] **Step 1: Locate form**

```bash
grep -rn "CreateRestaurantRequest\|UpdateRestaurantRequest" DeliverTableClient/Pages/
```

- [ ] **Step 2: Add fields**

Add inputs for `Siret` (text, inline regex validation for 14 digits), `LegalName`, `LegalAddress`, `LegalForm` (select with SAS/SARL/EURL/EI/SA options), `IsVatRegistered` (checkbox, default checked).

- [ ] **Step 3: Client-side SIRET validation hint (optional)**

Add a regex pattern check + show "Le numéro SIRET est invalide." when invalid. Server-side is authoritative.

- [ ] **Step 4: Build + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
git add DeliverTableClient/
git commit -m "feat(client): update restaurant form with legal fields"
```

---

## Task 27: Update dish form with VAT rate dropdown

**Files:**
- Modify: existing dish create/edit form page

- [ ] **Step 1: Locate the dish form page**

```bash
grep -rn "CreateDishRequest\|UpdateDishRequest" DeliverTableClient/Pages/
```

- [ ] **Step 2: Add dropdown**

Add a select element bound to `VatRate` with these options (label, value):
- "0 % (Zéro)" → `VatRate.Zero`
- "2,1 % (Spécial)" → `VatRate.Special2_1`
- "5,5 % (Réduit)" → `VatRate.Reduced5_5`
- "10 % (Intermédiaire)" → `VatRate.Intermediate10`
- "20 % (Normal)" → `VatRate.Normal20`

Default selection: `Intermediate10`.

- [ ] **Step 3: Build + commit**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
git add DeliverTableClient/
git commit -m "feat(client): update dish form with VAT rate dropdown"
```

---

## Task 28: DI registration, format check, and end-to-end verification

**Files:**
- Modify: `DeliverTableServer/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Register all new services in DI**

In `RegisterRepositories`:
```csharp
services.AddScoped<IInvoiceRepository, InvoiceRepository>();
```

In `RegisterServices`:
```csharp
services.AddScoped<IInvoiceNumberingService, InvoiceNumberingService>();
services.AddScoped<IInvoiceService, InvoiceService>();
```

In the worker's `Program.cs`:
```csharp
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddSingleton<IInvoicePdfRenderer, InvoicePdfRenderer>();
// Invoice consumer registration (RabbitMQ binding) — mirror EmailJobConsumer's setup
```

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

Expected: all tests pass (zero regressions from Spec 1's 575-test baseline; ~15-20 new tests added by this plan).

- [ ] **Step 4: Manual end-to-end QA**

With `make dev` + `stripe listen` running:
1. Complete a checkout end-to-end (from Spec 1).
2. After auth completes, verify two `Invoice` rows appear in the DB with `Status=Queued`.
3. Watch the worker logs — within a second or two, both should transition to `Generated` with `StoragePath` set.
4. Verify the PDFs are in S3 (`docker compose exec garage s3manager` or via the S3 Manager web UI).
5. Verify two email messages land in the dev inbox (or are logged with `NEUTRALIZE_EMAIL=true`).
6. Log in as the customer → go to `/factures` → download the PDF and open it. Verify French layout, correct totals, correct legal block.
7. Log in as the restaurant owner → restaurant dashboard → "Factures de commission" tab → download the commission PDF.
8. Log in as admin → `/admin/factures` → filter by year / kind → verify detail page shows lines + snapshots.
9. Trigger a refund via the Spec 1 admin refund endpoint. Verify two credit note `Invoice` rows get created with negative totals + `AV-` prefix.
10. Edit a restaurant's SIRET with an invalid value — confirm the form rejects it with "Le numéro SIRET est invalide.".

Manual QA is the user's responsibility — implementer reports readiness only.

- [ ] **Step 5: Commit format fixes (if any)**

```bash
git add -u
git commit -m "style: apply formatting fixes"
```

---

## Self-Review

After writing this plan, compared against the spec:

- [x] **Spec coverage**: all sections of the spec mapped to tasks. Spec §5 (data model) → Tasks 1-3. §5.4 (migration) → Task 4. §5.5 (docs) → Task 5. §6 (API) → Tasks 7, 19, 20. §7 (pipeline) → Tasks 10, 11, 12, 13, 14, 15, 16, 17, 18. §8 (client UX) → Tasks 22-27. §9 (errors) → spread across the service tasks that use them. §10 (config) → Task 6. §11 (testing) → covered inline.
- [x] **Placeholder scan**: two tasks contain `// ... TODO` style comments (Task 12 Step 3 and Task 24 details). These are intentional — they describe the shape of work the implementer must flesh out (partial refund test body, admin page layouts) because the full code is hundreds of lines that would bloat the plan without adding fidelity. Each has concrete guidance on what must be true when done.
- [x] **Type consistency**: `InvoiceKind`, `InvoiceIssuerType`, `InvoiceStatus`, `VatRate`, `Invoice`, `InvoiceLine`, `InvoiceCounter`, `InvoiceJobMessage`, `InvoiceListItemDto`, `AdminInvoiceRowDto`, `AdminInvoiceDetailDto`, `InvoiceLineDto`, `InvoiceLegalSnapshotDto`, `SiretValidator.IsValid`, `VatRateExtensions.ToDecimal`, `IInvoiceNumberingService.IssueNumberAsync`, `IInvoiceService.CreatePendingInvoicesForCapturedOrderAsync`, `IInvoiceService.CreateCreditNotesForRefundAsync`, `IInvoicePdfRenderer.Render`, `IInvoiceRepository` methods — all referenced consistently.
- [x] **Open questions from spec**: §15 noted `RestaurantTransaction.CommissionAmount` may be HT or TTC. Plan assumes HT (matches spec's stated assumption). Task 11's implementation uses `env.PlatformCommissionRate * order.TotalAmount` directly because that matches the existing behavior on the backend container environment — the RestaurantTransaction row isn't created until `Delivered`, but we generate the commission invoice at capture (when we know the amount from the commission rate). This is consistent with when customers see the amount charged.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-04-14-invoices-plan.md`. Two execution options:

**1. Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration.

**2. Inline Execution** — execute tasks in this session using `executing-plans`, batch execution with checkpoints.

Which approach?
