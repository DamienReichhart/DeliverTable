# Monthly Commission Statements — Design Spec

**Date:** 2026-05-29
**Status:** Draft — pending user review
**Author:** Damien (via brainstorming session)

## 1. Summary

Replace per-order platform commission invoicing with a single monthly aggregated **commission statement** per restaurant. On the 1st of each month (02:00 Europe/Paris), a scheduled job produces one statement per restaurant covering every delivered, paid order from the previous calendar month. Refunds of already-invoiced orders generate standalone credit notes. Customer invoices (paid by the diner) are unchanged.

## 2. Motivation

The current system creates one commission invoice per order, fired from the Stripe `payment_intent.amount_capturable_updated` webhook ([InvoiceService.cs:587](../../../DeliverTableServer/Services/InvoiceService.cs#L587)). For restaurants that process many orders per month, this means dozens of small PDF documents to reconcile. A single monthly statement is easier for both sides to process and matches standard B2B accounting practice.

## 3. Scope

### In scope

- New `CommissionStatement` entity, separate from `Invoice`, with its own lines, PDF, S3 storage, email, numbering.
- Quartz.NET-driven monthly job in `DeliverTableScheduler` (Europe/Paris, 1st @ 02:00).
- Idempotent generation guarded by a partial unique index on `(RecipientRestaurantId, PeriodYear, PeriodMonth)` for `Kind = Invoice`.
- Refund handling:
  - Refunds of orders not yet invoiced (current period): order is excluded from the upcoming monthly statement (full refund) or invoiced on the net amount (partial refund).
  - Refunds of already-invoiced orders (prior period): standalone `CommissionStatement` with `Kind = CreditNote`, one per refund event.
- Admin catch-up endpoint to manually re-trigger generation for a given period.
- Cutover gate: from a fixed date constant, stop creating per-order `Invoice` rows of kind `CommissionInvoiceToRestaurant`.

### Out of scope (deliberate, follow-ups)

- Restaurant-facing UI to list/download statements.
- Admin list/download UI (catch-up endpoint only in v1).
- Settlement tracking (Paid/Unpaid status on statements).
- SEPA/Stripe direct-debit collection of the commission.
- Backfill/migration of historical per-order commission invoices.
- Extra email-retry logic beyond what `JobSweepService` already provides.

### Untouched

- Customer invoices (`InvoiceKind.CustomerInvoiceFromRestaurant`).
- The existing per-order refund credit-note flow for legacy (pre-cutover) commission invoices.
- The `InvoiceKind.CommissionInvoiceToRestaurant` enum value stays for historical reads.

## 4. Data model

### New entity: `CommissionStatement`

Location: `DeliverTableInfrastructure/Models/CommissionStatement.cs`

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `Number` | `string` | `COMM-{YYYY}-{MM}-{seq:D6}` (Invoice) or `AVOIR-COMM-{YYYY}-{MM}-{seq:D6}` (CreditNote) |
| `Kind` | `CommissionStatementKind` | `Invoice` \| `CreditNote` |
| `RecipientRestaurantId` | `Guid` | FK → Restaurant |
| `PeriodYear` | `int` | Calendar year, Europe/Paris. For `CreditNote`, copies the period of the original invoice statement (the order's delivery month) — *not* the month the refund happened. |
| `PeriodMonth` | `int` | 1–12. Same semantics as `PeriodYear`. |
| `IssuedAt` | `DateTimeOffset` | UTC timestamp of row creation |
| `TotalHt` | `decimal(9,2)` | Sum of line `LineHt` (negative for credit notes) |
| `TotalVat` | `decimal(9,2)` | Sum of line `LineVat` |
| `TotalTtc` | `decimal(9,2)` | Sum of line `LineTtc` |
| `Status` | `CommissionStatementStatus` | `Queued` → `Generated` \| `Failed` |
| `PdfStorageKey` | `string?` | S3 key once rendered |
| `FailureReason` | `string?` | Set when `Status = Failed` |
| `IssuerLegalSnapshot` | owned type (jsonb) | Platform legal info at issue time (mirrors existing Invoice snapshot pattern) |
| `RecipientLegalSnapshot` | owned type (jsonb) | Restaurant legal info at issue time |
| `RelatedStatementId` | `Guid?` | For `CreditNote` only — points to the original Invoice statement |

### New entity: `CommissionStatementLine`

Location: `DeliverTableInfrastructure/Models/CommissionStatementLine.cs`

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `CommissionStatementId` | `Guid` | FK |
| `OrderId` | `Guid` | FK → Order (always set — even a credit note covers exactly one order) |
| `OrderNumber` | `string` | Snapshot for display |
| `OrderCompletedAt` | `DateTimeOffset` | Snapshot |
| `OrderTotalAmount` | `decimal(9,2)` | Base on which commission was calculated (post-refund net for partial refunds) |
| `CommissionRateSnapshot` | `decimal(5,4)` | The rate used (e.g. `0.1500`) — captured for audit |
| `LineHt` | `decimal(9,2)` | Negative for credit-note lines |
| `LineVat` | `decimal(9,2)` | Negative for credit-note lines |
| `LineTtc` | `decimal(9,2)` | Negative for credit-note lines |
| `RefundEventId` | `string?` | Set only on credit-note lines; Stripe refund/charge id; unique when not null |

### Changes to `Order`

Two nullable FK columns added:

- `CommissionStatementId : Guid?` — set when the order is included in an `Invoice`-kind statement. Used by the sweep job to find unbilled orders cheaply.
- `CommissionRefundStatementId : Guid?` — informational pointer to the most recent credit-note statement issued for refunds on this order. Not used for dedup (multiple partial refunds can produce multiple credit notes — see §5 for how dedup actually works via `RefundEventId` on the line).

### Indexes / constraints

- Partial unique index on `(RecipientRestaurantId, PeriodYear, PeriodMonth) WHERE Kind = Invoice` — at most one invoice per restaurant per month.
- Add nullable `RefundEventId : string?` to `CommissionStatementLine` — populated only for credit-note lines, holds the Stripe refund/charge id, used for dedup.
- Partial unique index on `CommissionStatementLine(RefundEventId) WHERE RefundEventId IS NOT NULL` — prevents processing the same Stripe refund event twice.
- B-tree index on `Order(CommissionStatementId)` for sweep query.
- B-tree index on `CommissionStatement(RecipientRestaurantId, PeriodYear, PeriodMonth)`.

### New shared enums

Location: `DeliverTableSharedLibrary/Enums/`

```csharp
public enum CommissionStatementKind { Invoice, CreditNote }
public enum CommissionStatementStatus { Queued, Generated, Failed }
```

### New DTOs

Location: `DeliverTableSharedLibrary/Dtos/`

- `CommissionStatementDto` (id, number, kind, period, totals, status, recipient summary)
- `CommissionStatementLineDto` (order snapshot fields, commission amounts)
- `CommissionStatementGenerationResultDto` (see §7)

## 5. Generation job

### Quartz.NET setup

- Add `Quartz` and `Quartz.Extensions.Hosting` NuGet packages to `DeliverTableScheduler`.
- Configure in `DeliverTableScheduler/Program.cs` via `services.AddQuartz(...)` and `services.AddQuartzHostedService(...)`.
- Job class: `MonthlyCommissionStatementJob : IJob`, decorated with `[DisallowConcurrentExecution]`.
- Trigger: cron `0 0 2 1 * ?` with `.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"))`.
- Misfire policy: `WithMisfireHandlingInstructionFireAndProceed` — fires as soon as the scheduler comes back if it was down at 02:00. The DB uniqueness constraint guarantees idempotency.
- Timezone data: `Europe/Paris` should resolve in the .NET 10 base image. Verify during implementation; if missing, add `tzdata` to the Scheduler Dockerfile.

### Job logic

`MonthlyCommissionStatementJob.Execute`:

1. Compute the **target period** = previous calendar month in Europe/Paris.
   - Firing on 2026-06-01 02:00 Paris → target = (year=2026, month=5).
2. Call `ICommissionStatementService.GenerateForPeriodAsync(year, month, ct)`.
3. Log the returned `CommissionStatementGenerationResultDto`.

### Service: `ICommissionStatementService.GenerateForPeriodAsync(int year, int month, CancellationToken ct)`

Location: `DeliverTableServer/Services/CommissionStatementService.cs`

Returns `ServiceResult<CommissionStatementGenerationResultDto>`.

1. Compute `[periodStartUtc, periodEndUtc)` from `(year, month)` interpreted in Europe/Paris.
2. Query restaurant IDs with at least one eligible order in the window:
   - `Status = OrderStatus.Delivered`
   - `PaymentStatus = PaymentStatus.Completed`
   - `CompletedAt` (or whichever timestamp marks Delivered transition — confirmed in implementation plan) within `[periodStartUtc, periodEndUtc)`
   - `CommissionStatementId IS NULL`
   - Net amount (after refunds) > 0
3. For each restaurant:
   - Check uniqueness guard: does an `Invoice`-kind statement already exist for `(restaurant, year, month)`? If yes → record as skipped and continue (idempotent re-run).
   - Open one DB transaction:
     - Fetch eligible orders + their refund totals.
     - For each order, compute commission using `PlatformCommissionRate` and `PlatformVatApplicable` from `AppEnvironment` ([AppEnvironment.cs:19](../../../DeliverTableServer/Configuration/AppEnvironment.cs#L19)) — same math as today's `BuildCommissionInvoice` ([InvoiceService.cs:587](../../../DeliverTableServer/Services/InvoiceService.cs#L587)) but applied to `OrderTotalAmount − totalRefunded`.
     - Snapshot the rate on each line (`CommissionRateSnapshot`).
     - Allocate `Number` from the global Postgres sequence `commission_statement_number_seq`.
     - Insert `CommissionStatement` (Status=Queued) + all `CommissionStatementLine` rows.
     - Update each included order's `CommissionStatementId`.
   - Commit, then publish a RabbitMQ message `CommissionStatementJob { StatementId }` to routing key `commission-statement.render` on existing `delivertable.jobs` exchange.
4. Return aggregate result.

### Refund handling for prior-period orders

Lives in `PaymentService` (or wherever Stripe refund webhooks are handled today — `PaymentService.HandleStripeEventAsync` in [PaymentService.cs](../../../DeliverTableServer/Services/PaymentService.cs)).

On refund webhook for an order:

- If `Order.CommissionStatementId IS NULL` → no statement yet. Do nothing here; the next monthly run will naturally exclude (full refund) or use net amount (partial refund).
- If `Order.CommissionStatementId IS NOT NULL`:
  - Check `CommissionStatementLine.RefundEventId = <this refund event id>` exists. If yes → already processed, no-op.
  - Compute commission delta on refunded portion using the **snapshotted rate** from the original statement line (not current `AppEnvironment.PlatformCommissionRate` — the rate may have changed since).
  - Insert `CommissionStatement(Kind=CreditNote, RelatedStatementId=originalStatementId, PeriodYear/PeriodMonth=originalPeriod)` + one negative line with `RefundEventId = <stripe id>`.
  - Update `Order.CommissionRefundStatementId` to point to this new credit note (informational only, overwrites any prior pointer).
  - Enqueue PDF render via the same RabbitMQ pipeline.

Multiple partial refunds on the same order produce multiple credit-note statements (1 statement = 1 line = 1 refund event). The unique index on `RefundEventId` guarantees idempotency at the DB level.

### Cutover

- Date constant in `DeliverTableServer/Constants/CommissionInvoicingCutover.cs`:
  ```csharp
  public static class CommissionInvoicingCutover
  {
      public static readonly DateTimeOffset MonthlyStartUtc =
          new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
  }
  ```
  **Final cutover date to be confirmed by user before merge.**
- In [InvoiceService.cs:587](../../../DeliverTableServer/Services/InvoiceService.cs#L587) `BuildCommissionInvoice` / `CreatePendingInvoicesForCapturedOrderAsync`: gate commission-invoice creation behind `if (DateTimeOffset.UtcNow < CommissionInvoicingCutover.MonthlyStartUtc)`. Customer-invoice creation is untouched.
- After cutover: customer invoices continue per-order; commission statements only via the monthly job.

### Concurrency

- `[DisallowConcurrentExecution]` on `MonthlyCommissionStatementJob`.
- Admin catch-up endpoint calls the same service method; uniqueness constraint blocks any race.

## 6. PDF & email

### PDF renderer

`CommissionStatementPdfRenderer` in `DeliverTableWorker/Services/`, mirroring [InvoicePdfRenderer.cs](../../../DeliverTableWorker/Services/InvoicePdfRenderer.cs).

A4 layout, French:

- Header: "RELEVÉ DE COMMISSIONS" (Invoice) or "AVOIR DE COMMISSIONS" (CreditNote).
- Issuer block (platform legal info from snapshot) + Recipient block (restaurant legal info from snapshot).
- Period banner: "Période du 1er mai 2026 au 31 mai 2026".
- Table of orders: `N° commande | Date livraison | Montant TTC | Taux | Commission HT | TVA | Commission TTC`.
- Totals block: Total HT, TVA, Total TTC.
- Footer: statement number, platform legal mentions.

S3 storage key: `commission-statements/{year}/{month}/{number}.pdf`.

### Worker consumer

`CommissionStatementJobConsumer` in `DeliverTableWorker/Consumers/`, mirroring `InvoiceJobConsumer`.

- Bound to routing key `commission-statement.render` on existing `delivertable.jobs` exchange.
- On success: set `Status = Generated`, set `PdfStorageKey`, enqueue email via `EmailJobConsumer`.
- On failure: set `Status = Failed` with reason. Existing [JobSweepService](../../../DeliverTableWorker/Services/JobSweepService.cs) retries stale jobs.

### Email

Reuse existing `EmailJobConsumer`.

- Subject (Invoice): `Votre relevé de commissions de {mois} {année} est disponible`
- Subject (CreditNote): `Avoir sur commissions — commande {orderNumber}`
- Body: French template with statement summary + PDF attached.
- Recipient: restaurant's billing contact email (existing field on `Restaurant`).

### Numbering

- Global Postgres sequence `commission_statement_number_seq` — never reset, never per-restaurant. Numbers are globally unique and monotonic, simplifying audit.
- Format:
  - Invoice: `COMM-{YYYY}-{MM}-{seq:D6}` (e.g. `COMM-2026-05-000042`)
  - CreditNote: `AVOIR-COMM-{YYYY}-{MM}-{seq:D6}`
- `YYYY-MM` reflects the **period**, not the issue date — so a credit note emitted in August 2026 for an order originally invoiced in May 2026 still carries `2026-05` in its number.

## 7. Admin catch-up endpoint

**Route:** `POST /api/v1/admin/commission-statements/run`

Added to `DeliverTableSharedLibrary/Constants/ApiRoutes.cs` per project convention.

**Authorization:** `[Authorize(Roles = nameof(UserRole.Admin))]`.

**Controller:** new `AdminCommissionStatementController` in `DeliverTableServer/Controllers/`.

**Request body:**

```json
{
  "year": 2026,
  "month": 5
}
```

Both fields optional. When omitted, defaults to the most recent fully-closed calendar month in Europe/Paris.

**Response:** `CommissionStatementGenerationResultDto`

```json
{
  "periodYear": 2026,
  "periodMonth": 5,
  "restaurantsProcessed": 87,
  "statementsCreated": 84,
  "restaurantsSkipped": 3,
  "failures": [
    { "restaurantId": "…", "reason": "…" }
  ]
}
```

**Behaviour:**

- Idempotent — re-running for an already-processed period returns `restaurantsSkipped` reflecting the no-ops.
- Synchronous from the caller's perspective: returns once DB rows are inserted. PDF rendering happens async via RabbitMQ as usual.
- Wrapped in `ServiceResult<T>` per project convention; controller maps via `.ToOkResult()`.

## 8. Testing strategy

Project mandates TDD for services and controllers ([CLAUDE.md](../../../CLAUDE.md)). Tests live with the code they test.

| Layer | Tests |
|---|---|
| `CommissionStatementService` (unit, mock repo + env) | Picks correct period; one statement per restaurant; skips restaurant with no eligible orders; skips already-billed restaurants (idempotency); correct commission math; partial-refund net-amount math; rate snapshot captured; transaction rolls back on line-insert failure |
| `MonthlyCommissionStatementJob` (unit) | Computes previous-month Europe/Paris correctly across DST transitions; delegates to service; `[DisallowConcurrentExecution]` attribute present |
| `CommissionStatementService.HandleRefundForPriorPeriodAsync` (unit) | Emits CreditNote for prior-period order; no-op for current-period order; partial refund produces correct delta line; uses snapshotted rate (not current); dedup via `RefundEventId` |
| `AdminCommissionStatementController` (unit, mock service) | Defaults to last closed month when body omitted; authorizes Admin only; maps ServiceResult; rejects invalid year/month |
| Integration (existing `TestDatabase` fixture) | End-to-end: insert orders, run service, verify statement + lines + order FK; verify partial unique index blocks double-insert |
| PDF renderer | Render a fixture statement, assert non-empty byte output (mirrors existing invoice renderer tests if any) |
| Cutover gate (`InvoiceService` test) | Before cutover, per-order commission invoice still created; after cutover, only the customer invoice is created |

## 9. Migration

Single EF Core migration adds:

- `CommissionStatement` table + indexes.
- `CommissionStatementLine` table + indexes.
- `Order.CommissionStatementId` and `Order.CommissionRefundStatementId` nullable FK columns.
- Partial unique indexes (per §4).
- Postgres sequence `commission_statement_number_seq`.

No data backfill — historical `Invoice` rows of kind `CommissionInvoiceToRestaurant` remain.

## 10. Open items (to resolve in implementation plan)

- Confirm final cutover date (currently placeholder `2026-07-01`).
- Confirm timestamp field on `Order` that marks "Delivered transition" — there may not be one today; if not, add a `DeliveredAt` column in the same migration.
- Confirm `Europe/Paris` timezone resolves in the Scheduler container; add `tzdata` to Dockerfile if not.
- Email template HTML layout (final wording, branding).

## 11. Out-of-scope follow-ups (tracked, not in this PR)

- Restaurant dashboard page to view and download statements.
- Admin list/download endpoints (extend `AdminInvoiceController` style).
- Settlement / payment status on statements.
- SEPA or Stripe direct-debit collection of commissions.
- Deprecation pathway for `InvoiceKind.CommissionInvoiceToRestaurant` once all legacy invoices are archived.
