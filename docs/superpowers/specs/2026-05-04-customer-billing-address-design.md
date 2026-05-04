# Customer Billing Address — Design

**Date:** 2026-05-04
**Status:** Approved (spec)
**Owner:** damien.reichhart@gmail.com

## Problem

Customer invoices issued by restaurants today use only the customer's name and email as the recipient block. The legal `Address` slot on the recipient snapshot is set to an empty string. For a French B2C invoice, the customer's postal address is part of the legally required recipient information; without it, invoices are technically incomplete.

The `User` entity has no address fields at all. The `Order.DeliveryAddress` is a free-text string captured per-order — it's a delivery concern, not a billing concern, and there's no concept of a persistent billing identity on the customer's profile.

## Goal

Customer invoices show a complete French-format recipient block (name + structured postal address). Customers manage their billing address from their profile page. New orders cannot be placed unless the customer has a complete billing address on file.

## Non-goals

- B2B customers — no `IsBusiness` flag, no SIRET / VAT / company name on `User`. The customer is always treated as an individual.
- Phone field on the user profile.
- Country dropdown / autocomplete — free-text input is sufficient.
- Backfill of existing customers' addresses or rewriting recipient snapshots on already-issued invoices.
- Auto-fill of `Order.DeliveryAddress` from billing address.
- Registration-form changes — registration stays minimal; the address gets filled later, prompted at checkout.
- Commission invoices (`BuildCommissionInvoice`) — still platform → restaurant, both fully populated.

## Design decisions (recap)

| # | Decision | Rationale |
|---|---|---|
| Q1 | B2C only | No business-account concept exists in the codebase; introducing one is out of scope |
| Q2 | Structured fields, no phone | Better UX, validatable per field, useful for analytics; phone is unrelated to invoicing |
| Q3 | Hard guard at checkout | Guarantees every new invoice is complete without breaking sign-up; existing customers fill in once on their next order |

## Architecture

```
User entity (5 new Billing* columns, all default '')
    │
    ├── BillingAddressHelper (pure static)
    │       ├── HasCompleteBillingAddress(User) → bool
    │       └── FormatBillingAddressForSnapshot(User) → string ("\n"-joined French postal block)
    │
    ├── AuthService.UpdateProfileAsync (writes the fields, trimmed)
    │
    ├── OrderService.CreateFromCartAsync (calls HasCompleteBillingAddress; ServiceError if not)
    │
    └── InvoiceService.BuildCustomerInvoice (calls FormatBillingAddressForSnapshot
                                              into recipient snapshot's Address field)
```

The `InvoiceLegalSnapshotDto` shape is unchanged — the `Address` field already exists and is rendered conditionally by the PDF renderer. The renderer treats `\n` as line breaks (QuestPDF default behavior), so a multi-line address renders as multiple stacked lines under the recipient name without any renderer change.

## Data model

### `User` entity — five new properties (`DeliverTableInfrastructure/Models/User.cs`)

```csharp
[MaxLength(200)]
public string BillingAddressLine1 { get; set; } = string.Empty;

[MaxLength(200)]
public string BillingAddressLine2 { get; set; } = string.Empty;

[MaxLength(10)]
public string BillingPostalCode { get; set; } = string.Empty;

[MaxLength(100)]
public string BillingCity { get; set; } = string.Empty;

[MaxLength(100)]
public string BillingCountry { get; set; } = string.Empty;
```

All default to `string.Empty`. No `[Required]` at the entity level — the requirement is enforced by the checkout guard, not by persistence. This matches how `Restaurant.LegalAddress` is `string.Empty` by default until populated through onboarding.

### EF migration `AddUserBillingAddress`

Adds the five columns to `AspNetUsers`, all `varchar(N)` `NOT NULL DEFAULT ''`. The default backfills existing rows; no separate data migration. Forward-only; `Down` drops the columns.

### "Complete" definition

A billing address is complete when **Line 1, PostalCode, City, and Country** are all non-empty after `Trim()`. Line 2 is always optional. This single definition lives in `BillingAddressHelper.HasCompleteBillingAddress`, used by both the checkout guard and (transitively) the invoice formatter.

## Profile UX

### DTO update — `DeliverTableSharedLibrary/Dtos/Auth/UpdateProfileRequest.cs`

Five new fields, each optional at the DTO level (allowing partial saves), with `[MaxLength]` matching the entity:

```csharp
[MaxLength(200)] public string BillingAddressLine1 { get; set; } = "";
[MaxLength(200)] public string BillingAddressLine2 { get; set; } = "";
[MaxLength(10)]  public string BillingPostalCode  { get; set; } = "";
[MaxLength(100)] public string BillingCity        { get; set; } = "";
[MaxLength(100)] public string BillingCountry     { get; set; } = "";
```

`ConnectionResponse` is updated only if it currently exposes profile data; if not, the client refetches.

### Service update — `AuthService.UpdateProfileAsync`

After the existing `FirstName`/`LastName`/`Email` mutations, copy the five new fields onto the `User` entity, applying `(value ?? "").Trim()` so leading/trailing whitespace is normalized once at the boundary.

### Profile page — `DeliverTableClient/Pages/Profile/Profile.razor`

A new "Adresse de facturation" section below the existing identity fields:

| Label (FR) | Field | Required* | Default / placeholder |
|---|---|---|---|
| Adresse | `BillingAddressLine1` | yes | "12 rue de la Paix" |
| Complément d'adresse (optionnel) | `BillingAddressLine2` | no | "Bât. B, 3ᵉ étage" |
| Code postal | `BillingPostalCode` | yes | "75002" |
| Ville | `BillingCity` | yes | "Paris" |
| Pays | `BillingCountry` | yes | defaults to "France" |

*Client-side soft check only — disables the submit button until the four required fields are non-empty. The DTO accepts partial values for staged saving.

A small banner above the section reads: *"Ces informations apparaissent sur vos factures et sont requises pour passer commande."*

Country defaults to "France" pre-filled on first load. Free-text input — no dropdown.

Registration form is unchanged.

## Checkout guard

### Where

In `OrderService.CreateFromCartAsync` (`DeliverTableServer/Services/OrderService.cs`), at the very top — before cart load, Stripe intent, and any DB writes:

```csharp
var user = await _userRepository.GetByIdAsync(customerId, ct);
if (user is null)
    return new ServiceError(ErrorMessages.UserNotFound, 404);

if (!BillingAddressHelper.HasCompleteBillingAddress(user))
    return new ServiceError(ErrorMessages.BillingAddressIncomplete);
```

### Helper

`DeliverTableServer/Common/BillingAddressHelper.cs` (alongside `ServiceResult` and other shared server helpers). Two static methods:

```csharp
public static bool HasCompleteBillingAddress(User user) =>
    !string.IsNullOrWhiteSpace(user.BillingAddressLine1)
    && !string.IsNullOrWhiteSpace(user.BillingPostalCode)
    && !string.IsNullOrWhiteSpace(user.BillingCity)
    && !string.IsNullOrWhiteSpace(user.BillingCountry);

public static string FormatBillingAddressForSnapshot(User user)
{
    var lines = new List<string>();
    if (!string.IsNullOrWhiteSpace(user.BillingAddressLine1))
        lines.Add(user.BillingAddressLine1.Trim());
    if (!string.IsNullOrWhiteSpace(user.BillingAddressLine2))
        lines.Add(user.BillingAddressLine2.Trim());

    var postalCity = string.Join(" ", new[]
    {
        user.BillingPostalCode?.Trim() ?? string.Empty,
        user.BillingCity?.Trim() ?? string.Empty,
    }.Where(s => !string.IsNullOrEmpty(s)));
    if (!string.IsNullOrEmpty(postalCity))
        lines.Add(postalCity);

    if (!string.IsNullOrWhiteSpace(user.BillingCountry))
        lines.Add(user.BillingCountry.Trim());

    return string.Join("\n", lines);
}
```

### New error messages — `DeliverTableServer/Constants/ErrorMessages.cs`

```csharp
public const string BillingAddressIncomplete =
    "Veuillez compléter votre adresse de facturation dans votre profil avant de commander.";
public const string UserNotFound = "Utilisateur introuvable.";
```

(`UserNotFound` may already exist; only add if not.)

### Client UX hook

The HTTP 400 carries the French error string. The customer-side checkout page already shows API errors; on top of that, when the response message equals `BillingAddressIncomplete`, render an actionable button labeled `Compléter mon profil` linking to `/profile`. Single string-comparison check — sufficient for one error path.

### Where the guard does NOT fire

- Profile saves with partial address values (allowed — Section "Profile UX").
- Restaurant-owner profile edits and admin operations (don't hit `OrderService.CreateFromCartAsync`).
- Pending orders created before deploy (continue along their existing flow; no retroactive validation).

## Invoice integration

### `BuildCustomerInvoice` — `DeliverTableServer/Services/InvoiceService.cs`

Replace the empty-address recipient snapshot with:

```csharp
var recipientSnapshot = new InvoiceLegalSnapshotDto(
    Name: $"{customer.FirstName} {customer.LastName}".Trim(),
    LegalForm: string.Empty,
    Siret: string.Empty,
    VatNumber: string.Empty,
    Address: BillingAddressHelper.FormatBillingAddressForSnapshot(customer),
    Email: customer.Email ?? string.Empty);
```

Snapshot DTO shape unchanged. The `Address` field, which used to hold an empty string, now holds either the formatted address (for orders created post-feature) or empty (legacy orders).

### PDF renderer — already works

The renderer (`DeliverTableWorker/Services/InvoicePdfRenderer.cs`, around lines 70–73) already conditionally renders `recipient.Address`. QuestPDF's `.Text()` honors `\n` as line breaks, so the multi-line address renders as stacked lines automatically.

If integration testing reveals QuestPDF doesn't honor `\n` in this context, fallback: extend `InvoiceLegalSnapshotDto` with separate address-line fields and render them one-per-line in the renderer. The smoke test in the testing section will surface this.

### Snapshot semantics

`Invoice.RecipientSnapshotJson` is frozen at issue time. If a customer later edits their profile address, **previously-issued invoices keep the old address.** This is the intended legal-document behavior — invoices record state at the moment of issue. New invoices use the new address.

### Credit notes

Credit notes already inherit `RecipientSnapshotJson` from the original invoice (existing behavior unchanged by this feature). They automatically carry whatever billing block was on the original.

## Testing

| Test | Layer | Covers |
|---|---|---|
| `BillingAddressHelperTests.HasCompleteBillingAddress_AllRequiredPresent_ReturnsTrue` | helper | Happy path |
| `BillingAddressHelperTests.HasCompleteBillingAddress_LineTwoEmpty_StillTrue` | helper | Line 2 is optional |
| `BillingAddressHelperTests.HasCompleteBillingAddress_AnyRequiredFieldBlank_ReturnsFalse` | helper | Each required field, parameterized |
| `BillingAddressHelperTests.FormatBillingAddressForSnapshot_FullAddress_ReturnsFourLines` | helper | Line 1 / Line 2 / `Postal City` / Country |
| `BillingAddressHelperTests.FormatBillingAddressForSnapshot_NoLineTwo_ReturnsThreeLines` | helper | Line 2 omitted cleanly |
| `BillingAddressHelperTests.FormatBillingAddressForSnapshot_AllEmpty_ReturnsEmptyString` | helper | Legacy order shape |
| `OrderServiceTests.CreateFromCart_WhenBillingAddressIncomplete_ReturnsBillingError` | service | Hard checkout guard fires |
| `OrderServiceTests.CreateFromCart_WhenBillingAddressComplete_Succeeds` | service | Existing happy path still works |
| `AuthServiceTests.UpdateProfile_WithBillingFields_PersistsThemTrimmed` | service | Profile saves the new fields and trims whitespace |
| `InvoiceServiceTests.BuildCustomerInvoice_WithCustomerAddress_PopulatesRecipientSnapshot` | service | Invoice carries the address; deserialized snapshot contains the formatted block |

Existing `OrderServiceTests` happy-path tests will need their order setup adjusted — the seed customer used today has no billing address. They must be updated to populate the four required fields, otherwise they'll regress on the new guard.

## Atomic commit plan

Each commit is independently buildable; tests for that layer pass before commit. Conventional Commits format, no `PBI:`/`Task:` footer (matches branch convention).

| # | Scope | Description |
|---|---|---|
| 1 | `feat(server)` | Add `Billing*` properties to `User` entity |
| 2 | `feat(db)` | Migration `AddUserBillingAddress` |
| 3 | `feat(server)` | `BillingAddressHelper` (`HasCompleteBillingAddress` + `FormatBillingAddressForSnapshot`) + helper tests |
| 4 | `feat(shared)` | Extend `UpdateProfileRequest` DTO with the five fields |
| 5 | `feat(server)` | `AuthService.UpdateProfileAsync` persists trimmed billing fields + service test; new error message constants |
| 6 | `feat(server)` | Checkout guard in `OrderService.CreateFromCartAsync` + service tests; existing happy-path tests updated to satisfy guard |
| 7 | `feat(server)` | `BuildCustomerInvoice` populates recipient snapshot via helper + service test |
| 8 | `feat(client)` | Profile page section + banner |
| 9 | `feat(client)` | Checkout error UX — detect `BillingAddressIncomplete` and surface "Compléter mon profil" link |
| 10 | `style` | `make format-fix` if anything drifts |

Pre-final-commit gate: `make format-check` and `make test` both green (ignoring the documented `AppEnvironmentTests` Docker leak).
