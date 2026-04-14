# Stripe Payments Core — Design Spec

**Status**: Approved (brainstorm phase). Ready for implementation plan.
**Scope**: Spec 1 of 3 (Payments Core). Invoices (Spec 2) and Dispute handling (Spec 3) are deferred.
**Date**: 2026-04-14
**Target Stripe API version**: `2026-03-25.dahlia`

---

## 1. Goal

Introduce a full Stripe integration so customers can pay for their orders with cards (including Apple Pay / Google Pay via Stripe Payment Element). The platform holds funds until the restaurant accepts the order, then captures. Restaurant payouts remain manual via the existing `RestaurantAccountService.WithdrawAsync` — no Stripe Connect in this spec.

## 2. In scope

- Customer → platform payments via Stripe Payment Element (authorize on checkout, capture on restaurant accept).
- `Payment`, `Refund`, `ProcessedStripeEvent` entities and related EF configs/migration.
- `IStripeGateway` abstraction (Stripe SDK wrapper), `IPaymentService`, `PaymentController`, `StripeWebhookController`.
- Reversible loyalty/discount-code redemptions.
- New order lifecycle states: `AwaitingPayment` (order status), `Authorized` and `PartiallyRefunded` (payment status).
- `DeliverTableScheduler` project: two background jobs (15-min abandonment sweep, 24h restaurant-response sweep).
- Blazor WASM checkout page using Stripe.js via `IStripeJsInterop` wrapper.
- Admin partial/full refund endpoint.
- Saved cards via Stripe Customer per user (`setup_future_usage = "off_session"`).
- Single currency: EUR.
- French error messages in `ErrorMessages.cs`.

## 3. Out of scope (deferred)

- **Invoice generation** (→ Spec 2): QuestPDF, sequential numbering, SIRET/VAT additions, storage in Garage S3, client dashboard.
- **Dispute handling** (→ Spec 3): `Dispute` entity, `charge.dispute.*` webhooks, admin notifications.
- Stripe Connect, automatic restaurant payouts, subscriptions, multi-currency.
- Customer self-service refund UI.
- Stripe-native receipt emails (branded emails come with Spec 2).

## 4. Architecture overview

```
Customer          Client (Blazor)         Server              Stripe                Scheduler
  │                    │                    │                  │                       │
  │—"Checkout"────────▶│                    │                  │                       │
  │                    │—POST /api/v1/order▶│                  │                       │
  │                    │                    │—CreatePI────────▶│                       │
  │                    │                    │                  │                       │
  │                    │                    │◀—clientSecret────│                       │
  │                    │◀{orderId,secret}———│                  │                       │
  │◀Payment Element UI─│                    │                  │                       │
  │—confirm───────────▶│—stripe.confirmPayment────────────────▶│                       │
  │                    │                    │◀webhook auth ok──│                       │
  │                    │                    │(amount_capturable│                       │
  │                    │                    │    _updated)     │                       │
  │                    │                    │                  │                       │
  │               [restaurant accepts via backend]             │                       │
  │                    │                    │—capture PI──────▶│                       │
  │                    │                    │                  │                       │
  │                    │                    │        [15min/24h sweep queries]─────────│
  │                    │                    │◀───cancel PI via IPaymentLifecycleService│
```

**Source of truth**: Stripe webhooks. Client responses are optimistic UI.

### Components

| Component | Layer | Home project |
|---|---|---|
| `IStripeGateway` + `StripeGateway` | Infrastructure (SDK boundary) | `DeliverTableInfrastructure/Payments/` |
| `IPaymentLifecycleService` + impl | Infrastructure (shared between server + scheduler) | `DeliverTableInfrastructure/Payments/` |
| `IPaymentService` + `PaymentService` | Server business logic | `DeliverTableServer/Services/` |
| `IPaymentRepository` + `PaymentRepository` | Data access (`Payment`, `Refund`, `ProcessedStripeEvent`) | `DeliverTableInfrastructure/Repositories/` |
| `PaymentController` | HTTP | `DeliverTableServer/Controllers/` |
| `StripeWebhookController` | HTTP | `DeliverTableServer/Controllers/` |
| `OrderAbandonmentSweep`, `OrderRestaurantTimeoutSweep` | Background jobs | `DeliverTableScheduler/Jobs/` |
| `IStripeJsInterop` + `StripeJsInterop` + `Checkout.razor.js` | Client | `DeliverTableClient/` |

## 5. Data model

### 5.1 New entities

**`Payment`** — already scaffolded at `DeliverTableInfrastructure/Models/Payment.cs`. No schema change beyond adding `List<Refund> Refunds` navigation.

**`Refund`** (new):

| Field | Type | Notes |
|---|---|---|
| `Id` | int, PK | identity |
| `PaymentId` | int, FK → `Payment(Id)` | required, cascade on Payment delete |
| `StripeRefundId` | string(200) | unique, non-empty |
| `Amount` | decimal(9,2) | |
| `Currency` | string(3) | default `"EUR"` |
| `Reason` | string(500) | admin note |
| `CreatedByUserId` | int?, FK → `User(Id)` | nullable (null = system-initiated) |
| `CreatedAt` | DateTime UTC | default `DateTime.UtcNow` |

**`ProcessedStripeEvent`** (new):

| Field | Type | Notes |
|---|---|---|
| `StripeEventId` | string(200), PK | unique |
| `EventType` | string(100) | |
| `ProcessedAt` | DateTime UTC | default `DateTime.UtcNow` |

### 5.2 Modified entities

- **`User`** — add `StripeCustomerId` string(200), nullable. Set on first purchase.
- **`Payment`** — navigation only: `public List<Refund> Refunds { get; set; } = new();`.
- **`LoyaltyTransaction`** — add `Status` enum column: `Pending | Committed | Reversed` (default `Committed` for existing rows via migration data fix).
- **`DiscountCodeRedemption`** — add `Status` enum column: `Pending | Committed | Reversed` (default `Committed` for existing rows).

### 5.3 Enum changes

- **`OrderStatus`** (`DeliverTableSharedLibrary/Enums/OrderStatus.cs`): add `AwaitingPayment = 100`. Do not renumber existing members.
- **`PaymentStatus`** (`DeliverTableSharedLibrary/Enums/PaymentStatus.cs`): add `Authorized = 100`, `PartiallyRefunded = 101`.
- **`LoyaltyRedemptionStatus`** (new): `Pending = 0`, `Committed = 1`, `Reversed = 2`.
- **`DiscountRedemptionStatus`** (new): same values. (Can be shared with loyalty if naming allows, but kept separate for clarity.)

### 5.4 Migration

Single migration `AddStripePaymentsCore`:

- Creates `Refund` table.
- Creates `ProcessedStripeEvent` table.
- Adds `StripeCustomerId` column to `User`.
- Adds `Status` column to `LoyaltyTransaction` and `DiscountCodeRedemption` (default `Committed` = 1 for historical rows).

### 5.5 DB docs to update

- `docs/db/er-diagram.md` — new entities `Refund`, `ProcessedStripeEvent`; new columns; new relationships.
- `docs/db/data-dictionary.md` — entries for new entities, columns, and enum values.

## 6. API contracts

### 6.1 New routes (add to `ApiRoutes.cs`)

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

Extend `ApiRoutes.Admin`:

```csharp
public const string OrderRefundRoute = "orders/{id:int}/refund";
```

### 6.2 Modified endpoint — `POST /api/v1/order`

Request body: unchanged (`CreateOrderRequest`).
Response changes from `OrderDto` to `CreateOrderResponse`:

```csharp
public record CreateOrderResponse(
    int OrderId,
    string ClientSecret,
    string PublishableKey,
    decimal Amount,     // in euros (decimal), human-facing
    string Currency);   // always "eur" in v1
```

### 6.3 New endpoint — `POST /api/v1/payment/{orderId}/cancel`

Authorize: customer. Ownership verified (`order.CustomerId == currentUser.Id`).
Used if the customer dismisses the checkout page. Returns `204` on success.

### 6.4 New endpoint — `POST /api/v1/admin/orders/{id}/refund`

Authorize: `Administrator` role.
Request:

```csharp
public record AdminRefundRequest(decimal Amount, string Reason);
```

Response: `RefundDto`. Validation:

- Order must exist.
- `Payment.Status == Succeeded` (captured).
- `Amount > 0` and `Amount ≤ (Payment.Amount - sum(existing refunds))`.
- `Reason` non-empty.

### 6.5 New endpoint — `POST /api/v1/stripe/webhook`

`[AllowAnonymous]`. Raw body preserved for signature verification.

Handled event types (v1):

| Event type | Action |
|---|---|
| `payment_intent.amount_capturable_updated` | Authorization succeeded on a `capture_method=manual` PI — mark payment `Authorized`, order `AwaitingPayment → Pending`, commit loyalty/discount redemptions, clear customer's cart, publish confirmation email job via `DeliverTableWorker` |
| `payment_intent.succeeded` | Fires after capture — mark payment `Completed`, no order state change (already `Confirmed` or later) |
| `payment_intent.payment_failed` | Mark payment `Failed`, order → `Cancelled`, reverse loyalty/discount |
| `payment_intent.canceled` | Mark payment `Canceled`, order → `Cancelled` (if not already) |
| `charge.refunded` | Upsert `Refund` row by `StripeRefundId`, recompute cumulative refund total, update order `PaymentStatus` to `PartiallyRefunded` or `Refunded` |

All other event types: log + return 200 (acknowledge).

**Note on `succeeded` vs `amount_capturable_updated`**: for manual-capture PaymentIntents, Stripe emits `amount_capturable_updated` when the authorization completes (funds reserved, not captured). `succeeded` fires only after a successful capture. The handler correctly distinguishes the two events and transitions different state accordingly.

### 6.6 Modified DTOs

- `OrderDto` — add `PaymentGatewayStatus? GatewayStatus`, `List<RefundDto> Refunds`, `decimal TotalRefunded`.
- New `RefundDto(int Id, decimal Amount, string Currency, string Reason, DateTime CreatedAt)`.

## 7. Service logic & state machine

### 7.1 Order lifecycle (updated)

```
[AwaitingPayment]
  ├─ webhook: payment_intent.amount_capturable_updated → [Pending]    (auth complete; loyalty/discount committed, cart cleared)
  ├─ webhook: payment_intent.payment_failed → [Cancelled]  (loyalty/discount reversed)
  ├─ webhook: payment_intent.canceled → [Cancelled]
  ├─ scheduler: AwaitingPayment > 15min → [Cancelled]   (PI cancelled, loyalty/discount reversed)

[Pending]
  ├─ restaurant accepts → capture PI → [Confirmed]
  ├─ restaurant refuses → cancel PI → [Refused]
  ├─ scheduler: Pending > 24h → cancel PI → [Refused]

[Confirmed → Preparing → Ready → Delivering → Delivered]
  ├─ any cancellation before Delivered → refund captured amount → [Cancelled]
  └─ delivered: RestaurantTransaction credit (unchanged)
```

### 7.2 `OrderService.CreateFromCartAsync` (modified)

1. Compute totals (existing logic).
2. Create order in `AwaitingPayment` with `PaymentStatus.Pending`.
3. Apply loyalty/discount as `Pending` (not committed).
4. Call `IPaymentService.CreateIntentAsync(order)` → returns client secret.
5. **Do NOT clear cart here** (cart cleared only on `payment_intent.amount_capturable_updated` webhook, i.e. when authorization succeeds).
6. **Do NOT enqueue confirmation emails here** (moved to webhook handler — emails are sent only after payment authorization succeeds).
7. Return `CreateOrderResponse`.

### 7.3 `PaymentService.CreateIntentAsync(Order order)` (new)

1. Get or create `Stripe.Customer` via `IStripeGateway.CreateCustomerAsync(user)`:
   - If `user.StripeCustomerId` is null, create using `user.Email` and `user.FirstName + " " + user.LastName`, persist `StripeCustomerId` on the `User` row.
   - Otherwise reuse the existing id.
2. Create `PaymentIntent` with:
   - `amount = (long)Math.Round(order.TotalAmount * 100m, MidpointRounding.AwayFromZero)` — integer minor units (cents).
   - `currency = "eur"`
   - `customer = user.StripeCustomerId`
   - `capture_method = "manual"`
   - `setup_future_usage = "off_session"`
   - `metadata = { orderId, userId, restaurantId }`
   - `automatic_payment_methods = { enabled = true }`
   - Idempotency key: `"order:{orderId}:create-intent"`
3. Persist `Payment` row (`Amount = order.TotalAmount` stored as decimal euros, not cents).
4. Return `client_secret`.

### 7.4 `PaymentService.HandleStripeEventAsync(Event evt)` (new)

1. `IPaymentRepository.TryRegisterProcessedEventAsync(evt.Id, evt.Type)`. If `false` (already processed), return (idempotent no-op).
2. Dispatch on `evt.Type` to:
   - `HandleAuthorizationCompletedAsync` (on `payment_intent.amount_capturable_updated`) — locate Payment by PaymentIntent id, mark `Authorized`, transition Order `AwaitingPayment → Pending`, commit loyalty + discount redemptions, clear cart, enqueue confirmation email via RabbitMQ to `DeliverTableWorker`.
   - `HandleCaptureCompletedAsync` (on `payment_intent.succeeded`) — locate Payment, mark `Completed`, update `Payment.CapturedAt`. No order state change (order already `Confirmed` when we triggered the capture; further transitions happen via restaurant actions).
   - `HandlePaymentFailedAsync` / `HandlePaymentCanceledAsync` — reverse loyalty/discount, set Order to `Cancelled` (only if current status is `AwaitingPayment`), set Payment to `Failed`/`Canceled`.
   - `HandleChargeRefundedAsync` — upsert Refund row by `StripeRefundId`, recompute cumulative refund total, update `Order.PaymentStatus`.

### 7.5 `OrderService.UpdateStatusAsync` (modified)

- `Pending → Confirmed`: call `IPaymentService.CaptureAsync(orderId)` first. If gateway fails, return `ServiceError`, keep order in `Pending`.
- `Pending → Refused`: call `IPaymentService.CancelAuthorizationAsync(orderId)` (releases the auth with `CancelPaymentIntent`).
- `Confirmed | Preparing | Ready | Delivering → Cancelled`: call `IPaymentService.RefundAsync(orderId, fullAmount, reason: "order_cancelled", adminUserId: null)`.

### 7.6 `PaymentService.AdminRefundAsync(orderId, amount, reason, adminUserId)` (new)

1. Validate: order exists, payment captured, amount ≤ refundable remaining, reason non-empty.
2. Idempotency key: `"order:{orderId}:refund:{timestampTicks}"`.
3. Create Stripe refund via `IStripeGateway.CreateRefundAsync`.
4. Persist `Refund` row.
5. Update `Order.PaymentStatus`:
   - if cumulative refunds == payment amount → `Refunded`
   - else → `PartiallyRefunded`
6. Return `RefundDto`.

**Idempotency with webhook**: Stripe's `charge.refunded` webhook will also fire for the same refund. `HandleChargeRefundedAsync` uses `StripeRefundId` as an upsert key — if a Refund row with that id already exists, the handler updates timestamps but never duplicates. Thus direct API → DB write and webhook → DB write converge to the same final state.

### 7.7 `IPaymentLifecycleService` (infra, shared with scheduler)

```csharp
public interface IPaymentLifecycleService
{
    Task<ServiceResult> CancelAbandonedOrderAsync(int orderId, CancellationToken ct);
    Task<ServiceResult> AutoRefuseOrderAsync(int orderId, CancellationToken ct);
}
```

Implementation uses `IStripeGateway` + repositories directly; does not depend on any `DeliverTableServer` service. `DeliverTableServer.PaymentService` delegates to this service for the same transitions.

### 7.8 New repository methods

- `IOrderRepository.GetOrdersOlderThanAsync(OrderStatus status, DateTime threshold, CancellationToken ct)` — returns orders whose `CreatedAt < threshold` and whose current `Status == status`. Used by both scheduler jobs.
- `IPaymentRepository.GetByOrderIdAsync(int orderId, CancellationToken ct)` — returns the Payment for an order (one-to-one in practice; one-to-many only for future multi-payment edge cases).
- `IPaymentRepository.GetByStripePaymentIntentIdAsync(string piId, CancellationToken ct)` — used by webhook handlers to locate the Payment.
- `IPaymentRepository.UpsertRefundByStripeIdAsync(Refund refund, CancellationToken ct)` — idempotent refund row upsert.
- `IPaymentRepository.TryRegisterProcessedEventAsync(string stripeEventId, string eventType, CancellationToken ct)` — returns `true` if inserted, `false` if already seen (idempotency guard).

## 8. `DeliverTableScheduler` project

**New project**: `DeliverTableScheduler/DeliverTableScheduler.csproj` with `<Project Sdk="Microsoft.NET.Sdk.Worker">`. Mirrors `DeliverTableWorker` structure.

### 8.1 Structure

```
DeliverTableScheduler/
├── DeliverTableScheduler.csproj
├── Dockerfile
├── Program.cs
├── Configuration/
│   └── SchedulerEnvironment.cs
└── Jobs/
    ├── OrderAbandonmentSweep.cs       (every 60s; cancel AwaitingPayment > 15min)
    └── OrderRestaurantTimeoutSweep.cs (every 60s; auto-refuse Pending > 24h)
```

### 8.2 Package references

- `DotNetEnv`
- `Microsoft.EntityFrameworkCore.Design`, `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Stripe.net`

### 8.3 Project references

- `DeliverTableInfrastructure`
- `DeliverTableSharedLibrary`

### 8.4 `SchedulerEnvironment` fields

- `ConnectionStringDatabase` (required)
- `StripeSecretKey` (required)

### 8.5 DI and Program.cs

```csharp
DotNetEnv.Env.Load();
var env = SchedulerEnvironment.Load();
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(env);
builder.Services.AddDbContext<DeliverTableContext>(opts => opts.UseNpgsql(env.ConnectionStringDatabase));

StripeConfiguration.ApiKey = env.StripeSecretKey;
StripeConfiguration.ApiVersion = "2026-03-25.dahlia";
builder.Services.AddSingleton<IStripeGateway, StripeGateway>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();
builder.Services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();
builder.Services.AddScoped<IPaymentLifecycleService, PaymentLifecycleService>();

builder.Services.AddHostedService<OrderAbandonmentSweep>();
builder.Services.AddHostedService<OrderRestaurantTimeoutSweep>();

await builder.Build().RunAsync();
```

### 8.6 Job pattern (shared)

Both jobs follow the same `BackgroundService` shape; they differ only in the `(status, threshold)` pair they query and the `IPaymentLifecycleService` method they invoke:

| Job | Query | Lifecycle call |
|---|---|---|
| `OrderAbandonmentSweep` | `status = AwaitingPayment`, `threshold = now - 15 min` | `CancelAbandonedOrderAsync` |
| `OrderRestaurantTimeoutSweep` | `status = Pending`, `threshold = now - 24 h` | `AutoRefuseOrderAsync` |

Skeleton:

```csharp
protected override async Task ExecuteAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        try { await RunTickAsync(ct); }
        catch (Exception ex) { logger.LogError(ex, "Sweep tick failed"); }
        await Task.Delay(TimeSpan.FromSeconds(60), ct);
    }
}

// abstract members differ per job
protected abstract OrderStatus TargetStatus { get; }
protected abstract TimeSpan Threshold { get; }
protected abstract Task InvokeLifecycleAsync(IPaymentLifecycleService svc, int orderId, CancellationToken ct);

private async Task RunTickAsync(CancellationToken ct)
{
    using var scope = scopeFactory.CreateScope();
    var lifecycle = scope.ServiceProvider.GetRequiredService<IPaymentLifecycleService>();
    var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
    var stale = await repo.GetOrdersOlderThanAsync(TargetStatus, DateTime.UtcNow - Threshold, ct);
    foreach (var order in stale)
    {
        try { await InvokeLifecycleAsync(lifecycle, order.Id, ct); }
        catch (Exception ex) { logger.LogError(ex, "Failed sweep for order {Id}", order.Id); }
    }
}
```

A shared base class `PeriodicSweepJob : BackgroundService` keeps both concrete jobs to ~5 lines of overrides each.

### 8.7 Docker

**`DeliverTableScheduler/Dockerfile`** — mirrors `DeliverTableWorker/Dockerfile`.

**`docker-dev.yaml`** — add service:

```yaml
scheduler:
  build:
    context: .
    dockerfile: DeliverTableScheduler/Dockerfile
  env_file: .env
  networks: [dt-private-net]
  depends_on: [db]
```

Same addition in `docker-prod.yaml`.

### 8.8 Solution

Add `DeliverTableScheduler/DeliverTableScheduler.csproj` and `DeliverTableSchedulerTests/DeliverTableSchedulerTests.csproj` to `DeliverTable.sln`.

## 9. Client (Blazor WASM)

### 9.1 New folders

```
DeliverTableClient/
├── Pages/Checkout/
│   ├── Checkout/
│   │   ├── Checkout.razor
│   │   ├── Checkout.razor.scss
│   │   └── Checkout.razor.js
│   └── CheckoutResult/
│       ├── CheckoutResult.razor
│       └── CheckoutResult.razor.scss
└── Services/Payment/
    ├── IPaymentApiClient.cs
    ├── PaymentApiClient.cs
    ├── IStripeJsInterop.cs
    └── StripeJsInterop.cs
```

### 9.2 `IStripeJsInterop`

```csharp
public interface IStripeJsInterop : IAsyncDisposable
{
    Task InitializeAsync(string publishableKey);
    Task MountPaymentElementAsync(string clientSecret, string domElementId);
    Task<StripeConfirmResult> ConfirmPaymentAsync(string returnUrl);
    Task UnmountAsync();
}

public record StripeConfirmResult(bool Succeeded, string? ErrorMessage, string? PaymentIntentId);
```

### 9.3 Stripe.js loading

In `DeliverTableClient/wwwroot/index.html` `<head>`:

```html
<script src="https://js.stripe.com/v3/"></script>
```

### 9.4 Flow

1. Cart page "Commander" button navigates to `/checkout`.
2. `Checkout.razor`:
   - `OnInitializedAsync`: `POST /api/v1/order` via `IPaymentApiClient` → `{ orderId, clientSecret, publishableKey, amount, currency }`.
   - `OnAfterRenderAsync` (first render): `stripeJs.InitializeAsync(publishableKey)` then `MountPaymentElementAsync(clientSecret, "payment-element")`.
   - On "Payer" click: `stripeJs.ConfirmPaymentAsync(returnUrl: "{baseUrl}/checkout/result?orderId={orderId}")`.
3. `CheckoutResult.razor`:
   - Reads `orderId` from query string.
   - Polls `GET /api/v1/order/{id}` every 1s for up to 10s, waiting for `Status != AwaitingPayment`.
   - `Pending` → success view with link to order details.
   - `Cancelled` or timeout → French error + "Réessayer" button → `/checkout`.

### 9.5 DI registration (client)

```csharp
builder.Services.AddScoped<IPaymentApiClient, PaymentApiClient>();
builder.Services.AddScoped<IStripeJsInterop, StripeJsInterop>();
```

## 10. Error handling

### 10.1 French error messages (add to `ErrorMessages.cs`)

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

### 10.2 Exception strategy

- `StripeGateway` catches all `StripeException` types and returns `ServiceResult<T>` via mapper (never throws above the gateway boundary).
- Services and controllers use the existing `ServiceResult` pattern end to end.

## 11. Security

1. **Webhook signature verification** — mandatory. 400 on invalid.
2. **No secrets to the client** — only `STRIPE_PUBLISHABLE_KEY` leaves the backend, via `CreateOrderResponse`.
3. **Ownership checks** — cancel endpoint verifies `order.CustomerId == currentUser.Id`.
4. **Admin refund** — `[Authorize(Roles = Administrator)]`.
5. **Idempotency keys** on every Stripe write (`"order:{id}:{action}"` or `"order:{id}:refund:{timestampTicks}"`).
6. **Raw body preservation** — webhook controller reads raw body (stream rewind or `EnableBuffering`) for signature check.
7. **Metadata hygiene** — only `orderId`, `userId`, `restaurantId`. No PII.
8. **HTTPS** — existing Cloudflare tunnel in prod.

## 12. Testing

Per CLAUDE.md TDD discipline.

### 12.1 Unit tests

| Target | Mocks | Key scenarios |
|---|---|---|
| `PaymentService` | `IStripeGateway`, `IPaymentRepository`, `IOrderRepository`, `ILoyaltyRepository`, `IDiscountCodeRepository` | Happy path create/capture/refund, gateway failure, double-capture guard, refund overflow, idempotent event replay, unknown event type |
| `PaymentController` | `IPaymentService` | `ServiceResult` → `IActionResult` mapping |
| `StripeWebhookController` | `IPaymentService`, `IStripeGateway` | Valid signature dispatches, invalid returns 400, unknown event acks 200 |
| `OrderService` (modified methods) | existing + `IPaymentService` | Capture on Confirmed, cancel on Refused, refund on Cancelled-after-capture, capture failure keeps order Pending |
| `PaymentLifecycleService` (infra) | `IStripeGateway`, repositories | Abandoned order cancels PI + reverses loyalty/discount; auto-refuse releases auth |
| `OrderAbandonmentSweep` (scheduler) | `IPaymentLifecycleService`, `IOrderRepository`, clock | Picks > 15min, skips fresh, continues on per-item errors |
| `OrderRestaurantTimeoutSweep` (scheduler) | same | Picks `Pending > 24h`, skips others |
| `AdminController.RefundAsync` | `IPaymentService` | Admin-only, validates amount, returns `RefundDto` |

### 12.2 Integration-style test

One end-to-end webhook test using a recorded Stripe event payload + fake signature validator (via `IStripeGateway` substitute), running against in-memory `TestDatabase` fixture to verify full dispatch chain.

### 12.3 New test project

`DeliverTableSchedulerTests/` — mirrors `DeliverTableTests/` structure. Uses NUnit + NSubstitute + `TestDatabase` fixture. Added to `DeliverTable.sln` and to the `make test` target.

### 12.4 Not unit-tested (per CLAUDE.md)

Entity additions, enum additions, mappers, EF configs, migrations, DI registration, `StripeGateway` implementation itself (external boundary).

## 13. Configuration & deployment

### 13.1 `.env.example` additions

```bash
# ─────────────────────────────────────────────────
# Stripe
# ─────────────────────────────────────────────────
STRIPE_PUBLISHABLE_KEY=pk_test_1234567890
STRIPE_SECRET_KEY=sk_test_1234567890
# Signing secret for webhook signature verification
# Dev: `stripe listen --print-secret`
# Prod: Stripe Dashboard → Developers → Webhooks → endpoint → Signing secret
STRIPE_WEBHOOK_SECRET=whsec_1234567890
```

### 13.2 `AppEnvironment.cs` (server)

Add three required string fields: `StripePublishableKey`, `StripeSecretKey`, `StripeWebhookSecret`.

### 13.3 Stripe CLI dev workflow

Add to `README.md`:

```bash
stripe login
stripe listen --forward-to http://localhost:5268/api/v1/stripe/webhook
# copy whsec_... into STRIPE_WEBHOOK_SECRET in .env
```

### 13.4 Rollout order

1. Merge data-model + enum + migration (no behavior change).
2. Merge `IStripeGateway` + `PaymentService` + webhook endpoint.
3. Merge `DeliverTableScheduler` + Docker wiring.
4. Merge Blazor checkout page + cart navigation update.
5. QA in staging against Stripe test mode (cards incl. 3DS, declines, abandonment, restaurant timeout).
6. Swap live keys in prod.

No feature flag — new checkout URL is a distinct route from the old direct-order path.

## 14. Commit plan (guides the implementation plan)

Ordered commits per CLAUDE.md commit strategy. Each commit builds and has its own tests where CLAUDE.md requires.

1. `feat(shared): add AwaitingPayment, Authorized, PartiallyRefunded enum values` — plus new `LoyaltyRedemptionStatus` / `DiscountRedemptionStatus` enums.
2. `feat(server): add Refund and ProcessedStripeEvent entities` + EF configs.
3. `feat(server): add StripeCustomerId to User and Status to loyalty/discount redemptions`.
4. `feat(db): add migration AddStripePaymentsCore`.
5. `docs(db): update ER diagram and data dictionary for Stripe payments core`.
6. `feat(server): add Stripe configuration and French error messages` — env vars, `AppEnvironment`, `ErrorMessages` entries, Stripe SDK init.
7. `feat(shared): add Stripe payment DTOs and routes` — `CreateOrderResponse`, `AdminRefundRequest`, `RefundDto`, routes.
8. `feat(infra): add IStripeGateway with Stripe.net wrapper`.
9. `feat(infra): add IPaymentRepository with tests-less data access` (repo has no logic).
10. `feat(infra): add IPaymentLifecycleService with tests` — shared cancel/auto-refuse logic with reversal semantics.
11. `feat(server): add IPaymentService with tests` — CreateIntent, Capture, CancelAuth, Refund, HandleStripeEvent.
12. `feat(server): add PaymentController with tests` — cancel endpoint.
13. `feat(server): add StripeWebhookController with tests` — signature verification + dispatch.
14. `feat(server): wire payment into OrderService with tests` — CreateFromCart returns client secret, UpdateStatus hooks capture/cancel/refund.
15. `feat(server): add admin refund endpoint with tests` — AdminController extension.
16. `feat(server): register Stripe services in DI`.
17. `feat(scheduler): add DeliverTableScheduler project scaffold`.
18. `feat(scheduler): add OrderAbandonmentSweep with tests`.
19. `feat(scheduler): add OrderRestaurantTimeoutSweep with tests`.
20. `feat(docker): add scheduler to dev and prod compose files`.
21. `feat(client): add IStripeJsInterop wrapper and Stripe.js loading`.
22. `feat(client): add Checkout page`.
23. `feat(client): add CheckoutResult page with polling`.
24. `feat(client): redirect cart to checkout instead of direct order`.
25. `docs(server): document Stripe CLI dev workflow in README`.
26. `style: apply formatting fixes` (if any).

PBI/Task references (`AB#...`) per CLAUDE.md must be filled in by the user at commit time.

## 15. Assumptions and open questions

None blocking. Deferred items (invoices, disputes, Connect) have their own specs. Stripe API version pinned to `2026-03-25.dahlia`; `Stripe.net` package version pick deferred to implementation plan (must target this API version or later).
