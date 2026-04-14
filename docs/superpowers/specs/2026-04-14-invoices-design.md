# Invoices — Design Spec

**Status**: Approved (brainstorm phase). Ready for implementation plan.
**Scope**: Spec 2 of 3. Depends on Spec 1 (Stripe payments core) being merged.
**Date**: 2026-04-14

---

## 1. Goal

Generate legally compliant French invoices for every completed order: one from the restaurant to the customer (for the food) and one from the platform to the restaurant (for the commission). Auto-generate credit notes (avoirs) on refund. Store PDFs in object storage for 10 years. Deliver via email attachment and make them retrievable in customer/restaurant/admin dashboards.

## 2. In scope

- `Invoice`, `InvoiceLine`, `InvoiceCounter` entities and related EF configs / migration.
- Atomic invoice numbering (one sequence per legal entity per year, gapless).
- Restaurant legal fields: `Siret`, `LegalName`, `LegalAddress`, `LegalForm`, `IsVatRegistered`.
- Platform legal entity via env vars (`PLATFORM_LEGAL_NAME`, `PLATFORM_SIRET`, `PLATFORM_VAT_NUMBER`, `PLATFORM_ADDRESS`, `PLATFORM_LEGAL_FORM`, `PLATFORM_VAT_APPLICABLE`).
- Per-dish VAT rate via `VatRate` enum (`Intermediate10` default).
- Async PDF generation pipeline: server queues `InvoiceJobMessage`, worker renders via QuestPDF, uploads to S3, emails with attachment.
- Credit notes mirror invoices on refund (two per refund: customer-side + platform-side), with `AV-` prefix, sharing the issuer's counter.
- Customer, restaurant-owner, and admin UIs for listing / downloading invoices.
- 10-year hard retention (no delete path).
- SIRET Luhn validation.
- French legal requirements on every PDF: issuer legal block, sequential immutable number, HT / VAT / TTC breakdown, VAT exemption clause when applicable.

## 3. Out of scope

- Spec 3: dispute handling.
- Historical backfill of invoices for pre-deployment orders.
- Printing / postal delivery.
- Accounting software integration (SAGE, Cegid, etc.).
- Automated re-numbering tooling for legal corrections (invoices are immutable; corrections only via credit notes).

## 4. Architecture overview

```
[Spec 1 webhook] payment_intent.amount_capturable_updated
   ↓
[Server] PaymentService.HandleAuthorizationCompletedAsync
   ├─ Order.Status: AwaitingPayment → Pending
   ├─ Loyalty/discount committed, cart cleared
   └─ IInvoiceService.CreatePendingInvoicesForCapturedOrderAsync(orderId)
        ├─ IInvoiceNumberingService.IssueNumberAsync (DB row-lock atomic counter)
        ├─ Build + persist Invoice (Queued) + InvoiceLines, with legal snapshots
        └─ Publish InvoiceJobMessage × 2 (customer + restaurant)
   ↓
[Worker] InvoiceJobConsumer
   ├─ Render PDF via IInvoicePdfRenderer (QuestPDF)
   ├─ Upload to S3: invoices/{YYYY}/{MM}/{number}.pdf
   ├─ Invoice.StoragePath set, Status → Generated
   └─ Publish EmailJobMessage with AttachmentStoragePath
   ↓
[Worker] EmailJobConsumer (extended)
   ├─ Fetch PDF bytes from S3
   └─ Send email with PDF attached (Hostinger SMTP, existing)

// Refund path
[Spec 1 webhook] charge.refunded
   ↓
[Server] PaymentService.HandleChargeRefundedAsync
   ├─ Upsert Refund row (as in Spec 1)
   └─ IInvoiceService.CreateCreditNotesForRefundAsync(refundId)
        ├─ Load original pair of invoices
        └─ Create credit notes (AV-… prefix) with prorated negative lines
```

## 5. Data model

### 5.1 New entities

**`Invoice`** (`DeliverTableInfrastructure/Models/Invoice.cs`):

| Field | Type | Notes |
|---|---|---|
| `Id` | int, PK | |
| `Number` | string(50) | unique, immutable, e.g. `DT-2026-000123`, `R0042-2026-000123`, `AV-R0042-2026-000045` |
| `Kind` | `InvoiceKind` enum | see §5.3 |
| `OrderId` | int, FK → `Order` | required |
| `IssuerType` | `InvoiceIssuerType` enum | |
| `IssuerRestaurantId` | int?, FK → `Restaurant` | non-null iff `IssuerType=Restaurant` |
| `RecipientUserId` | int?, FK → `User` | |
| `RecipientRestaurantId` | int?, FK → `Restaurant` | |
| `RelatedInvoiceId` | int?, FK → `Invoice` | set on credit notes → references original |
| `IssuedAt` | DateTime UTC | |
| `TotalHt` / `TotalVat` / `TotalTtc` | decimal(9,2) | |
| `Currency` | string(3) | `"EUR"` |
| `StoragePath` | string(400)? | S3 key; null until worker finishes |
| `Status` | `InvoiceStatus` enum | `Queued`, `Generated`, `Failed` |
| `FailureReason` | string(2000)? | populated on `Failed` |
| `IssuerLegalSnapshotJson` | string(4000) | immutable legal snapshot |
| `RecipientSnapshotJson` | string(4000) | immutable recipient snapshot |
| `CreatedAt` / `UpdatedAt` | DateTime UTC | |

Indexes: unique on `Number`; non-unique on `OrderId`, `RecipientUserId`, `RecipientRestaurantId`.

**`InvoiceLine`**:

| Field | Type | Notes |
|---|---|---|
| `Id` | int, PK | |
| `InvoiceId` | int, FK → `Invoice` | cascade delete |
| `Description` | string(500) | |
| `Quantity` | decimal(9,3) | fractional allowed |
| `UnitPriceTtc` | decimal(9,2) | source of truth (we store TTC) |
| `UnitPriceHt` | decimal(9,2) | derived = TTC / (1 + rate) |
| `VatRate` | decimal(5,2) | e.g. `10.00` |
| `LineHt` / `LineVat` / `LineTtc` | decimal(9,2) | precomputed aggregates |
| `SortOrder` | int | stable rendering |

**`InvoiceCounter`**:

| Field | Type | Notes |
|---|---|---|
| `Id` | int, PK | |
| `EntityType` | `InvoiceIssuerType` enum | |
| `EntityId` | int? | null for platform (global), `RestaurantId` for restaurants |
| `Year` | int | |
| `NextNumber` | int | 1-based |

Unique index on (`EntityType`, `EntityId`, `Year`). `SELECT ... FOR UPDATE` during issuance within the creating transaction.

**`IInvoiceNumberingService` interface** (infra):

```csharp
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

Returns the formatted number (e.g. `DT-2026-000123` or `AV-R0042-2026-000045`). Both invoices and credit notes share the same counter row; prefix is computed from `isCreditNote` flag.

### 5.2 Modified entities

- **`Restaurant`** — add: `Siret` string(14), `LegalName` string(200), `LegalAddress` string(500), `LegalForm` string(50), `IsVatRegistered` bool (default `true`). Initial migration leaves these nullable / `""` default; a later migration tightens NOT NULL after backfill.
- **`Dish`** — add: `VatRate` `VatRate` enum column. Default `Intermediate10` for existing rows.

### 5.3 New enums (`DeliverTableSharedLibrary/Enums/`)

**`VatRate`** (applied to Dish):

| Member | Code | Real rate |
|---|---|---|
| `Zero` | 0 | 0% |
| `Special2_1` | 2 | 2.1% |
| `Reduced5_5` | 5 | 5.5% |
| `Intermediate10` | 10 | 10% |
| `Normal20` | 20 | 20% |

Real decimal rate exposed via `VatRateExtensions.ToDecimal(this VatRate)`. Stored int value is a display code.

**`InvoiceKind`**: `OrderInvoiceToCustomer = 0`, `CommissionInvoiceToRestaurant = 1`, `CreditNoteToCustomer = 2`, `CommissionCreditNoteToRestaurant = 3`.

**`InvoiceIssuerType`**: `Platform = 0`, `Restaurant = 1`.

**`InvoiceStatus`**: `Queued = 0`, `Generated = 1`, `Failed = 2`.

### 5.4 Migration

Single migration `AddInvoicing`:
- Creates `Invoice`, `InvoiceLine`, `InvoiceCounter` tables.
- Adds legal columns to `Restaurant` (nullable or default `""` for existing rows).
- Adds `VatRate` column to `Dish` with default `Intermediate10 = 10` for historical rows.

### 5.5 DB docs updates

- `docs/db/er-diagram.md` — new entities, columns, relationships.
- `docs/db/data-dictionary.md` — new entries for all new fields and enums.

## 6. API contracts

### 6.1 New routes (`ApiRoutes.cs`)

```csharp
public static class Invoice
{
    public const string Base = "api/v1/invoice";
    public const string MyListRoute = "me";
    public const string RestaurantListRoute = "restaurant/{id:int}";
    public const string DownloadRoute = "{id:int}/pdf";
}
```

Extend `ApiRoutes.Admin`:

```csharp
public const string InvoicesRoute = "invoices";
public const string InvoiceByIdRoute = "invoices/{id:int}";
```

### 6.2 Endpoints

| Method + path | Auth | Purpose |
|---|---|---|
| `GET /api/v1/invoice/me` | Customer | Paginated list of recipient-matched invoices |
| `GET /api/v1/invoice/restaurant/{id}` | RestaurantOwner (must own) or Admin | Restaurant's commission invoices |
| `GET /api/v1/invoice/{id}/pdf` | Recipient-user / recipient-restaurant-owner / Admin | Streams PDF from S3 (404 missing, 403 unauthorized, 409 not ready) |
| `GET /api/v1/admin/invoices` | Admin | Audit list with filters (year, kind, issuerType, restaurantId, customerEmail substring) |
| `GET /api/v1/admin/invoices/{id}` | Admin | Full detail including lines + snapshots; allows re-sending email |

PDF response uses `Content-Type: application/pdf`, `Content-Disposition: attachment; filename="{Number}.pdf"`.

### 6.3 DTOs (`DeliverTableSharedLibrary/Dtos/Invoice/`)

```csharp
public record InvoiceListItemDto(
    int Id, string Number, InvoiceKind Kind, int OrderId,
    DateTime IssuedAt, decimal TotalTtc, string Currency, InvoiceStatus Status);

public record AdminInvoiceRowDto(
    int Id, string Number, InvoiceKind Kind, InvoiceIssuerType IssuerType,
    string IssuerName, string RecipientName, DateTime IssuedAt,
    decimal TotalTtc, InvoiceStatus Status);

public record AdminInvoiceDetailDto(
    InvoiceListItemDto Header,
    List<InvoiceLineDto> Lines,
    InvoiceLegalSnapshotDto Issuer,
    InvoiceLegalSnapshotDto Recipient,
    int? RelatedInvoiceId);

public record InvoiceLineDto(
    string Description, decimal Quantity,
    decimal UnitPriceHt, decimal UnitPriceTtc,
    decimal VatRate, decimal LineHt, decimal LineVat, decimal LineTtc);

public record InvoiceLegalSnapshotDto(
    string Name, string LegalForm, string Siret,
    string VatNumber, string Address);
```

### 6.4 Modified `RestaurantController`

Existing restaurant create/edit DTOs (`CreateRestaurantRequest`, `UpdateRestaurantRequest`) gain the legal fields. Service-level validation:
- `Siret` must pass Luhn and be exactly 14 digits.
- `LegalName`, `LegalAddress`, `LegalForm` required non-empty for active restaurants that process payments (soft-enforced via service errors during invoicing; hard-enforced after backfill migration).

## 7. Invoice generation pipeline

### 7.1 RabbitMQ message

```csharp
public sealed record InvoiceJobMessage(int InvoiceId);
```

Published to queue `invoice`. Consumer: `InvoiceJobConsumer` in `DeliverTableWorker`.

### 7.2 `IInvoiceService.CreatePendingInvoicesForCapturedOrderAsync(int orderId, CancellationToken ct)`

Called from `PaymentService.HandleAuthorizationCompletedAsync` (Spec 1 webhook handler).

1. Load order + items + restaurant + customer + `RestaurantTransaction` via single EF include.
2. Idempotency: if any `Invoice` with `OrderId = orderId` and `Kind = OrderInvoiceToCustomer` already exists, skip (return `ServiceResult.Ok`).
3. For each of two invoice kinds:
   - Issue number via `IInvoiceNumberingService.IssueNumberAsync(issuerType, issuerId, year)` inside the same transaction.
   - Snapshot issuer legal info:
     - `IssuerType = Platform` → read from env (`PLATFORM_LEGAL_NAME`, etc.).
     - `IssuerType = Restaurant` → read from current `Restaurant` row.
   - Snapshot recipient (customer email + name, or restaurant legal block).
   - Build `InvoiceLine`s:
     - Customer invoice: one per `OrderItem`. Use `Dish.VatRate` (via `ToDecimal`). If `restaurant.IsVatRegistered == false`, every line has `VatRate = 0` and the PDF renders the VAT exemption clause.
     - Commission invoice: single line, `Description = "Commission plateforme sur commande #{orderId}"`, `Quantity = 1`, `UnitPriceTtc = RestaurantTransaction.CommissionAmount`, `VatRate = 20.0` if `PLATFORM_VAT_APPLICABLE == true`, else `0`.
   - Compute HT/VAT/TTC line totals and invoice totals (rounded to 2 decimals each line using banker's rounding).
   - Persist `Invoice` + `InvoiceLine`s with `Status = Queued`.
   - Publish `InvoiceJobMessage(invoiceId)`.
4. Commit transaction.

### 7.3 `IInvoiceService.CreateCreditNotesForRefundAsync(int refundId, CancellationToken ct)`

Called from `PaymentService.HandleChargeRefundedAsync` after the `Refund` row is upserted.

1. Load Refund + related Invoices (original pair) by `OrderId`. If originals missing (shouldn't happen — webhook ordering), log and skip.
2. For each of the two original invoices:
   - Compute ratio = `refund.Amount / originalInvoice.TotalTtc`. If full refund, ratio = 1.
   - Build the credit note:
     - `Kind = CreditNoteToCustomer` (mirrors `OrderInvoiceToCustomer`) or `CommissionCreditNoteToRestaurant` (mirrors `CommissionInvoiceToRestaurant`).
     - `RelatedInvoiceId = original.Id`.
     - Issue number via same counter (format `AV-{...}` prefix handled by `IInvoiceNumberingService`).
     - Lines: mirror original lines, multiply quantities by `-ratio` (negative), preserve unit prices and VAT rate. Recompute totals (will be negative).
     - Snapshots duplicated from original.
   - Persist + queue.

### 7.4 `InvoiceJobConsumer.ConsumeAsync` (worker)

1. Load `Invoice` with lines + snapshots.
2. Render PDF via `IInvoicePdfRenderer.RenderAsync(invoice)` → `byte[]`.
3. Upload to S3: key = `invoices/{YYYY}/{MM}/{Number}.pdf`.
4. Update `Invoice.StoragePath`, `Invoice.Status = Generated`.
5. Publish `EmailJobMessage` with template `InvoiceReadyCustomer` or `InvoiceReadyRestaurant`, recipient email from snapshot, and `AttachmentStoragePath = storageKey`.
6. Exception path: `Invoice.Status = Failed`, `FailureReason = ex.Message`; rethrow so RabbitMQ retries (consistent with existing email pipeline's resilience).

### 7.5 `IInvoicePdfRenderer` (worker)

QuestPDF-based, one class serving both invoices and credit notes. Layout:

- Header: platform/restaurant logo (if any), issuer legal block (`LegalName` / `LegalForm`, SIRET, VAT #, address).
- Invoice meta block: `Number`, `IssuedAt`, order reference, recipient block.
- Line table: `Description`, `Quantity`, `Unit HT`, `VAT rate`, `Total HT`, `Total TTC`.
- Totals footer: `Total HT`, `Total TVA`, `Total TTC`.
- If issuer is VAT-exempt (`IsVatRegistered=false` or `PLATFORM_VAT_APPLICABLE=false`): VAT columns collapse to a single clause "TVA non applicable, art. 293 B du CGI".
- Credit notes: "AVOIR — Réf. facture {RelatedInvoice.Number}" prominent. Negative totals shown with `-`.
- Payment terms clause: "Paiement : prélevé par Stripe, réf. {PaymentIntentId}, en date du {CapturedAt}".

### 7.6 Email attachment support (worker)

- `EmailJobMessage` gains optional `AttachmentStoragePath` and `AttachmentFilename` fields.
- `EmailJobConsumer`: when fields present, fetches blob via `IObjectStorageService.GetObjectAsync` and attaches to `MimeMessage` as `application/pdf`.

### 7.7 `IObjectStorageService` extension

Add overload (existing `UploadAsync(IFormFile, ...)` unchanged):

```csharp
Task<string> UploadAsync(byte[] content, string contentType, string folder, string fileName, CancellationToken ct = default);
```

## 8. Client UX

### 8.1 Customer

- **Order detail page** (existing): new "Télécharger la facture" button visible when `OrderInvoiceToCustomer` invoice exists with `Status=Generated`. Browser downloads via `GET /api/v1/invoice/{id}/pdf`.
- **New page** `/invoices` at `Pages/Invoices/MyInvoices/`:
  - Paginated table columns: Date, Numéro, N° commande, Montant TTC, Statut, Action.
  - Year filter.
  - Credit notes listed with "AVOIR" badge, link back to original invoice row.
  - Empty state: "Aucune facture pour le moment.".

### 8.2 Restaurant owner

- Existing restaurant dashboard (`Pages/Restaurant/RestaurantAccount/...`): add tab "Factures de commission".
- Table lists invoices where `RecipientRestaurantId == currentRestaurantId`.

### 8.3 Admin

- New pages:
  - `Pages/Admin/Invoices/AdminInvoices.razor` (list + filters: year, kind, issuerType, restaurantId, customerEmail substring).
  - `Pages/Admin/Invoices/AdminInvoiceDetail/AdminInvoiceDetail.razor` (full record, lines, legal snapshots, storage key, status, re-send email button).
- Re-send email: re-publishes `EmailJobMessage` with the same attachment path. No new PDF render; PDF is immutable.

### 8.4 Restaurant create/edit form

- New inputs: SIRET (text, Luhn-validated), Raison sociale, Adresse légale, Forme juridique (select: SAS, SARL, EURL, EI, SA), TVA applicable (checkbox, default checked).
- French error for invalid SIRET: "Le numéro SIRET est invalide.".

### 8.5 Dish create/edit form

- New input: "Taux de TVA" — select mapped from `VatRate` enum (`Intermediate10` labeled "10 %", etc.). Default `Intermediate10`.

## 9. Error handling

Add to `ErrorMessages.cs`:

```csharp
public const string SiretInvalid               = "Le numéro SIRET est invalide.";
public const string LegalFieldsRequired        = "Les informations légales (SIRET, raison sociale, adresse, forme juridique) sont obligatoires.";
public const string InvoiceNotFound            = "Facture introuvable.";
public const string InvoiceNotGeneratedYet     = "La facture est en cours de génération, réessayez dans quelques instants.";
public const string InvoiceGenerationFailed    = "Échec de la génération de la facture.";
public const string InvoiceAccessDenied        = "Vous n'êtes pas autorisé à consulter cette facture.";
public const string InvoiceCounterUnavailable  = "Impossible d'émettre un numéro de facture pour le moment.";
public const string DishVatRateRequired        = "Le taux de TVA du plat est obligatoire.";
```

## 10. Configuration & environment

### 10.1 `.env.example` additions

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

### 10.2 `AppEnvironment.cs` (server) and `WorkerEnvironment.cs`

Both load all six platform legal fields. Worker uses them in PDF rendering (for platform-issued invoices) and in legal snapshots.

### 10.3 Package references

- **`DeliverTableWorker.csproj`**: add `QuestPDF` (Community license).
- **Infrastructure / Server / Shared**: no new package refs.

## 11. Testing

### 11.1 Unit tests

| Target | Mocks | Key scenarios |
|---|---|---|
| `InvoiceNumberingService` | `DbContext` (real, in-memory with transaction semantics) | Concurrent issuance produces strictly-sequential non-overlapping numbers; new year restarts at 1; distinct entities don't share counters; `AV-` prefix on credit notes |
| `InvoiceService.CreatePendingInvoicesForCapturedOrderAsync` | repos + `IInvoiceNumberingService` + `IMessagePublisher` | Happy path queues 2 invoices; idempotent on replay (skips if already queued); VAT-exempt restaurant → 0% on all lines; platform exempt mode → 0% commission line; commission line computed from `RestaurantTransaction.CommissionAmount` |
| `InvoiceService.CreateCreditNotesForRefundAsync` | repos + publisher | Full refund mirrors exactly negative; partial refund prorates correctly; missing originals logged + ok result (no throw) |
| `InvoiceJobConsumer` | `IInvoiceService`, `IInvoicePdfRenderer`, `IObjectStorageService`, `IMessagePublisher` | Generates, uploads, sets `Generated`, publishes email; renderer exception → `Failed` with reason; upload exception → same |
| `InvoiceController` | `IInvoiceService` | Auth/ownership per endpoint; 409 when `Status != Generated`; admin scope includes all rows, customer scope excludes others' |
| `Admin.InvoicesController` | `IInvoiceService` | Filter combinations, pagination, detail fetch returns lines + snapshots, re-send email triggers publisher |
| `RestaurantService` (modified create/edit) | repo | SIRET Luhn validation rejects invalid, accepts valid; legal fields persist; error returned on empty |
| SIRET Luhn validator (pure utility) | n/a | Golden valid/invalid fixtures (valid: 73282932000074; invalid: 12345678900012 mistyped; 13-digit rejected; alpha rejected) |
| Email attachment wiring in `EmailJobConsumer` | `IObjectStorageService`, mail sender | When `AttachmentStoragePath` set, fetches blob + attaches |

### 11.2 Smoke test

`InvoicePdfRenderer` produces a non-zero byte array that parses as a valid PDF (via e.g. `PdfSharp`-based parser assertion or simple header check `%PDF-`). Visual QA in staging covers actual layout.

### 11.3 Not unit-tested (per CLAUDE.md)

Entity additions, enum additions, migrations, EF configs, DI registration, QuestPDF template internals beyond smoke test.

## 12. Security

1. **Access control on PDF download** — backend verifies the caller is the recipient (customer or restaurant owner) or Admin. Never returns PDFs by S3 key directly; stream through the endpoint.
2. **Tenant isolation** — restaurant-facing endpoints filter by `RecipientRestaurantId = { currentOwner's restaurants }`.
3. **Snapshot immutability** — `Invoice.IssuerLegalSnapshotJson` and `RecipientSnapshotJson` are set once; never updated. Even if the restaurant later changes its legal info, old invoices keep the original data.
4. **10-year retention** — no delete endpoint exists. Admin audit page has no delete button.
5. **SIRET Luhn validation** — server-side enforced (client-side is advisory only).

## 13. Rollout

1. Merge data model + enums + migration (nullable fields).
2. Ship `InvoiceNumberingService` + `IInvoiceService` + invoice entity generation (no UI, no workers yet).
3. Ship worker-side `IInvoicePdfRenderer`, `InvoiceJobConsumer`, email attachment support.
4. Ship server webhook hook; invoices start generating for new orders.
5. Admin backfills existing restaurants' SIRET / legal fields.
6. Ship customer + restaurant UX (download links, dashboard lists).
7. Tighten restaurant legal fields to NOT NULL (second migration).

No feature flag; capture-time trigger means new orders get invoices, old orders are untouched.

## 14. Commit plan (orienting the implementation plan)

1. `feat(shared): add VatRate, InvoiceKind, InvoiceIssuerType, InvoiceStatus enums`
2. `feat(server): add legal fields to Restaurant and VatRate to Dish`
3. `feat(server): add Invoice, InvoiceLine, InvoiceCounter entities with EF configs`
4. `feat(db): add migration AddInvoicing`
5. `docs(db): update ER diagram and data dictionary for invoicing`
6. `feat(server): add platform legal env vars and update AppEnvironment + WorkerEnvironment`
7. `feat(shared): add invoice DTOs and routes`
8. `feat(shared): add SIRET Luhn validator utility with tests`
9. `feat(infra): add IInvoiceRepository`
10. `feat(server): add IInvoiceNumberingService with tests`
11. `feat(server): add IInvoiceService create-pending-invoices with tests`
12. `feat(server): add IInvoiceService credit-note generation with tests`
13. `feat(server): wire invoice queue into PaymentService capture and refund webhooks with tests`
14. `feat(worker): add QuestPDF dependency and IInvoicePdfRenderer`
15. `feat(worker): extend IObjectStorageService with byte[] upload overload`
16. `feat(worker): add InvoiceJobConsumer with tests`
17. `feat(worker): extend EmailJobMessage with attachments and update email consumer`
18. `feat(worker): add invoice-ready email templates`
19. `feat(server): add InvoiceController with tests` (list + download)
20. `feat(server): add admin invoice endpoints with tests`
21. `feat(server): validate restaurant SIRET and legal fields on create/edit with tests`
22. `feat(client): add MyInvoices page`
23. `feat(client): add restaurant commission invoices tab`
24. `feat(client): add admin invoices list and detail pages`
25. `feat(client): add invoice download button to order detail page`
26. `feat(client): update restaurant form with legal fields`
27. `feat(client): update dish form with VAT rate dropdown`
28. `style: apply formatting fixes` (if any)

PBI / Task references (`AB#...`) per CLAUDE.md are filled in at commit time.

## 15. Assumptions and open questions

- QuestPDF Community license acceptable for current revenue (under €1M).
- `RestaurantTransaction.CommissionAmount` is the commission HT or TTC? Assumption: HT. If it's TTC, commission-line computation inverts: `UnitPriceHt = CommissionAmount`, `UnitPriceTtc = HT × (1 + 0.20)`. Implementation plan should verify against existing `RestaurantAccountService` logic.
- Existing `OrderItem.UnitPrice` is TTC (per Q7a). If that assumption is wrong, the HT/TTC split in lines is inverted.
- Email SMTP (`Hostinger`) supports PDF attachments up to their limits (typical 20-25 MB; PDFs are small).
- No historical backfill of invoices for pre-deployment orders.
