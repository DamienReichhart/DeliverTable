# Restaurant Owner Registration With First Restaurant — Design

## Context

Today, becoming a restaurant owner on DeliverTable is a two-step user journey:

1. Register at `/owner/register` (`POST /api/v1/auth/restaurant/register`) — creates a `User` + `RestaurantOwner` with `CompanyName`, `VatNumber`, `ContactPhoneNumber`.
2. Log in, navigate to `/restaurant/create` (`POST /api/v1/restaurant`) — creates the actual `Restaurant` (legal data, address, type, etc.) linked to the owner.

Between the two steps the system holds a "restaurant owner with no restaurants" state, which has no useful product purpose. We want a single combined onboarding so that creating a restaurant-owner account always materializes a first restaurant atomically.

## Goals

- A new restaurant owner provides their personal details and their first restaurant's details in one flow, with one server submission.
- If restaurant validation fails (SIRET, geocoding), no user account is created — all-or-nothing.
- Restaurant owner adding *additional* restaurants later continues to use the existing standalone create-restaurant flow.
- Move company identity (`CompanyName`, `VatNumber`) off `RestaurantOwner` and onto `Restaurant`, where it belongs (a single owner can hold multiple restaurants under different SIRETs / VAT numbers).

## Non-goals

- No changes to the customer registration flow.
- No changes to login, password reset, or session management.
- No automatic seed data (menus, opening hours, etc.) for the first restaurant — only the `Restaurant` row.

## Decisions

| # | Decision | Rationale |
|---|---|---|
| 1 | Single API call, multi-step **wizard** UX (2 steps) | ~13 fields total — too many for one screen, but logically two distinct concepts (owner identity vs. restaurant). Single submit keeps server logic atomic. |
| 2 | **All-or-nothing** atomicity: if restaurant validation fails, the user is not created | Avoids dangling owner accounts; matches the user's framing ("create the first restaurant *with* it"). |
| 3 | Drop `CompanyName` and `VatNumber` from `RestaurantOwner`. Add `VatNumber` (nullable) to `Restaurant`. `LegalName` already covers company name on `Restaurant`. | A single owner may hold multiple restaurants under different legal entities; per-restaurant fields fit better. |
| 4 | `Restaurant.VatNumber` is **conditionally required** at the service layer when `IsVatRegistered = true` | Internally consistent: if not VAT-registered, no VAT number is expected. |
| 5 | The combined endpoint reuses URL `POST /api/v1/auth/restaurant/register` with a restructured request body | This endpoint had only one client; the change is acceptable since the project is pre-prod. |

## Schema changes

### `RestaurantOwner` (entity)

Drop:

- `CompanyName : string`
- `VatNumber : string`

Keep `ContactPhoneNumber` (it remains an owner-level contact detail, distinct from any restaurant).

Update `RestaurantOwnerConfiguration` to remove these column mappings.

### `Restaurant` (entity)

Add:

- `VatNumber : string?` — nullable, max length 20.

Update `RestaurantConfiguration` to map the column.

### Migration

A single EF Core migration that:

- `DROP COLUMN RestaurantOwners.CompanyName`
- `DROP COLUMN RestaurantOwners.VatNumber`
- `ADD COLUMN Restaurants.VatNumber NVARCHAR(20) NULL`

Existing development data is disposable; no data backfill is needed.

## DTO changes

### `RestaurantRegister` (the wizard payload)

Restructured to a flat-but-grouped shape, with the restaurant payload nested:

```csharp
public class RestaurantRegister
{
    // Step 1 — account & owner
    [Required, MaxLength(50)]  public string FirstName { get; set; } = "";
    [Required, MaxLength(100)] public string LastName  { get; set; } = "";
    [Required, EmailAddress, MaxLength(50)] public string Email { get; set; } = "";
    [Required, MinLength(10), MaxLength(20)] public string ContactPhoneNumber { get; set; } = "";
    [Required, MinLength(12)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password))] public string ConfirmPassword { get; set; } = "";

    // Step 2 — first restaurant
    [Required] public CreateRestaurantDto Restaurant { get; set; } = new();
}
```

The `[Required]` on `CompanyName`/`VatNumber` is removed (fields removed). `ContactPhoneNumber` stays.

### `CreateRestaurantDto`

Add:

```csharp
[MaxLength(20)]
public string? VatNumber { get; set; }
```

(No `[Required]` — conditional rule is service-layer.)

### `UpdateRestaurantDto`, `RestaurantDto`, `DetailedRestaurantDto`

Add `VatNumber` (same nullable string shape) so owners can edit and the client can display it.

### `RestaurantMapper`

Copy `VatNumber` in entity↔DTO mappings (both directions).

## Server orchestration

### `RestaurantService` — new public surface

Two helpers extracted to support the combined flow without duplicating logic:

1. `Task<ServiceResult<(double lat, double lon)>> ValidateLegalAndLocateAsync(...)` — currently private; promote to a method on `IRestaurantService`.
2. `Task<ServiceResult<RestaurantDto>> CreateValidatedAsync(CreateRestaurantDto dto, int ownerId, (double lat, double lon) coords, CancellationToken ct = default)` — new overload that skips the SIRET/geocode round-trip when coords are already validated. Used by `AuthService` to avoid duplicate external calls. The existing public `CreateAsync` keeps its current behavior (validates internally) for the standalone create-restaurant flow.

### `RestaurantService.CreateAsync` / `UpdateAsync` — conditional VAT validation

After existing legal/coords validation:

```csharp
if (dto.IsVatRegistered && string.IsNullOrWhiteSpace(dto.VatNumber))
    return new ServiceError(ErrorMessages.VatNumberRequiredWhenVatRegistered);
```

Add `VatNumberRequiredWhenVatRegistered` to `ErrorMessages` (French): `"Le numéro de TVA est requis pour une entreprise assujettie à la TVA."`

When constructing the `Restaurant` entity, set `VatNumber = dto.VatNumber` (or `null` if not VAT-registered — normalize to `null` when `IsVatRegistered = false` to avoid stale data).

### `AuthService.RegisterRestaurantAsync` — rewired

```text
1. EmailExistsAsync(email) → 400 if used
2. RestaurantService.ValidateLegalAndLocateAsync(...)  → fail-fast on SIRET/legal/geocode error
3. BEGIN TRANSACTION
   3a. UserManager.CreateAsync(user, password)
       user has: FirstName, LastName, Email, RestaurantOwner { ContactPhoneNumber }, Customer = new()
   3b. UserManager.AddToRoleAsync(user, nameof(UserRole.RestaurantOwner))
   3c. RestaurantService.CreateValidatedAsync(dto.Restaurant, ownerId: user.Id, coords)
   3d. COMMIT (or ROLLBACK on any failure)
4. EmailJobService.QueueWelcomeEmailAsync(...)
5. return BuildConnectionResponse(user)
```

**Atomicity implementation**: open an `IDbContextTransaction` on the shared `DbContext` so Identity user creation, role assignment, and restaurant creation all participate. On exception or non-success `ServiceResult` from any step, rollback and return the original error. Step 2 runs *before* the transaction since it has no DB writes (only external API reads).

**Note on duplicate validation**: step 3c uses `CreateValidatedAsync` (defined above) with the coords already produced in step 2, so the SIRET / geocode external calls run exactly once.

### `AuthController.RegisterRestaurant`

URL unchanged (`POST /api/v1/auth/restaurant/register`). Action signature unchanged (it still accepts `RestaurantRegister` — the model just has different fields now). `ServiceResult` → `IActionResult` mapping unchanged.

## Client changes

### `Pages/Auth/RestaurantRegister/RestaurantRegisterPage.razor` — wizard

- Single component, single bound `RestaurantRegister` model, `currentStep : int` field.
- Step 1 fields: FirstName, LastName, Email, ContactPhoneNumber, Password, ConfirmPassword.
- Step 2 fields: all of `CreateRestaurantDto` — Name, Description, Type (select, default `Autre`), AdressLine1, AdressLine2, City, ZipCode, Country (select, default `France`), Siret, LegalName, LegalAddress, LegalForm, IsVatRegistered (checkbox), VatNumber (text — visible only when IsVatRegistered=true).
- "Suivant" button on step 1: validates step 1 client-side via `EditContext` partial validation (validate only step 1 properties); only advances if valid.
- "Précédent" / "Créer mon compte" buttons on step 2.
- Step indicator (1/2 → 2/2) at the top of the form.
- On server error: if the error references a step-1 field (e.g. `EmailAlreadyUsed`), navigate back to step 1 and show the error there; otherwise display the error on step 2.

### `Pages/Auth/RestaurantRegister/RestaurantRegisterPage.razor.scss`

Add wizard-specific styles:

- `.wizard-steps` indicator
- `.wizard-step` containers with show/hide
- "Précédent"/"Suivant" button row layout

### `Services/Auth/AuthService.cs` (client)

`RegisterRestaurant(RestaurantRegister)` — payload shape change only; URL constant in `ApiRoutes` unchanged.

### `Pages/Restaurant/Create/RestaurantCreation.razor` (existing standalone page)

Add the `VatNumber` input (visible only when `IsVatRegistered` is checked). No other changes.

## Testing

All new server tests use NUnit + NSubstitute, following the project's existing patterns.

### `RestaurantServiceTests`

New tests:

- `CreateAsync_IsVatRegisteredTrueWithoutVatNumber_ReturnsError`
- `CreateAsync_IsVatRegisteredTrueWithVatNumber_Persists`
- `CreateAsync_IsVatRegisteredFalseAndNoVatNumber_PersistsWithNullVat`
- `CreateAsync_IsVatRegisteredFalseWithStaleVatNumber_PersistsWithNullVat` (verify normalization)
- `UpdateAsync_*` mirroring the above

Updated tests: existing `CreateAsync_ValidLegalData_Persists` and `UpdateAsync_ValidLegalData_Persists` extended to round-trip `VatNumber`.

### `AuthServiceTests` (new file or extension of existing)

- `RegisterRestaurantAsync_EmailAlreadyUsed_ReturnsError_NoUserCreated`
- `RegisterRestaurantAsync_RestaurantLegalValidationFails_NoUserCreated`
- `RegisterRestaurantAsync_ValidPayload_CreatesUserOwnerRoleAndRestaurant`
- `RegisterRestaurantAsync_RestaurantInsertThrows_RollsBackUser`
- `RegisterRestaurantAsync_QueuesWelcomeEmailOnSuccess`

Mocks: `IUserRepository`, `IRestaurantService` (for the validate + create calls), `IEmailJobService`, transaction abstractions as needed.

### `AuthControllerTests`

Existing register-restaurant test updated to send the new nested DTO and assert pass-through to the service.

## Commit plan

Per CLAUDE.md commit strategy. Each commit is a buildable unit and includes Azure Boards reference (`PBI: AB#<id>`, `Task: AB#<id>`) — IDs provided by user before committing.

1. `feat(server): add VatNumber to Restaurant, drop CompanyName/VatNumber from RestaurantOwner` — entities + EF configurations.
2. `feat(db): migration for owner→restaurant company fields shift`.
3. `feat(shared): add VatNumber to restaurant DTOs and restructure RestaurantRegister`.
4. `feat(shared): update RestaurantMapper for VatNumber`.
5. `feat(server): add conditional VAT validation in RestaurantService with tests`.
6. `feat(server): expose restaurant validation helper for AuthService reuse`.
7. `feat(server): combine restaurant owner registration with first-restaurant creation`.
8. `feat(client): combined restaurant owner registration wizard`.
9. `feat(client): add VatNumber field to standalone restaurant creation`.
10. `style: apply formatting fixes` (only if `make format-check` flags anything).

End-of-feature: `make format-fix` → `make test` → final style commit if needed.

## Files affected (summary)

**Server:**

- `DeliverTableServer/Models/RestaurantOwner.cs`
- `DeliverTableServer/Models/Restaurant.cs`
- `DeliverTableServer/Configurations/RestaurantOwnerConfiguration.cs`
- `DeliverTableServer/Configurations/RestaurantConfiguration.cs`
- `DeliverTableServer/Migrations/<new>_OwnerRestaurantCompanyFieldsShift.*`
- `DeliverTableServer/Services/AuthService.cs`
- `DeliverTableServer/Services/RestaurantService.cs`
- `DeliverTableServer/Services/IRestaurantService.cs`
- `DeliverTableServer/Constants/ErrorMessages.cs`

**Shared:**

- `DeliverTableSharedLibrary/Dtos/Auth/RestaurantRegister.cs`
- `DeliverTableSharedLibrary/Dtos/Restaurant/CreateRestaurantDto.cs`
- `DeliverTableSharedLibrary/Dtos/Restaurant/UpdateRestaurantDto.cs`
- `DeliverTableSharedLibrary/Dtos/Restaurant/RestaurantDto.cs`
- `DeliverTableSharedLibrary/Dtos/Restaurant/DetailedRestaurantDto.cs`
- `DeliverTableServer/Mappers/RestaurantMappers.cs`

**Client:**

- `DeliverTableClient/Pages/Auth/RestaurantRegister/RestaurantRegisterPage.razor`
- `DeliverTableClient/Pages/Auth/RestaurantRegister/RestaurantRegisterPage.razor.scss`
- `DeliverTableClient/Services/Auth/AuthService.cs`
- `DeliverTableClient/Pages/Restaurant/Create/RestaurantCreation.razor`

**Tests:**

- `DeliverTableTests/Server/Unit/Services/RestaurantServiceTests.cs`
- `DeliverTableTests/Server/Unit/Services/AuthServiceTests.cs`
- `DeliverTableTests/Server/Unit/Controllers/AuthControllerTests.cs`

## Risks & open items

- **Identity transaction interaction**: `UserManager` uses the registered `IUserStore` over the same `DbContext` as the rest of the app. Wrapping its calls in an explicit `IDbContextTransaction` is supported but warrants verification in a focused test (`RegisterRestaurantAsync_RestaurantInsertThrows_RollsBackUser`).
- **Duplicate SIRET validation cost**: avoided by passing precomputed coords through `RestaurantService.CreateValidatedAsync` overload.
- **Wizard step-1 → step-1 error routing**: requires the client to interpret server error codes / messages — coordinate with `ErrorMessages` so the client can match on a stable signal (e.g. specific message constants exposed via shared library, not free-form strings). If matching by message string is too brittle, expose an error code on `ServiceError`.
- **No existing-data migration concern** — confirmed by user that existing dev data is disposable.
