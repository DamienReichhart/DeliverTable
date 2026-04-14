# Disputes — Design Spec

**Status**: Approved (brainstorm phase). Ready for implementation plan.
**Scope**: Spec 3 of 3. Depends on Spec 1 (Stripe payments core). Optionally integrates with Spec 2 (invoices) via soft DI check.
**Date**: 2026-04-14

---

## 1. Goal

Track Stripe disputes (chargebacks) end-to-end: receive webhook events, persist the dispute, immediately reverse the restaurant's credit to protect the platform balance, re-credit on won disputes, notify admins (in-app + email) and restaurant owners (in-app + email), surface disputes in dedicated admin and restaurant UIs, and block refunds while a dispute is open. Evidence submission remains out-of-band in the Stripe dashboard.

## 2. In scope

- `Dispute` entity, repository, EF config, migration.
- Webhook handlers for `charge.dispute.created`, `charge.dispute.updated`, `charge.dispute.closed`.
- Automatic reversal of restaurant credit on dispute open; automatic restore on won.
- `NotificationType.Dispute` in-app notifications for admins + restaurant owner.
- Email alerts to `ADMIN_DISPUTE_EMAIL` and the restaurant owner (six French templates).
- Admin UI (`/admin/disputes` list + detail) with Stripe dashboard deep link.
- Restaurant UI (read-only "Litiges" tab).
- Refund guard on open disputes.
- `DisputeState` enum, extension of `TransactionType` and `NotificationType`.
- Optional Spec 2 integration: credit notes generated on Lost (behind soft DI check).

## 3. Out of scope

- Early fraud warnings (`charge.warning_*` events).
- Platform-side evidence submission UI (admins use the Stripe dashboard).
- Multiple simultaneous disputes on a single charge (rare Stripe scenario; first-write wins, subsequent events upsert).
- Automated dispute negotiation or AI-assisted evidence drafting.
- Balance-recovery automation from restaurants with negative balances (admin-initiated, out-of-band).

## 4. Architecture overview

```
[Stripe] charge.dispute.created
   ↓
[Server] StripeWebhookController
   └─ PaymentService.HandleStripeEventAsync (Spec 1)
       └─ dispatch to IDisputeService.HandleCreatedAsync
            ├─ Dispute row upsert by StripeDisputeId
            ├─ RestaurantTransaction(Type=DisputeReversal, -amount, balance updated)
            ├─ Notification × N (all admins + restaurant owner)
            └─ EmailJobMessage × 2 (admin DL, restaurant owner)

[Stripe] charge.dispute.updated  → refresh DueBy, payload snapshot
[Stripe] charge.dispute.closed
   ├─ status=won  → Dispute.State=Won, RestaurantTransaction(DisputeRestored, +amount)
   └─ status=lost → Dispute.State=Lost (no balance move)
                    + (Spec 2 dep, soft) IInvoiceService.CreateCreditNotesForDisputeLossAsync
```

## 5. Data model

### 5.1 New entity — `Dispute`

`DeliverTableInfrastructure/Models/Dispute.cs`:

| Field | Type | Notes |
|---|---|---|
| `Id` | int, PK | |
| `StripeDisputeId` | string(200) | unique, non-empty |
| `PaymentId` | int, FK → `Payment` | |
| `OrderId` | int, FK → `Order` | denormalized |
| `RestaurantId` | int, FK → `Restaurant` | denormalized |
| `Amount` | decimal(9,2) | disputed amount (may be partial) |
| `Currency` | string(3) | `"EUR"` |
| `ReasonCode` | string(60) | raw Stripe reason (`fraudulent`, `product_not_received`, etc.) |
| `State` | `DisputeState` enum | `Open`, `Won`, `Lost` |
| `DueBy` | DateTime? UTC | Stripe evidence deadline |
| `OpenedAt` | DateTime UTC | Stripe `created` timestamp |
| `ClosedAt` | DateTime? UTC | set on Won/Lost |
| `StripePayload` | string(8000) | last-known JSON snapshot (debug) |
| `CreatedAt` / `UpdatedAt` | DateTime UTC | |

Indexes: unique on `StripeDisputeId`; non-unique on `PaymentId`, `OrderId`, `RestaurantId`; composite (`RestaurantId`, `State`) for fast open-dispute lookups.

### 5.2 Enum changes

**New** — `DisputeState` (`DeliverTableSharedLibrary/Enums/DisputeState.cs`):
- `Open = 0` (Stripe `needs_response` or `under_review`)
- `Won = 1`
- `Lost = 2`

**Extend `NotificationType`**: add `Dispute = 100`.

**Extend `TransactionType`**: add `DisputeReversal = 100`, `DisputeRestored = 101`.

### 5.3 `RestaurantTransaction` usage (no schema change)

Two new types use existing fields:
- **`DisputeReversal`** — `GrossAmount = dispute.Amount`, `CommissionAmount = 0`, `NetAmount = -dispute.Amount`, `BalanceAfter` recalculated, `OrderId = dispute.OrderId`.
- **`DisputeRestored`** — mirror with `NetAmount = +dispute.Amount`.

### 5.4 Migration

Single migration `AddDisputes`:
- Creates `Dispute` table with indexes.
- No other schema changes (enum additions are int-compatible).

### 5.5 DB docs updates

- `docs/db/er-diagram.md` — `Dispute` with relationships to `Payment`, `Order`, `Restaurant`.
- `docs/db/data-dictionary.md` — entries for `Dispute`, `DisputeState`, extended `TransactionType` and `NotificationType`.

## 6. API contracts

### 6.1 New routes (`ApiRoutes.cs`)

```csharp
public static class Dispute
{
    public const string Base = "api/v1/dispute";
    public const string RestaurantListRoute = "restaurant/{id:int}";
}

// Extend ApiRoutes.Admin
public const string DisputesRoute = "disputes";
public const string DisputeByIdRoute = "disputes/{id:int}";
```

### 6.2 Endpoints

| Method + path | Auth | Purpose |
|---|---|---|
| `GET /api/v1/dispute/restaurant/{id}` | RestaurantOwner (must own) or Admin | Paginated `DisputeRowDto` list, read-only |
| `GET /api/v1/admin/disputes` | Admin | Filters (state, year, restaurantId), paginated `AdminDisputeRowDto` |
| `GET /api/v1/admin/disputes/{id}` | Admin | Full `AdminDisputeDetailDto` (linked txns + Stripe dashboard URL) |

No mutation endpoints from the platform — evidence submission is out-of-band.

### 6.3 DTOs (`DeliverTableSharedLibrary/Dtos/Dispute/`)

```csharp
public record DisputeRowDto(
    int Id, string StripeDisputeId, int OrderId,
    decimal Amount, string Currency, string ReasonCode,
    DisputeState State, DateTime OpenedAt, DateTime? ClosedAt, DateTime? DueBy);

public record AdminDisputeRowDto(
    int Id, string StripeDisputeId, int OrderId, int RestaurantId,
    string RestaurantName, string CustomerEmail,
    decimal Amount, string Currency, string ReasonCode,
    DisputeState State, DateTime OpenedAt, DateTime? ClosedAt, DateTime? DueBy);

public record AdminDisputeDetailDto(
    AdminDisputeRowDto Header,
    string StripeDashboardUrl,
    int PaymentId, string StripeChargeId,
    decimal PaymentAmount,
    List<RestaurantTransactionDto> LinkedTransactions);

public record DisputeAdminFilter(
    DisputeState? State, int? RestaurantId, int? Year,
    int Page = 1, int PageSize = 20);
```

`StripeDashboardUrl` format: `https://dashboard.stripe.com/{"test/" if test mode else ""}disputes/{StripeDisputeId}`.

## 7. Service logic

### 7.1 Webhook dispatch (extends Spec 1)

`PaymentService.HandleStripeEventAsync` (Spec 1) gains cases:

| Stripe event | Action |
|---|---|
| `charge.dispute.created` | `IDisputeService.HandleCreatedAsync(stripeDispute)` |
| `charge.dispute.updated` | `IDisputeService.HandleUpdatedAsync(stripeDispute)` |
| `charge.dispute.closed` | `IDisputeService.HandleClosedAsync(stripeDispute)` |
| `charge.dispute.funds_withdrawn` / `charge.dispute.funds_reinstated` | log + ack (already accounted for via created/closed) |
| `charge.warning_*` | log + ack (out of scope §3) |

Idempotency is guaranteed by Spec 1's `ProcessedStripeEvent`. Dispute handlers additionally upsert by `StripeDisputeId` for robustness.

### 7.2 `IDisputeService` interface

```csharp
public interface IDisputeService
{
    Task<ServiceResult<Dispute>> HandleCreatedAsync(Stripe.Dispute stripeDispute, CancellationToken ct);
    Task<ServiceResult> HandleUpdatedAsync(Stripe.Dispute stripeDispute, CancellationToken ct);
    Task<ServiceResult> HandleClosedAsync(Stripe.Dispute stripeDispute, CancellationToken ct);

    Task<bool> HasOpenDisputeForOrderAsync(int orderId, CancellationToken ct);

    Task<ServiceResult<PaginatedResult<AdminDisputeRowDto>>> ListForAdminAsync(
        DisputeAdminFilter filter, CancellationToken ct);

    Task<ServiceResult<PaginatedResult<DisputeRowDto>>> ListForRestaurantAsync(
        int restaurantId, int page, int pageSize, CancellationToken ct);

    Task<ServiceResult<AdminDisputeDetailDto>> GetAdminDetailAsync(int disputeId, CancellationToken ct);
}
```

### 7.3 `HandleCreatedAsync` logic

1. Upsert `Dispute` by `StripeDisputeId` (idempotent).
2. Locate `Payment` by `StripeChargeId`. Not found → log + `ServiceError.NotFound` (surfaces to admin via `Dispute` row with empty `PaymentId` — admin must investigate).
3. If a `DisputeReversal` transaction already exists for this `StripeDisputeId` (via `Dispute` lookup), skip balance work (idempotent).
4. Create `RestaurantTransaction`:
   - `Type = DisputeReversal`
   - `GrossAmount = dispute.Amount`
   - `CommissionAmount = 0`
   - `NetAmount = -dispute.Amount`
   - `BalanceAfter = restaurant.Balance - dispute.Amount`
   - `OrderId = order.Id`
5. Update `restaurant.Balance`.
6. Raise notifications (§7.6).

### 7.4 `HandleUpdatedAsync` logic

1. Load `Dispute` by `StripeDisputeId`. Missing → log + `ServiceError.NotFound`.
2. Refresh `DueBy`, `StripePayload`, `UpdatedAt`.
3. Do not change state unless Stripe reports `status` different than current.

### 7.5 `HandleClosedAsync` logic

1. Load `Dispute` by `StripeDisputeId`. Missing → log + `ServiceError.NotFound`.
2. Parse `stripeDispute.Status`:
   - `"won"` and current `State == Open`:
     - Set `Dispute.State = Won`, `ClosedAt = now`.
     - Create `RestaurantTransaction(Type = DisputeRestored, NetAmount = +dispute.Amount, BalanceAfter = restaurant.Balance + dispute.Amount)`.
     - Update `restaurant.Balance`.
   - `"lost"` and current `State == Open`:
     - Set `Dispute.State = Lost`, `ClosedAt = now`.
     - No balance change.
     - **Soft Spec 2 integration**: if `IInvoiceService` is registered in DI, call `CreateCreditNotesForDisputeLossAsync(disputeId, ct)` which mirrors the refund credit-note flow (Spec 2 §7.3) for the disputed amount. If not registered (Spec 2 not yet merged), skip silently.
3. Raise notifications.

### 7.6 Notifications

```csharp
// In DisputeService (pseudo)
var payload = JsonSerializer.Serialize(new {
    disputeId, stripeDisputeId, orderId, restaurantId,
    amount = dispute.Amount, reason = dispute.ReasonCode, state = dispute.State
});

await notifications.RaiseForAllAdminsAsync(NotificationType.Dispute, payload, ct);
await notifications.RaiseForUserAsync(restaurant.Owner.Id, NotificationType.Dispute, payload, ct);

// Emails via RabbitMQ to DeliverTableWorker
await publisher.PublishAsync("email", new EmailJobMessage {
    Template = eventTemplateName,       // e.g. DisputeOpenedAdmin
    ToEmail = env.AdminDisputeEmail,
    ... payload ...
});
await publisher.PublishAsync("email", new EmailJobMessage {
    Template = eventTemplateRestaurant, // e.g. DisputeOpenedRestaurant
    ToEmail = restaurant.Owner.Email,
    ... payload ...
});
```

`INotificationService.RaiseForAllAdminsAsync(type, payload, ct)` is the missing method; add if not already present. Existing `AdminNotificationService` may cover this — confirm at implementation time and reuse or extend.

### 7.7 Refund guard (extends Spec 1)

In `PaymentService.AdminRefundAsync` (Spec 1 §7.6), before calling `IStripeGateway.CreateRefundAsync`, call:

```csharp
if (await disputeService.HasOpenDisputeForOrderAsync(orderId, ct))
    return ServiceResult<RefundDto>.Fail(ErrorMessages.RefundBlockedByOpenDispute);
```

## 8. Client UX

### 8.1 Admin

**New page** `Pages/Admin/Disputes/`:

```
AdminDisputes/
├── AdminDisputes.razor
└── AdminDisputes.razor.scss
AdminDisputeDetail/
├── AdminDisputeDetail.razor
└── AdminDisputeDetail.razor.scss
```

**List**: columns Date (Opened), Stripe ID, Order #, Restaurant, Customer email, Montant, Motif, Échéance (DueBy), État badge. Filters: State (Open/Won/Lost/All), Year, Restaurant. Empty state: "Aucun litige à ce jour.".

**Detail view**: full `AdminDisputeDetailDto`. Prominent "Soumettre des preuves sur Stripe →" link to `StripeDashboardUrl` (external, new tab). `LinkedTransactions` table visualizes reversal/restore on the restaurant balance. Deadline countdown (e.g. "J-3") when Open.

**Admin dashboard** landing page gains an "Litiges ouverts" counter card (count of `State=Open`).

### 8.2 Restaurant owner

Existing restaurant dashboard (`Pages/Restaurant/RestaurantAccount/...`): add read-only "Litiges" tab. Columns: Date, Commande, Montant, Motif, État, Échéance. Banner message: "Nous gérons la défense du litige avec Stripe. Vous serez notifié du résultat.". No action buttons.

### 8.3 Notifications

`NotificationType.Dispute` in-app notifications:
- Admin title: "Nouveau litige sur la commande #{orderId}" (or Won/Lost variants).
- Restaurant title: "Un litige a été ouvert sur votre commande #{orderId}".
- Click action: navigate to dispute detail (admin) or restaurant dispute tab (restaurant owner).

### 8.4 Order detail page

For **admin viewers only**, if any `Dispute` exists for the order, render a "Litige en cours" banner at the top linking to the dispute detail page.

### 8.5 Email templates (worker)

Six Razor templates in `DeliverTableWorker/Templates/`, all in French:

| Template | Recipient | When |
|---|---|---|
| `DisputeOpenedAdmin` | `ADMIN_DISPUTE_EMAIL` | `charge.dispute.created` |
| `DisputeOpenedRestaurant` | Restaurant owner | `charge.dispute.created` |
| `DisputeWonAdmin` | `ADMIN_DISPUTE_EMAIL` | `charge.dispute.closed` / `won` |
| `DisputeWonRestaurant` | Restaurant owner | same |
| `DisputeLostAdmin` | `ADMIN_DISPUTE_EMAIL` | `charge.dispute.closed` / `lost` |
| `DisputeLostRestaurant` | Restaurant owner | same |

Admin emails include links to the admin detail page and the Stripe dashboard URL. Restaurant emails include a summary and the dispute amount; no external link.

## 9. Error handling

Add to `ErrorMessages.cs`:

```csharp
public const string DisputeNotFound                  = "Litige introuvable.";
public const string DisputeAccessDenied              = "Vous n'êtes pas autorisé à consulter ce litige.";
public const string RefundBlockedByOpenDispute       = "Impossible de rembourser : un litige est ouvert sur cette commande.";
public const string DisputePaymentNotFound           = "Aucun paiement correspondant à ce litige n'a été trouvé.";
```

## 10. Configuration

### 10.1 `.env.example` additions

```bash
# Email recipient for dispute alerts
ADMIN_DISPUTE_EMAIL=disputes@delivertable.example
```

### 10.2 `AppEnvironment.cs`

Add required string `AdminDisputeEmail`.

### 10.3 Stripe webhook subscription (production setup)

Ensure the production webhook endpoint subscribes to these events (in addition to Spec 1's events):
- `charge.dispute.created`
- `charge.dispute.updated`
- `charge.dispute.closed`

Subscription managed via Stripe Dashboard → Developers → Webhooks → (endpoint) → Events.

## 11. Testing

### 11.1 Unit tests

| Target | Mocks | Key scenarios |
|---|---|---|
| `DisputeService.HandleCreatedAsync` | `IDisputeRepository`, `IPaymentRepository`, `IRestaurantRepository` (balance), `INotificationService`, `IMessagePublisher` | Happy path persists dispute + reversal txn + notifications; duplicate `StripeDisputeId` → idempotent no-op; payment not found → `ServiceError`; partial-amount dispute reverses exactly `dispute.Amount` |
| `DisputeService.HandleClosedAsync` (Won) | same | Idempotent when already Won; creates restore txn; balance restored; notifications sent |
| `DisputeService.HandleClosedAsync` (Lost) | same + optional `IInvoiceService` mock | Idempotent when already Lost; no balance change; when `IInvoiceService` registered, credit-note generation is called; when not registered, skipped silently |
| `DisputeService.HandleUpdatedAsync` | repo | Refreshes `DueBy`, `StripePayload`, `UpdatedAt`; missing dispute → `ServiceError.NotFound` |
| `DisputeService.HasOpenDisputeForOrderAsync` | repo | True for `State=Open`, false for Won/Lost/none |
| `DisputeController` + admin endpoints | `IDisputeService` | Auth/ownership filters; pagination; detail includes linked txns and Stripe URL (test + live mode) |
| `PaymentService.AdminRefundAsync` (extended) | existing + `IDisputeService` | Rejects with `RefundBlockedByOpenDispute` when open dispute; happy path when no dispute |
| `StripeWebhookController` (dispute dispatch) | `IPaymentService`, `IStripeGateway` | Valid sig + dispute event routes to correct handler; warning events ack without dispatch |
| Notifications raising | `INotificationService`, publisher | Raises for all admins + restaurant owner; enqueues two emails per event (admin, restaurant) |

### 11.2 Not unit-tested (per CLAUDE.md)

Entity additions, enum additions, migrations, EF configs, DI registration, email template rendering (smoke test in staging).

## 12. Rollout

1. Merge data model + enums + migration.
2. Ship `DisputeService` + webhook handler extension + notification wiring.
3. Ship email templates in worker.
4. Ship refund guard extension in `PaymentService`.
5. Ship admin UI (list + detail).
6. Ship restaurant UI ("Litiges" tab).
7. Subscribe production Stripe webhook to dispute events.
8. Set `ADMIN_DISPUTE_EMAIL` in prod env.
9. Test end-to-end in staging with `stripe trigger charge.dispute.created`.

No feature flag — disputes are always-on once merged. Server fails startup if `ADMIN_DISPUTE_EMAIL` is missing (required env var).

## 13. Commit plan

1. `feat(shared): add DisputeState enum and extend NotificationType and TransactionType`
2. `feat(server): add Dispute entity with EF config`
3. `feat(db): add migration AddDisputes`
4. `docs(db): update ER diagram and data dictionary for disputes`
5. `feat(server): add ADMIN_DISPUTE_EMAIL env var and French dispute error messages`
6. `feat(shared): add dispute DTOs and routes`
7. `feat(infra): add IDisputeRepository`
8. `feat(server): add IDisputeService with created/updated/closed handlers and tests`
9. `feat(server): extend StripeWebhookController to dispatch dispute events with tests`
10. `feat(server): block admin refund when open dispute exists with tests`
11. `feat(server): extend INotificationService with admin bulk raise if missing`
12. `feat(server): add DisputeController and admin dispute endpoints with tests`
13. `feat(worker): add dispute email templates`
14. `feat(client): add restaurant Litiges tab`
15. `feat(client): add admin disputes list and detail pages`
16. `feat(client): add dispute banner to order detail for admin viewers`
17. `style: apply formatting fixes` (if any)

PBI / Task references per CLAUDE.md filled in at commit time.

## 14. Dependencies and assumptions

- **Spec 1 required**: Payment, StripeWebhookController, ProcessedStripeEvent, AdminRefundAsync must exist.
- **Spec 2 optional**: Lost disputes generate credit notes when `IInvoiceService` is DI-registered. If Spec 2 not merged yet, Lost still works (no credit note generated).
- `AdminNotificationService` may already expose a bulk-admin raise method; confirm at implementation and reuse or extend.
- Restaurant owner email is `Restaurant.Owner.Email` (navigation on `Restaurant.OwnerId → User`); assumed kept current via auth flow.
- Partial disputes handled by reversing `dispute.Amount` (not full payment). Tested.
- Multiple disputes on a single charge: rare Stripe scenario. Model supports it (one `Dispute` row per `StripeDisputeId`), but notifications fire per event without deduplication.
