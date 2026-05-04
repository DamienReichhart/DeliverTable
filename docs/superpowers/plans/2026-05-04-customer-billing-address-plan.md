# Customer Billing Address Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a structured French postal billing address to every customer (`User`), make it editable from the profile page, require it at checkout, and render it on the customer invoice's recipient block.

**Architecture:** Five new flat `Billing*` columns on the `User` entity (Approach 1 from the spec). A single static helper `BillingAddressHelper` exposes `HasCompleteBillingAddress` (used by the order checkout guard) and `FormatBillingAddressForSnapshot` (used by `BuildCustomerInvoice` to populate the existing `InvoiceLegalSnapshotDto.Address` field as a `\n`-joined French postal block). Profile UI gets one new section with five inputs.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core (Npgsql), Blazor WASM, NUnit 4, NSubstitute, Docker Compose, QuestPDF (no renderer changes — `\n` already renders as line breaks).

**Spec:** [`docs/superpowers/specs/2026-05-04-customer-billing-address-design.md`](../specs/2026-05-04-customer-billing-address-design.md)

---

## File Structure

### Created
- `DeliverTableServer/Common/BillingAddressHelper.cs` — pure static helper (`HasCompleteBillingAddress` + `FormatBillingAddressForSnapshot`).
- `DeliverTableInfrastructure/Migrations/<timestamp>_AddUserBillingAddress.cs` (+ `.Designer.cs`) — generated.
- `DeliverTableTests/Server/Unit/Common/BillingAddressHelperTests.cs` — helper tests.

### Modified
- `DeliverTableInfrastructure/Models/User.cs` — five `Billing*` properties.
- `DeliverTableInfrastructure/Migrations/DeliverTableContextModelSnapshot.cs` — auto-updated by EF.
- `DeliverTableSharedLibrary/Dtos/Auth/UpdateProfileRequest.cs` — five new optional fields with `[MaxLength]`.
- `DeliverTableServer/Services/AuthService.cs` — copy + trim billing fields in `UpdateProfileAsync`.
- `DeliverTableServer/Services/OrderService.cs` — checkout guard at the top of `CreateFromCartAsync`.
- `DeliverTableServer/Services/InvoiceService.cs` — populate `recipientSnapshot.Address` via the helper.
- `DeliverTableServer/Constants/ErrorMessages.cs` — add `BillingAddressIncomplete`.
- `DeliverTableTests/Server/Factories/ServerEntityFactory.cs` — populate billing fields on `CreateValidUser` so existing tests satisfy the new guard.
- `DeliverTableTests/Server/Unit/Services/AuthServiceTests.cs` — new test for billing-field persistence.
- `DeliverTableTests/Server/Unit/Services/OrderServiceTests.cs` — new guard tests; existing happy-path mocks updated to return a billing-complete user.
- `DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs` — new test asserting recipient snapshot carries the formatted address.
- `DeliverTableClient/Pages/Profile/Profile.razor` + `Profile.razor.cs` + `Profile.razor.scss` — new "Adresse de facturation" section.
- `DeliverTableClient/Pages/Checkout/Checkout/Checkout.razor` (+ codebehind) — special-case `BillingAddressIncomplete` error → render "Compléter mon profil" link.

---

## Pre-flight

The dev stack must be running for build, test, and migration commands. Verify:

```bash
docker compose -f /home/damien/DeliverTable/docker-dev.yaml ps --format "table {{.Service}}\t{{.State}}"
```

`backend`, `database`, `frontend` should all be `running`. If not, run `make dev` first.

The pre-existing test failure `AppEnvironmentTests.Load_AppliesDefaults_WhenOptionalVarsAreMissing` is a documented Docker env-leak (CLAUDE.md). Ignore it; any *other* failure must be fixed before commit.

---

## Task 1: Add `Billing*` properties to `User` entity

**Why:** Persistence layer for the new fields. No business logic — pure schema addition. No TDD per CLAUDE.md ("Enums, entities/models, DTOs, mappers, EF configurations, migrations, DI registrations" do not need TDD).

**Files:**
- Modify: `/home/damien/DeliverTable/DeliverTableInfrastructure/Models/User.cs`

- [ ] **Step 1: Add the five properties to the entity**

Open `User.cs`. After the existing `StripeCustomerId` property and before the navigation properties (`RestaurantOwner`, `Customer`, `Restaurants`), add:

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

The `using System.ComponentModel.DataAnnotations;` directive is already at the top of the file (it's required for the existing `[Required]`, `[EmailAddress]`, `[MaxLength]` attributes). No new using directives needed.

- [ ] **Step 2: Build to verify**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: green, no new warnings (4 pre-existing MailKit advisories are acceptable).

- [ ] **Step 3: Commit**

```bash
git add DeliverTableInfrastructure/Models/User.cs
git commit -m "feat(server): add billing address properties to User entity"
```

---

## Task 2: EF migration `AddUserBillingAddress`

**Why:** Apply the new columns to the `AspNetUsers` table on the database side.

**Files:**
- Create: `DeliverTableInfrastructure/Migrations/<timestamp>_AddUserBillingAddress.cs` (+ `.Designer.cs`) — generated.
- Modify: `DeliverTableInfrastructure/Migrations/DeliverTableContextModelSnapshot.cs` — auto-updated.

- [ ] **Step 1: Generate the migration**

```bash
docker compose -f docker-dev.yaml exec backend dotnet ef migrations add AddUserBillingAddress \
    --project /src/DeliverTableInfrastructure \
    --startup-project /src/DeliverTableServer \
    --output-dir Migrations
```

Expected: three new/modified files. The new timestamp prefix should be ≥ `20260504074652` (the current latest, `AddInvoiceLineKind`).

- [ ] **Step 2: Verify the generated migration**

Open the new `<timestamp>_AddUserBillingAddress.cs`. The `Up` method should contain five `migrationBuilder.AddColumn<string>` calls on `"AspNetUsers"`, one per field, all with `nullable: false` and `defaultValue: ""`. For example:

```csharp
migrationBuilder.AddColumn<string>(
    name: "BillingAddressLine1",
    table: "AspNetUsers",
    type: "character varying(200)",
    maxLength: 200,
    nullable: false,
    defaultValue: "");
```

`Down` should contain five `DropColumn` calls.

If `defaultValue: ""` is missing on any column, edit the file to add it — without it, the migration would fail to apply against tables with existing rows.

- [ ] **Step 3: Apply the migration**

```bash
make dev-migrate
```

Expected: `Applying migration '<timestamp>_AddUserBillingAddress'.` followed by five `ALTER TABLE "AspNetUsers" ADD ...` SQL statements, then `Done.`

- [ ] **Step 4: Build to verify**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: green.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableInfrastructure/Migrations/
git commit -m "feat(db): add migration AddUserBillingAddress"
```

---

## Task 3: `BillingAddressHelper` with helper tests

**Why:** Single source of truth for "complete" semantics and address formatting. Used by the checkout guard (Task 6) and invoice integration (Task 7). Pure static — no dependencies, easy to unit-test.

**Files:**
- Create: `/home/damien/DeliverTable/DeliverTableServer/Common/BillingAddressHelper.cs`
- Create: `/home/damien/DeliverTable/DeliverTableTests/Server/Unit/Common/BillingAddressHelperTests.cs`

Strict TDD.

- [ ] **Step 1: Write the failing tests**

Create `DeliverTableTests/Server/Unit/Common/BillingAddressHelperTests.cs`:

```csharp
using DeliverTableInfrastructure.Models;
using DeliverTableServer.Common;

namespace DeliverTableTests.Server.Unit.Common;

[TestFixture]
public class BillingAddressHelperTests
{
    private static User UserWith(
        string line1 = "12 rue de la Paix",
        string line2 = "",
        string postal = "75002",
        string city = "Paris",
        string country = "France") => new()
    {
        FirstName = "Jean",
        LastName = "Dupont",
        Email = "jean@example.fr",
        BillingAddressLine1 = line1,
        BillingAddressLine2 = line2,
        BillingPostalCode = postal,
        BillingCity = city,
        BillingCountry = country,
    };

    [Test]
    public void HasCompleteBillingAddress_AllRequiredPresent_ReturnsTrue()
    {
        Assert.That(BillingAddressHelper.HasCompleteBillingAddress(UserWith()), Is.True);
    }

    [Test]
    public void HasCompleteBillingAddress_LineTwoEmpty_StillTrue()
    {
        Assert.That(BillingAddressHelper.HasCompleteBillingAddress(UserWith(line2: "")), Is.True);
    }

    [TestCase("", "75002", "Paris", "France")]
    [TestCase("12 rue", "", "Paris", "France")]
    [TestCase("12 rue", "75002", "", "France")]
    [TestCase("12 rue", "75002", "Paris", "")]
    [TestCase("   ", "75002", "Paris", "France")]
    public void HasCompleteBillingAddress_AnyRequiredFieldBlank_ReturnsFalse(
        string line1, string postal, string city, string country)
    {
        Assert.That(BillingAddressHelper.HasCompleteBillingAddress(
            UserWith(line1: line1, postal: postal, city: city, country: country)), Is.False);
    }

    [Test]
    public void FormatBillingAddressForSnapshot_FullAddress_ReturnsFourLines()
    {
        var user = UserWith(line2: "Bât. B");
        var formatted = BillingAddressHelper.FormatBillingAddressForSnapshot(user);

        Assert.That(formatted, Is.EqualTo("12 rue de la Paix\nBât. B\n75002 Paris\nFrance"));
    }

    [Test]
    public void FormatBillingAddressForSnapshot_NoLineTwo_ReturnsThreeLines()
    {
        var formatted = BillingAddressHelper.FormatBillingAddressForSnapshot(UserWith());

        Assert.That(formatted, Is.EqualTo("12 rue de la Paix\n75002 Paris\nFrance"));
    }

    [Test]
    public void FormatBillingAddressForSnapshot_AllEmpty_ReturnsEmptyString()
    {
        var user = UserWith(line1: "", postal: "", city: "", country: "");

        Assert.That(BillingAddressHelper.FormatBillingAddressForSnapshot(user), Is.EqualTo(string.Empty));
    }

    [Test]
    public void FormatBillingAddressForSnapshot_TrimsWhitespace()
    {
        var user = UserWith(line1: "  12 rue  ", postal: " 75002 ", city: " Paris ", country: " France ");
        var formatted = BillingAddressHelper.FormatBillingAddressForSnapshot(user);

        Assert.That(formatted, Is.EqualTo("12 rue\n75002 Paris\nFrance"));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~BillingAddressHelperTests"
```

Expected: build error — `BillingAddressHelper` is not defined.

- [ ] **Step 3: Implement the helper**

Create `DeliverTableServer/Common/BillingAddressHelper.cs`:

```csharp
using DeliverTableInfrastructure.Models;

namespace DeliverTableServer.Common;

public static class BillingAddressHelper
{
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
            (user.BillingPostalCode ?? string.Empty).Trim(),
            (user.BillingCity ?? string.Empty).Trim(),
        }.Where(s => s.Length > 0));
        if (postalCity.Length > 0)
            lines.Add(postalCity);

        if (!string.IsNullOrWhiteSpace(user.BillingCountry))
            lines.Add(user.BillingCountry.Trim());

        return string.Join("\n", lines);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~BillingAddressHelperTests"
```

Expected: 11 PASS (3 base + 5 parameterized + 3 formatter).

- [ ] **Step 5: Build full solution**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: green.

- [ ] **Step 6: Commit**

```bash
git add DeliverTableServer/Common/BillingAddressHelper.cs \
        DeliverTableTests/Server/Unit/Common/BillingAddressHelperTests.cs
git commit -m "feat(server): add BillingAddressHelper with completeness and formatting"
```

---

## Task 4: Extend `UpdateProfileRequest` DTO

**Why:** Add the five fields to the profile-update payload. Optional at the DTO level (so the customer can save partial state); the hard requirement lives in `OrderService` (Task 6).

**Files:**
- Modify: `/home/damien/DeliverTable/DeliverTableSharedLibrary/Dtos/Auth/UpdateProfileRequest.cs`

No TDD — pure DTO extension.

- [ ] **Step 1: Add the five fields**

Replace `UpdateProfileRequest.cs` with:

```csharp
using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Auth;

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Votre prénom est nécessaire")]
    [MaxLength(50, ErrorMessage = "Le prénom ne peut pas dépasser 50 caractères")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Votre nom de famille est nécessaire")]
    [MaxLength(50, ErrorMessage = "Le nom de famille ne peut pas dépasser 50 caractères")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Votre adresse mail est nécessaire")]
    [EmailAddress(ErrorMessage = "L'adresse mail n'est pas valide")]
    [MaxLength(100, ErrorMessage = "L'adresse mail ne peut pas dépasser 100 caractères")]
    public string Email { get; set; } = "";

    [MaxLength(200, ErrorMessage = "L'adresse ne peut pas dépasser 200 caractères")]
    public string BillingAddressLine1 { get; set; } = "";

    [MaxLength(200, ErrorMessage = "Le complément d'adresse ne peut pas dépasser 200 caractères")]
    public string BillingAddressLine2 { get; set; } = "";

    [MaxLength(10, ErrorMessage = "Le code postal ne peut pas dépasser 10 caractères")]
    public string BillingPostalCode { get; set; } = "";

    [MaxLength(100, ErrorMessage = "La ville ne peut pas dépasser 100 caractères")]
    public string BillingCity { get; set; } = "";

    [MaxLength(100, ErrorMessage = "Le pays ne peut pas dépasser 100 caractères")]
    public string BillingCountry { get; set; } = "";
}
```

- [ ] **Step 2: Build to verify**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: green. The existing `AuthService.UpdateProfileAsync` keeps compiling — it doesn't read the new fields yet (Task 5 wires them up).

- [ ] **Step 3: Commit**

```bash
git add DeliverTableSharedLibrary/Dtos/Auth/UpdateProfileRequest.cs
git commit -m "feat(shared): add billing address fields to UpdateProfileRequest"
```

---

## Task 5: `AuthService.UpdateProfileAsync` persists billing fields + error message

**Why:** Wire the DTO fields into the entity through the existing service method. Add the new error message constant for use in Task 6.

**Files:**
- Modify: `/home/damien/DeliverTable/DeliverTableServer/Constants/ErrorMessages.cs`
- Modify: `/home/damien/DeliverTable/DeliverTableServer/Services/AuthService.cs`
- Modify: `/home/damien/DeliverTable/DeliverTableTests/Server/Unit/Services/AuthServiceTests.cs`

TDD for the service change.

- [ ] **Step 1: Add the error message constant**

Open `DeliverTableServer/Constants/ErrorMessages.cs`. Add (anywhere after existing constants — match local placement convention; if there's an alphabetical/logical grouping, put it near `UserNotFound` or `CartEmpty`):

```csharp
public const string BillingAddressIncomplete =
    "Veuillez compléter votre adresse de facturation dans votre profil avant de commander.";
```

`UserNotFound` already exists — do NOT re-add it.

- [ ] **Step 2: Write the failing test**

Open `DeliverTableTests/Server/Unit/Services/AuthServiceTests.cs`. Add this test inside the existing `[TestFixture]`:

```csharp
[Test]
public async Task UpdateProfile_WithBillingFields_PersistsThemTrimmed()
{
    var user = ServerEntityFactory.CreateValidUser("u@example.fr");
    user.Id = 42;
    user.BillingAddressLine1 = string.Empty;
    user.BillingPostalCode = string.Empty;
    user.BillingCity = string.Empty;
    user.BillingCountry = string.Empty;

    _userRepository.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(user);
    _userRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

    var request = new UpdateProfileRequest
    {
        FirstName = "Jean",
        LastName = "Dupont",
        Email = "u@example.fr",
        BillingAddressLine1 = "  12 rue de la Paix  ",
        BillingAddressLine2 = "",
        BillingPostalCode = " 75002 ",
        BillingCity = " Paris ",
        BillingCountry = " France ",
    };

    var result = await _sut.UpdateProfileAsync(42, request, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(user.BillingAddressLine1, Is.EqualTo("12 rue de la Paix"));
    Assert.That(user.BillingAddressLine2, Is.EqualTo(""));
    Assert.That(user.BillingPostalCode, Is.EqualTo("75002"));
    Assert.That(user.BillingCity, Is.EqualTo("Paris"));
    Assert.That(user.BillingCountry, Is.EqualTo("France"));
}
```

- [ ] **Step 3: Run the test to verify it fails**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~AuthServiceTests.UpdateProfile_WithBillingFields_PersistsThemTrimmed"
```

Expected: FAIL — `BillingAddressLine1` etc. are still `""` because `UpdateProfileAsync` doesn't copy them yet.

- [ ] **Step 4: Update `AuthService.UpdateProfileAsync`**

In `DeliverTableServer/Services/AuthService.cs`, find `UpdateProfileAsync`. After the existing `user.UpdatedAt = DateTime.UtcNow;` line (and before `await _userRepository.SaveChangesAsync(ct);`), insert:

```csharp
user.BillingAddressLine1 = (request.BillingAddressLine1 ?? string.Empty).Trim();
user.BillingAddressLine2 = (request.BillingAddressLine2 ?? string.Empty).Trim();
user.BillingPostalCode = (request.BillingPostalCode ?? string.Empty).Trim();
user.BillingCity = (request.BillingCity ?? string.Empty).Trim();
user.BillingCountry = (request.BillingCountry ?? string.Empty).Trim();
```

- [ ] **Step 5: Run the test to verify it passes**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~AuthServiceTests.UpdateProfile_WithBillingFields_PersistsThemTrimmed"
```

Expected: PASS.

- [ ] **Step 6: Run all `AuthServiceTests`**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~AuthServiceTests"
```

Expected: all green.

- [ ] **Step 7: Commit**

```bash
git add DeliverTableServer/Constants/ErrorMessages.cs \
        DeliverTableServer/Services/AuthService.cs \
        DeliverTableTests/Server/Unit/Services/AuthServiceTests.cs
git commit -m "feat(server): persist trimmed billing fields in UpdateProfileAsync"
```

---

## Task 6: Checkout guard in `OrderService.CreateFromCartAsync`

**Why:** Block order creation when the customer's billing address is incomplete. This is the hard requirement that guarantees every new invoice has the legally-required recipient block.

**Files:**
- Modify: `/home/damien/DeliverTable/DeliverTableServer/Services/OrderService.cs`
- Modify: `/home/damien/DeliverTable/DeliverTableTests/Server/Factories/ServerEntityFactory.cs`
- Modify: `/home/damien/DeliverTable/DeliverTableTests/Server/Unit/Services/OrderServiceTests.cs`

TDD for the guard. Existing tests need their seed user populated with a complete billing address so they continue to pass — that's done by updating `ServerEntityFactory.CreateValidUser`.

- [ ] **Step 1: Update `CreateValidUser` to seed a complete billing address**

In `DeliverTableTests/Server/Factories/ServerEntityFactory.cs`, modify `CreateValidUser` so the returned `User` has the four required billing fields populated. Replace the method body with:

```csharp
public static User CreateValidUser(string? email = null)
{
    var resolvedEmail = email ?? $"user{Interlocked.Increment(ref _emailCounter)}@example.com";

    return new User
    {
        UserName = resolvedEmail,
        Email = resolvedEmail,
        NormalizedEmail = resolvedEmail.ToUpperInvariant(),
        NormalizedUserName = resolvedEmail.ToUpperInvariant(),
        FirstName = "Test",
        LastName = "User",
        Status = UserStatus.Active,
        SecurityStamp = Guid.NewGuid().ToString(),
        BillingAddressLine1 = "12 rue de la Paix",
        BillingAddressLine2 = string.Empty,
        BillingPostalCode = "75002",
        BillingCity = "Paris",
        BillingCountry = "France",
    };
}
```

- [ ] **Step 2: Write the failing tests**

In `DeliverTableTests/Server/Unit/Services/OrderServiceTests.cs`, add two new tests inside the existing `[TestFixture]` (place them after the existing `CreateFromCart` tests). The first test exercises the guard's failure path:

```csharp
[Test]
public async Task CreateFromCart_WhenBillingAddressIncomplete_ReturnsBillingError()
{
    var customer = ServerEntityFactory.CreateValidUser("c@example.fr");
    customer.Id = 1;
    customer.BillingAddressLine1 = string.Empty; // wipe one required field

    _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(customer);

    var request = new CreateOrderRequest
    {
        RestaurantId = 5,
        OrderType = nameof(OrderType.Delivery),
        DeliveryAddress = "1 av des Champs-Élysées",
    };

    var result = await _sut.CreateFromCartAsync(1, request, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.False);
    Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.BillingAddressIncomplete));
    await _restaurantRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
}
```

The second test asserts the happy path with a complete billing address still works. If a similar happy-path test already exists in this file, you may rely on it instead — but adding a fresh, focused test makes the guard's positive case explicit:

```csharp
[Test]
public async Task CreateFromCart_WhenBillingAddressComplete_PassesGuard()
{
    var customer = ServerEntityFactory.CreateValidUser("c@example.fr");
    customer.Id = 1;
    // CreateValidUser already seeds a complete billing address — leave as-is.

    _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(customer);

    var request = new CreateOrderRequest
    {
        RestaurantId = 5,
        OrderType = nameof(OrderType.Delivery),
        DeliveryAddress = "1 av des Champs-Élysées",
    };

    var result = await _sut.CreateFromCartAsync(1, request, CancellationToken.None);

    // The guard MUST pass; a downstream failure (e.g. RestaurantNotFound) is acceptable —
    // we're only asserting the billing error did NOT short-circuit.
    Assert.That(result.IsSuccess
        || result.Error!.Message != ErrorMessages.BillingAddressIncomplete,
        Is.True);
    await _restaurantRepository.Received().GetByIdAsync(5, Arg.Any<CancellationToken>());
}
```

- [ ] **Step 3: Run new tests to verify they fail**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~OrderServiceTests.CreateFromCart_WhenBillingAddress"
```

Expected: BOTH fail — the first because no guard exists yet (no `BillingAddressIncomplete` error is returned), the second because the existing flow may fail before reaching `_restaurantRepository.GetByIdAsync` (the `_userRepository.GetByIdAsync` call doesn't exist in `CreateFromCartAsync` yet, so the `Received()` assertion may fail with a different downstream error).

- [ ] **Step 4: Add the guard to `OrderService.CreateFromCartAsync`**

In `DeliverTableServer/Services/OrderService.cs`, locate the `CreateFromCartAsync` method. Find the very first lines of the method body (after the method signature, before the `OrderType` parsing). Insert the guard at the top:

```csharp
public async Task<ServiceResult<CreateOrderResponse>> CreateFromCartAsync(
    int customerId, CreateOrderRequest request, CancellationToken ct = default)
{
    var customer = await _userRepository.GetByIdAsync(customerId, ct);
    if (customer is null)
        return new ServiceError(ErrorMessages.UserNotFound, 404);

    if (!BillingAddressHelper.HasCompleteBillingAddress(customer))
        return new ServiceError(ErrorMessages.BillingAddressIncomplete);

    if (!Enum.TryParse<OrderType>(request.OrderType, out var orderType))
    // ... rest of the method unchanged
```

Add the using directive at the top of the file if it isn't already there:

```csharp
using DeliverTableServer.Common;
```

- [ ] **Step 5: Run the new tests to verify they pass**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~OrderServiceTests.CreateFromCart_WhenBillingAddress"
```

Expected: both PASS.

- [ ] **Step 6: Run ALL `OrderServiceTests` to find regressions**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~OrderServiceTests"
```

Expected: all green. Pre-existing `CreateFromCart` tests should pass because:
1. `CreateValidUser` now seeds a complete billing address.
2. Tests that previously didn't mock `_userRepository.GetByIdAsync(customerId, ...)` will now hit the guard's `customer is null` branch and return `UserNotFound` instead of silently bypassing the guard.

If any pre-existing test fails because it didn't mock `_userRepository.GetByIdAsync`, update that test to include the mock. Search for tests that call `CreateFromCartAsync` without setting up `_userRepository.GetByIdAsync` and add:

```csharp
_userRepository.GetByIdAsync(<customerId>, Arg.Any<CancellationToken>())
    .Returns(ServerEntityFactory.CreateValidUser());
```

- [ ] **Step 7: Run the full server-test suite**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build
```

Expected: all green except the documented `AppEnvironmentTests` Docker leak.

- [ ] **Step 8: Commit**

```bash
git add DeliverTableServer/Services/OrderService.cs \
        DeliverTableTests/Server/Factories/ServerEntityFactory.cs \
        DeliverTableTests/Server/Unit/Services/OrderServiceTests.cs
git commit -m "feat(server): require complete billing address before order creation"
```

---

## Task 7: `BuildCustomerInvoice` populates recipient address

**Why:** Surface the customer's billing address on the invoice's recipient block so the legal recipient information is complete.

**Files:**
- Modify: `/home/damien/DeliverTable/DeliverTableServer/Services/InvoiceService.cs`
- Modify: `/home/damien/DeliverTable/DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs`

TDD for the integration.

- [ ] **Step 1: Write the failing test**

In `DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs`, add this test inside the existing `[TestFixture]` (place near the other `BuildCustomerInvoice_With...` tests):

```csharp
[Test]
public async Task BuildCustomerInvoice_WithCustomerAddress_PopulatesRecipientSnapshot()
{
    var captured = ArrangeCapture();
    var resto = Resto();
    var customer = Cust();
    customer.BillingAddressLine1 = "12 rue de la Paix";
    customer.BillingAddressLine2 = "Bât. B";
    customer.BillingPostalCode = "75002";
    customer.BillingCity = "Paris";
    customer.BillingCountry = "France";

    var order = BuildOrder(
        orderId: 100,
        resto,
        customer,
        items: new() { Item("Plat", 50m, 1, VatRate.Normal20) },
        discounts: new());
    ArrangeDefaultMocks(100, order, resto.Id);

    var result = await _sut.CreatePendingInvoicesForCapturedOrderAsync(100, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    var customerInvoice = captured.Single(i => i.Kind == InvoiceKind.OrderInvoiceToCustomer);
    var snapshot = JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(customerInvoice.RecipientSnapshotJson);
    Assert.That(snapshot, Is.Not.Null);
    Assert.That(snapshot!.Address,
        Is.EqualTo("12 rue de la Paix\nBât. B\n75002 Paris\nFrance"));
}
```

If the file doesn't already import `System.Text.Json`, add `using System.Text.Json;` at the top.

- [ ] **Step 2: Run the test to verify it fails**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~InvoiceServiceTests.BuildCustomerInvoice_WithCustomerAddress_PopulatesRecipientSnapshot"
```

Expected: FAIL — the snapshot's `Address` is empty (current code passes `Address: string.Empty`).

- [ ] **Step 3: Update `BuildCustomerInvoice`**

In `DeliverTableServer/Services/InvoiceService.cs`, find the `recipientSnapshot` construction (around line 460). Replace the `Address: string.Empty,` line with:

```csharp
Address: BillingAddressHelper.FormatBillingAddressForSnapshot(customer),
```

Add the using directive at the top of the file if it's not already there:

```csharp
using DeliverTableServer.Common;
```

- [ ] **Step 4: Run the test to verify it passes**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~InvoiceServiceTests.BuildCustomerInvoice_WithCustomerAddress_PopulatesRecipientSnapshot"
```

Expected: PASS.

- [ ] **Step 5: Run the full `InvoiceServiceTests` suite**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~InvoiceServiceTests"
```

Expected: all green. Existing tests use orders built via `BuildOrder(..., customer: Cust())` where `Cust()` does not set billing fields — they currently pass through with empty `Address`, which the renderer skips. That's still the desired behavior for legacy data.

- [ ] **Step 6: Run the full test suite**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build
```

Expected: all green except the documented Docker leak.

- [ ] **Step 7: Commit**

```bash
git add DeliverTableServer/Services/InvoiceService.cs \
        DeliverTableTests/Server/Unit/Services/InvoiceServiceTests.cs
git commit -m "feat(server): populate customer invoice recipient address from profile"
```

---

## Task 8: Profile page UI

**Why:** Give customers a place to fill in / edit their billing address. The form already binds to `UpdateProfileRequest`; we just need to add five inputs and a banner.

**Files:**
- Modify: `/home/damien/DeliverTable/DeliverTableClient/Pages/Profile/Profile.razor`
- Modify: `/home/damien/DeliverTable/DeliverTableClient/Pages/Profile/Profile.razor.cs` (codebehind, if it exists — otherwise the `@code` block within `Profile.razor`)
- Modify: `/home/damien/DeliverTable/DeliverTableClient/Pages/Profile/Profile.razor.scss` (small style additions if needed)

No TDD — UI work. Manual smoke verification in the browser.

- [ ] **Step 1: Read the existing profile page**

Open `Profile.razor` and `Profile.razor.cs` (or look at the `@code` block). Identify:
- The `EditForm` block that binds to `_profileModel` (an `UpdateProfileRequest`).
- The submit button(s).
- The success / error display blocks.
- The default value for `BillingCountry` — which should be pre-filled to `"France"` when the model is initialized for an existing user with no address.

- [ ] **Step 2: Pre-fill `BillingCountry` default in the load handler**

In `Profile.razor.cs` (or the `@code` block), find where `_profileModel` is populated from the loaded profile (after `UserService.GetProfileAsync()`). After the assignment, ensure `BillingCountry` defaults to `"France"` if empty:

```csharp
if (string.IsNullOrWhiteSpace(_profileModel.BillingCountry))
    _profileModel.BillingCountry = "France";
```

- [ ] **Step 3: Add the billing address form section**

In `Profile.razor`, inside the `EditForm` block (after the existing identity fields — `FirstName`, `LastName`, `Email`), insert a new fieldset/section:

```razor
<fieldset class="profile-page__section">
    <legend class="profile-page__section-title">Adresse de facturation</legend>
    <p class="profile-page__section-help">
        Ces informations apparaissent sur vos factures et sont requises pour passer commande.
    </p>

    <div class="profile-page__field">
        <label for="billing-line1">Adresse</label>
        <InputText id="billing-line1" class="form-control"
                   placeholder="12 rue de la Paix"
                   @bind-Value="_profileModel.BillingAddressLine1" />
        <ValidationMessage For="() => _profileModel.BillingAddressLine1" />
    </div>

    <div class="profile-page__field">
        <label for="billing-line2">Complément d'adresse (optionnel)</label>
        <InputText id="billing-line2" class="form-control"
                   placeholder="Bât. B, 3ᵉ étage"
                   @bind-Value="_profileModel.BillingAddressLine2" />
        <ValidationMessage For="() => _profileModel.BillingAddressLine2" />
    </div>

    <div class="profile-page__field-row">
        <div class="profile-page__field profile-page__field--postal">
            <label for="billing-postal">Code postal</label>
            <InputText id="billing-postal" class="form-control"
                       placeholder="75002"
                       @bind-Value="_profileModel.BillingPostalCode" />
            <ValidationMessage For="() => _profileModel.BillingPostalCode" />
        </div>

        <div class="profile-page__field profile-page__field--city">
            <label for="billing-city">Ville</label>
            <InputText id="billing-city" class="form-control"
                       placeholder="Paris"
                       @bind-Value="_profileModel.BillingCity" />
            <ValidationMessage For="() => _profileModel.BillingCity" />
        </div>
    </div>

    <div class="profile-page__field">
        <label for="billing-country">Pays</label>
        <InputText id="billing-country" class="form-control"
                   @bind-Value="_profileModel.BillingCountry" />
        <ValidationMessage For="() => _profileModel.BillingCountry" />
    </div>
</fieldset>
```

If the existing form uses different markup conventions (e.g. plain `<input>` tags or a different CSS class system), match the surrounding style instead of copying these classes verbatim.

- [ ] **Step 4: Add minimal styles**

In `Profile.razor.scss`, add (or adapt to match existing patterns):

```scss
.profile-page__section {
    border: none;
    padding: 0;
    margin-top: 1.5rem;
}

.profile-page__section-title {
    font-weight: 600;
    margin-bottom: 0.5rem;
}

.profile-page__section-help {
    font-size: 0.875rem;
    color: var(--color-text-muted, #6c757d);
    margin-bottom: 1rem;
}

.profile-page__field-row {
    display: grid;
    grid-template-columns: 1fr 2fr;
    gap: 1rem;
}

.profile-page__field {
    margin-bottom: 1rem;
}
```

If similar utilities already exist on the page, use those instead. Don't duplicate.

- [ ] **Step 5: Smoke-test in the browser**

Visit `https://localhost` (or whatever the dev URL is per `docker-dev.yaml`) and navigate to the customer profile page. Verify:
- The new "Adresse de facturation" section appears.
- All five inputs are rendered and bindable.
- Country defaults to "France".
- Saving with all fields filled returns success.
- Refreshing the page shows the saved values.
- Saving with a field cleared also succeeds (DTO accepts partial state).

If you can't access the browser from this environment, skip this step and rely on Task 9's smoke test (which exercises the full checkout-redirect-edit-checkout loop).

- [ ] **Step 6: Build to verify**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: green.

- [ ] **Step 7: Commit**

```bash
git add DeliverTableClient/Pages/Profile/
git commit -m "feat(client): add billing address section to customer profile page"
```

---

## Task 9: Checkout error UX — surface "Compléter mon profil" link

**Why:** When the new checkout guard fires (Task 6), the customer sees the French error message. We add a one-click way to navigate to the profile page so the fix is obvious and one-step.

**Files:**
- Modify: `/home/damien/DeliverTable/DeliverTableClient/Pages/Checkout/Checkout/Checkout.razor`
- Modify: `/home/damien/DeliverTable/DeliverTableClient/Pages/Checkout/Checkout/Checkout.razor.cs` (codebehind — adjust path to match actual file structure)

- [ ] **Step 1: Read the existing checkout error display**

Open `Checkout.razor`. Find the error block (the one that renders `_error`). Find where `_error` is set — likely in a method called `StartPaymentAsync` or `CreateOrderAsync`, when the response from the order creation API is non-success.

- [ ] **Step 2: Add a special-case render for billing-incomplete**

Locate the existing error block, currently something like:

```razor
@if (_error is not null)
{
    <div class="error-message">
        <p>@_error</p>
    </div>
    <div class="checkout-page__back">
        <button type="button" class="app-btn app-btn--secondary" @onclick="GoBack">
            Retour au panier
        </button>
    </div>
}
```

Replace it with:

```razor
@if (_error is not null)
{
    <div class="error-message">
        <p>@_error</p>
    </div>
    <div class="checkout-page__back">
        @if (_isBillingIncomplete)
        {
            <a class="app-btn app-btn--primary" href="/profile">
                Compléter mon profil
            </a>
        }
        <button type="button" class="app-btn app-btn--secondary" @onclick="GoBack">
            Retour au panier
        </button>
    </div>
}
```

- [ ] **Step 3: Add the `_isBillingIncomplete` flag and the message constant**

`ErrorMessages` lives in the server project and the client doesn't reference it. So we hard-code the French sentence on the client (a single string in one place — fine for one error path).

In the checkout codebehind (`Checkout.razor.cs`), inside the page class:

```csharp
private const string BillingIncompleteMessage =
    "Veuillez compléter votre adresse de facturation dans votre profil avant de commander.";

private bool _isBillingIncomplete;
```

In the method that handles the order-creation failure (the one that currently sets `_error = ...`), set the flag based on the message:

```csharp
_error = errorMessage;
_isBillingIncomplete = errorMessage == BillingIncompleteMessage;
```

(Replace `errorMessage` with whatever local variable holds the API error string in this codebase. If the page has multiple failure paths setting `_error`, also set `_isBillingIncomplete = false` on success / on each non-billing failure to keep the flag consistent across retries.)

- [ ] **Step 4: Smoke-test in the browser (manual)**

If you can access the browser:
1. Wipe one of the four required billing fields on a customer profile.
2. Try to place an order.
3. The checkout should display the French error and the "Compléter mon profil" button.
4. Click the button → should navigate to `/profile`.
5. Fill in the missing field, save, return to checkout, place the order — should succeed.

If browser access isn't possible, build and rely on the unit-test coverage from Task 6 plus a code review.

- [ ] **Step 5: Build to verify**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: green.

- [ ] **Step 6: Commit**

```bash
git add DeliverTableClient/Pages/Checkout/
git commit -m "feat(client): surface billing-incomplete checkout error with profile link"
```

---

## Task 10: Format gate

**Why:** CLAUDE.md mandates `make format-check` passes before final commit.

- [ ] **Step 1: Run the format check**

```bash
make format-check
```

- [ ] **Step 2: If format-check failed, apply fixes**

```bash
make format-fix
```

If `make format-check` already passed (`Formatted 0 of N files`), skip steps 3 and 4 — there's nothing to commit.

- [ ] **Step 3: Build and run full test suite**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build
```

Expected: green except the documented Docker leak.

- [ ] **Step 4: Commit formatting fixes**

```bash
git add -A
git commit -m "style: apply formatting fixes"
```

---

## Final Verification

- [ ] **All commits land on the current branch:**

```bash
git log --oneline -12
```

You should see (newest first, depending on whether Task 10 ran):

```
<hash> style: apply formatting fixes                                      (skipped if not needed)
<hash> feat(client): surface billing-incomplete checkout error with profile link
<hash> feat(client): add billing address section to customer profile page
<hash> feat(server): populate customer invoice recipient address from profile
<hash> feat(server): require complete billing address before order creation
<hash> feat(server): persist trimmed billing fields in UpdateProfileAsync
<hash> feat(shared): add billing address fields to UpdateProfileRequest
<hash> feat(server): add BillingAddressHelper with completeness and formatting
<hash> feat(db): add migration AddUserBillingAddress
<hash> feat(server): add billing address properties to User entity
```

- [ ] **CI gate:**

```bash
make ci
```

Expected: full gate passes.
