# Restaurant Owner Registration With First Restaurant — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Combine restaurant-owner registration with first-restaurant creation into a single atomic, multi-step wizard. Move company identity (`CompanyName`, `VatNumber`) off `RestaurantOwner` and onto `Restaurant`.

**Architecture:** Multi-step wizard on the client posts a single combined payload to `POST /api/v1/auth/restaurant/register`. Server validates restaurant legal data first (SIRET + geocoding) before any DB writes; on success it creates the user, assigns the `RestaurantOwner` role, then creates the linked `Restaurant`. If anything in the create path fails after the user has been persisted, the user is deleted to preserve all-or-nothing semantics.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core 8, ASP.NET Identity, Blazor WebAssembly, NUnit 4, NSubstitute, PostgreSQL, Docker dev stack.

---

## Spec reference

`docs/superpowers/specs/2026-05-06-restaurant-owner-registration-with-first-restaurant-design.md`

## Pre-flight

The dev stack must be running before you start. The container is where `dotnet build`, `dotnet test`, and `dotnet ef migrations add` execute.

```bash
make dev-detach
```

After every implementation step that touches `.cs`, run:

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

After every TDD task, run:

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~<TestClass>"
```

---

## Task 1: Add `VatNumber` to restaurant DTOs (additive)

**Why:** Pure DTO additions. No business logic, no TDD. They unlock subsequent tasks but break nothing on their own.

**Files:**
- Modify: `DeliverTableSharedLibrary/Dtos/Restaurant/CreateRestaurantDto.cs`
- Modify: `DeliverTableSharedLibrary/Dtos/Restaurant/UpdateRestaurantDto.cs`
- Modify: `DeliverTableSharedLibrary/Dtos/Restaurant/RestaurantDto.cs`
- Modify: `DeliverTableSharedLibrary/Dtos/Restaurant/DetailedRestaurantDto.cs`

- [ ] **Step 1: Add `VatNumber` to `CreateRestaurantDto`**

In `CreateRestaurantDto.cs`, add this property between `LegalForm` (line 48) and `IsVatRegistered` (line 50):

```csharp
[MaxLength(20)]
public string? VatNumber { get; set; }
```

- [ ] **Step 2: Add `VatNumber` to `UpdateRestaurantDto`**

Same property, same place in `UpdateRestaurantDto.cs`.

```csharp
[MaxLength(20)]
public string? VatNumber { get; set; }
```

- [ ] **Step 3: Add `VatNumber` to `RestaurantDto`**

In `RestaurantDto.cs`, add at the end of the class:

```csharp
public string? VatNumber { get; set; }
```

- [ ] **Step 4: Add `VatNumber` to `DetailedRestaurantDto`**

In `DetailedRestaurantDto.cs`, add after `IsVatRegistered`:

```csharp
public string? VatNumber { get; set; }
```

- [ ] **Step 5: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: Build succeeded. 0 errors.

- [ ] **Step 6: Commit**

```bash
git add DeliverTableSharedLibrary/Dtos/Restaurant/
git commit -m "feat(shared): add VatNumber field to restaurant DTOs"
```

---

## Task 2: Add `VatNumber` column to `Restaurant` entity + mapper

**Why:** Mirror the DTO addition on the entity and mapper. Still additive — `RestaurantOwner` is untouched here.

**Files:**
- Modify: `DeliverTableInfrastructure/Models/Restaurant.cs`
- Modify: `DeliverTableInfrastructure/Data/ModelConfiguration/RestaurantConfiguration.cs`
- Modify: `DeliverTableServer/Mappers/RestaurantMappers.cs`

- [ ] **Step 1: Add property to `Restaurant` entity**

In `Restaurant.cs`, after `IsVatRegistered` (line 47), add:

```csharp
[MaxLength(20)]
public string? VatNumber { get; set; }
```

- [ ] **Step 2: Configure column in `RestaurantConfiguration`**

In `RestaurantConfiguration.cs`, inside `Configure(...)` after the `Balance` block (line 24), add:

```csharp
builder.Property(r => r.VatNumber).HasMaxLength(20);
```

- [ ] **Step 3: Map `VatNumber` in `ToDetailedDto`**

In `RestaurantMappers.cs`, add the line inside `ToDetailedDto` (after `IsVatRegistered = restaurantModel.IsVatRegistered,` on line 52):

```csharp
VatNumber = restaurantModel.VatNumber,
```

(Do not add to `ToDto` or `ToMapDto`. The summary `RestaurantDto` does not currently expose legal info; we keep the surface minimal. If a later UI needs it, a separate task will expand the mapper.)

- [ ] **Step 4: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: Build succeeded. 0 errors.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableInfrastructure/Models/Restaurant.cs \
        DeliverTableInfrastructure/Data/ModelConfiguration/RestaurantConfiguration.cs \
        DeliverTableServer/Mappers/RestaurantMappers.cs
git commit -m "feat(server): add VatNumber column to Restaurant entity and mapper"
```

---

## Task 3: Add `VatNumberRequiredWhenVatRegistered` error message

**Why:** Constant used by the next two TDD tasks. Constants don't need TDD (per CLAUDE.md). Bundled into the next commit.

**Files:**
- Modify: `DeliverTableServer/Constants/ErrorMessages.cs`

- [ ] **Step 1: Add the constant**

In `ErrorMessages.cs`, in the "SIRET / Legal" region (around line 122-124), add after `LegalFieldsRequired`:

```csharp
public const string VatNumberRequiredWhenVatRegistered = "Le numéro de TVA est requis pour une entreprise assujettie à la TVA";
```

- [ ] **Step 2: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: Build succeeded.

(No commit yet — this constant is committed alongside Task 4.)

---

## Task 4: TDD — Conditional VAT validation in `RestaurantService.CreateAsync`

**Why:** Validate that `VatNumber` is supplied when `IsVatRegistered = true`, and normalize to `null` otherwise.

**Files:**
- Modify: `DeliverTableTests/Server/Unit/Services/RestaurantServiceTests.cs`
- Modify: `DeliverTableServer/Services/RestaurantService.cs`

- [ ] **Step 1: Write failing test — VAT-registered without VatNumber returns error**

In `RestaurantServiceTests.cs`, inside the `#region CreateAsync - SIRET validation`, add:

```csharp
[Test]
public async Task CreateAsync_IsVatRegisteredTrueWithoutVatNumber_ReturnsError()
{
    var request = new CreateRestaurantDto
    {
        Name = "X",
        Description = "Une description",
        AdressLine1 = "1 rue Test",
        City = "Paris",
        ZipCode = "75001",
        Country = "France",
        Type = RestaurantType.Autre.ToString(),
        Siret = "73282932000074",
        LegalName = "X SAS",
        LegalAddress = "1 rue",
        LegalForm = "SAS",
        IsVatRegistered = true,
        VatNumber = null,
    };

    _geoLocationService
        .GetCoordinatesAsync(request.AdressLine1, request.City, request.ZipCode)
        .Returns((48.85, 2.35));

    var result = await _sut.CreateAsync(request, ownerId: 1, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.False);
    Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.VatNumberRequiredWhenVatRegistered));
}
```

- [ ] **Step 2: Write failing test — VAT-registered with VatNumber persists it**

```csharp
[Test]
public async Task CreateAsync_IsVatRegisteredTrueWithVatNumber_PersistsVatNumber()
{
    var request = new CreateRestaurantDto
    {
        Name = "Valid Restaurant",
        Description = "Une description",
        AdressLine1 = "1 rue Valid",
        City = "Paris",
        ZipCode = "75001",
        Country = "France",
        Type = RestaurantType.Autre.ToString(),
        Siret = "73282932000074",
        LegalName = "Valid SAS",
        LegalAddress = "1 rue Valid",
        LegalForm = "SAS",
        IsVatRegistered = true,
        VatNumber = "FR12345678901",
    };

    _geoLocationService
        .GetCoordinatesAsync(request.AdressLine1, request.City, request.ZipCode)
        .Returns((48.85, 2.35));

    var createdRestaurant = CreateRestaurant(id: 99, ownerId: 1);
    _restaurantRepository
        .CreateAsync(Arg.Any<Restaurant>(), Arg.Any<CancellationToken>())
        .Returns(callInfo => callInfo.Arg<Restaurant>());

    var result = await _sut.CreateAsync(request, ownerId: 1, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    await _restaurantRepository.Received(1).CreateAsync(
        Arg.Is<Restaurant>(r =>
            r.IsVatRegistered == true
            && r.VatNumber == "FR12345678901"),
        Arg.Any<CancellationToken>());
}
```

- [ ] **Step 3: Write failing test — non-VAT-registered persists VatNumber as null even if supplied**

```csharp
[Test]
public async Task CreateAsync_IsVatRegisteredFalseWithStaleVatNumber_PersistsAsNull()
{
    var request = new CreateRestaurantDto
    {
        Name = "Valid Restaurant",
        Description = "Une description",
        AdressLine1 = "1 rue Valid",
        City = "Paris",
        ZipCode = "75001",
        Country = "France",
        Type = RestaurantType.Autre.ToString(),
        Siret = "73282932000074",
        LegalName = "Valid SAS",
        LegalAddress = "1 rue Valid",
        LegalForm = "SAS",
        IsVatRegistered = false,
        VatNumber = "FR12345678901",
    };

    _geoLocationService
        .GetCoordinatesAsync(request.AdressLine1, request.City, request.ZipCode)
        .Returns((48.85, 2.35));

    _restaurantRepository
        .CreateAsync(Arg.Any<Restaurant>(), Arg.Any<CancellationToken>())
        .Returns(callInfo => callInfo.Arg<Restaurant>());

    var result = await _sut.CreateAsync(request, ownerId: 1, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    await _restaurantRepository.Received(1).CreateAsync(
        Arg.Is<Restaurant>(r =>
            r.IsVatRegistered == false
            && r.VatNumber == null),
        Arg.Any<CancellationToken>());
}
```

- [ ] **Step 4: Run tests to confirm they fail**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~RestaurantServiceTests.CreateAsync_IsVatRegistered"
```

Expected: 3 tests FAIL. The first should fail because no validation exists. The second/third should fail because the entity doesn't get `VatNumber` assigned yet.

- [ ] **Step 5: Implement validation and entity assignment**

In `RestaurantService.cs`, in `CreateAsync` (line 54-85), insert the conditional check after the `if (!legalAndCoords.IsSuccess) return legalAndCoords.Error!;` line and before constructing the `Restaurant`:

```csharp
if (dto.IsVatRegistered && string.IsNullOrWhiteSpace(dto.VatNumber))
    return new ServiceError(ErrorMessages.VatNumberRequiredWhenVatRegistered);
```

Then, in the `new Restaurant { ... }` initializer, after `IsVatRegistered = dto.IsVatRegistered,` (line 80), add:

```csharp
VatNumber = dto.IsVatRegistered ? dto.VatNumber : null,
```

The full updated initializer block looks like:

```csharp
var restaurant = new Restaurant
{
    Name = dto.Name,
    Description = dto.Description ?? string.Empty,
    AdressLine1 = dto.AdressLine1,
    AdressLine2 = dto.AdressLine2 ?? string.Empty,
    City = dto.City,
    ZipCode = dto.ZipCode,
    Type = ParseRestaurantType(dto.Type),
    Country = char.ToUpper(dto.Country[0]) + dto.Country[1..],
    OwnerId = ownerId,
    Longitude = coords.lon,
    Latitude = coords.lat,
    Siret = dto.Siret,
    LegalName = dto.LegalName,
    LegalAddress = dto.LegalAddress,
    LegalForm = dto.LegalForm,
    IsVatRegistered = dto.IsVatRegistered,
    VatNumber = dto.IsVatRegistered ? dto.VatNumber : null,
};
```

- [ ] **Step 6: Run tests to confirm they pass**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~RestaurantServiceTests"
```

Expected: All `RestaurantServiceTests` PASS, including the existing `CreateAsync_*` and `UpdateAsync_*` tests.

- [ ] **Step 7: Commit**

```bash
git add DeliverTableServer/Constants/ErrorMessages.cs \
        DeliverTableServer/Services/RestaurantService.cs \
        DeliverTableTests/Server/Unit/Services/RestaurantServiceTests.cs
git commit -m "feat(server): conditional VAT validation in RestaurantService.CreateAsync"
```

---

## Task 5: TDD — Conditional VAT validation in `RestaurantService.UpdateAsync`

**Why:** Mirror the rule on update so existing restaurants can't bypass it via the edit form.

**Files:**
- Modify: `DeliverTableTests/Server/Unit/Services/RestaurantServiceTests.cs`
- Modify: `DeliverTableServer/Services/RestaurantService.cs`

- [ ] **Step 1: Write failing tests**

Inside `#region UpdateAsync - SIRET validation`, add:

```csharp
[Test]
public async Task UpdateAsync_IsVatRegisteredTrueWithoutVatNumber_ReturnsError()
{
    var existing = CreateRestaurant(id: 1);
    _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

    _geoLocationService
        .GetCoordinatesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
        .Returns((48.85, 2.35));

    var request = new UpdateRestaurantDto
    {
        Name = "X",
        Description = "Une description",
        AdressLine1 = "1 rue Test",
        City = "Paris",
        ZipCode = "75001",
        Country = "France",
        Type = RestaurantType.Autre.ToString(),
        Siret = "73282932000074",
        LegalName = "X SAS",
        LegalAddress = "1 rue",
        LegalForm = "SAS",
        IsVatRegistered = true,
        VatNumber = null,
    };

    var result = await _sut.UpdateAsync(1, request, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.False);
    Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.VatNumberRequiredWhenVatRegistered));
}

[Test]
public async Task UpdateAsync_IsVatRegisteredFalseWithStaleVatNumber_PersistsAsNull()
{
    var existing = CreateRestaurant(id: 1);
    existing.VatNumber = "FR99999999999";
    _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

    _geoLocationService
        .GetCoordinatesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
        .Returns((48.85, 2.35));

    _restaurantRepository
        .UpdateAsync(Arg.Any<Restaurant>(), Arg.Any<CancellationToken>())
        .Returns(callInfo => callInfo.Arg<Restaurant>());

    var request = new UpdateRestaurantDto
    {
        Name = "Updated",
        Description = "desc",
        AdressLine1 = "1 rue Test",
        City = "Paris",
        ZipCode = "75001",
        Country = "France",
        Type = RestaurantType.Autre.ToString(),
        Siret = "73282932000074",
        LegalName = "X SAS",
        LegalAddress = "1 rue",
        LegalForm = "SAS",
        IsVatRegistered = false,
        VatNumber = "FR12345678901",
    };

    var result = await _sut.UpdateAsync(1, request, CancellationToken.None);

    Assert.That(result.IsSuccess, Is.True);
    await _restaurantRepository.Received(1).UpdateAsync(
        Arg.Is<Restaurant>(r => r.IsVatRegistered == false && r.VatNumber == null),
        Arg.Any<CancellationToken>());
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~RestaurantServiceTests.UpdateAsync_IsVatRegistered"
```

Expected: 2 tests FAIL.

- [ ] **Step 3: Implement**

In `RestaurantService.UpdateAsync` (line 87-118), insert the conditional check after `if (!legalAndCoords.IsSuccess) return legalAndCoords.Error!;` and before mutating `restaurant`:

```csharp
if (dto.IsVatRegistered && string.IsNullOrWhiteSpace(dto.VatNumber))
    return new ServiceError(ErrorMessages.VatNumberRequiredWhenVatRegistered);
```

Then, after `restaurant.IsVatRegistered = dto.IsVatRegistered;` (line 114), add:

```csharp
restaurant.VatNumber = dto.IsVatRegistered ? dto.VatNumber : null;
```

- [ ] **Step 4: Run tests to confirm they pass**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~RestaurantServiceTests"
```

Expected: All `RestaurantServiceTests` PASS.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableServer/Services/RestaurantService.cs \
        DeliverTableTests/Server/Unit/Services/RestaurantServiceTests.cs
git commit -m "feat(server): conditional VAT validation in RestaurantService.UpdateAsync"
```

---

## Task 6: Expose validation and create overloads on `IRestaurantService`

**Why:** `AuthService` needs to validate restaurant data before user creation (atomic semantics) and then create the restaurant without re-running external SIRET / geocode calls. Refactor `RestaurantService` to expose the validation step and a "validated create" overload, both reusable by `AuthService`.

**Files:**
- Modify: `DeliverTableServer/Services/Interfaces/IRestaurantService.cs`
- Modify: `DeliverTableServer/Services/RestaurantService.cs`

- [ ] **Step 1: Extend the interface**

In `IRestaurantService.cs`, add two methods after the existing `CreateAsync` (line 13):

```csharp
Task<ServiceResult<(double lat, double lon)>> ValidateLegalAndLocateAsync(
    string siret, string? legalName, string? legalAddress, string? legalForm,
    string addressLine1, string city, string zipCode);

Task<ServiceResult<RestaurantDto>> CreateValidatedAsync(
    CreateRestaurantDto dto, int ownerId, (double lat, double lon) coords, CancellationToken ct = default);
```

- [ ] **Step 2: Promote the private validator to public**

In `RestaurantService.cs`, change `private async Task<ServiceResult<(double lat, double lon)>> ValidateLegalAndLocateAsync(...)` (line 132) to `public`. The signature stays the same.

- [ ] **Step 3: Extract the construction step into `CreateValidatedAsync`**

In `RestaurantService.cs`, replace the body of `CreateAsync` to delegate to the new method. After your edit, the two methods look like this:

```csharp
public async Task<ServiceResult<RestaurantDto>> CreateAsync(
    CreateRestaurantDto dto, int ownerId, CancellationToken ct = default)
{
    var legalAndCoords = await ValidateLegalAndLocateAsync(
        dto.Siret, dto.LegalName, dto.LegalAddress, dto.LegalForm,
        dto.AdressLine1, dto.City, dto.ZipCode);
    if (!legalAndCoords.IsSuccess) return legalAndCoords.Error!;

    return await CreateValidatedAsync(dto, ownerId, legalAndCoords.Value, ct);
}

public async Task<ServiceResult<RestaurantDto>> CreateValidatedAsync(
    CreateRestaurantDto dto, int ownerId, (double lat, double lon) coords, CancellationToken ct = default)
{
    if (dto.IsVatRegistered && string.IsNullOrWhiteSpace(dto.VatNumber))
        return new ServiceError(ErrorMessages.VatNumberRequiredWhenVatRegistered);

    var restaurant = new Restaurant
    {
        Name = dto.Name,
        Description = dto.Description ?? string.Empty,
        AdressLine1 = dto.AdressLine1,
        AdressLine2 = dto.AdressLine2 ?? string.Empty,
        City = dto.City,
        ZipCode = dto.ZipCode,
        Type = ParseRestaurantType(dto.Type),
        Country = char.ToUpper(dto.Country[0]) + dto.Country[1..],
        OwnerId = ownerId,
        Longitude = coords.lon,
        Latitude = coords.lat,
        Siret = dto.Siret,
        LegalName = dto.LegalName,
        LegalAddress = dto.LegalAddress,
        LegalForm = dto.LegalForm,
        IsVatRegistered = dto.IsVatRegistered,
        VatNumber = dto.IsVatRegistered ? dto.VatNumber : null,
    };

    var created = await _restaurantRepository.CreateAsync(restaurant, ct);
    return created.ToDto();
}
```

- [ ] **Step 4: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: Build succeeded.

- [ ] **Step 5: Run all RestaurantService tests to confirm no regression**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~RestaurantServiceTests"
```

Expected: All tests PASS (the public surface change is internal — existing tests still cover behavior end-to-end via `CreateAsync` and `UpdateAsync`).

- [ ] **Step 6: Commit**

```bash
git add DeliverTableServer/Services/Interfaces/IRestaurantService.cs \
        DeliverTableServer/Services/RestaurantService.cs
git commit -m "refactor(server): expose ValidateLegalAndLocateAsync and CreateValidatedAsync on IRestaurantService"
```

---

## Task 7: Drop `CompanyName`/`VatNumber` from `RestaurantOwner`, restructure `RestaurantRegister`, rewire `AuthService.RegisterRestaurantAsync`

**Why:** This is the core orchestration change. It is unavoidably cross-cutting: dropping the entity columns breaks every reference, and the new `AuthService` flow uses the new `RestaurantRegister` shape and the new `IRestaurantService` methods. All of these must commit together to keep the build green.

**Files:**
- Modify: `DeliverTableInfrastructure/Models/RestaurantOwner.cs`
- Modify: `DeliverTableInfrastructure/Data/ModelConfiguration/RestaurantOwnerConfiguration.cs`
- Modify: `DeliverTableSharedLibrary/Dtos/Auth/RestaurantRegister.cs`
- Modify: `DeliverTableServer/Services/AuthService.cs`
- Modify: `DeliverTableTests/Server/Factories/ServerEntityFactory.cs`
- Modify: `DeliverTableTests/SharedLibrary/Factories/SharedLibraryDtoFactory.cs`
- Modify: `DeliverTableTests/SharedLibrary/Unit/Dtos/Auth/RestaurantRegisterValidationTests.cs`
- Modify: `DeliverTableTests/Client/Unit/Services/Auth/AuthServiceTests.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/AuthServiceTests.cs`

### 7.A — Schema and DTO changes

- [ ] **Step 1: Drop fields from `RestaurantOwner` entity**

Replace the contents of `DeliverTableInfrastructure/Models/RestaurantOwner.cs` with:

```csharp
namespace DeliverTableInfrastructure.Models;

public class RestaurantOwner
{
    public int Id { get; set; } // User.Id
    public User User { get; init; } = null!;
    public string ContactPhoneNumber { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

- [ ] **Step 2: Drop column configuration from `RestaurantOwnerConfiguration`**

Replace the contents of `DeliverTableInfrastructure/Data/ModelConfiguration/RestaurantOwnerConfiguration.cs` with:

```csharp
using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class RestaurantOwnerConfiguration : IEntityTypeConfiguration<RestaurantOwner>
{
    public void Configure(EntityTypeBuilder<RestaurantOwner> builder)
    {
        builder.HasKey(ro => ro.Id);

        builder.HasOne(ro => ro.User)
            .WithOne(u => u.RestaurantOwner)
            .HasForeignKey<RestaurantOwner>(ro => ro.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(u => u.ContactPhoneNumber).HasMaxLength(20);

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();
    }
}
```

- [ ] **Step 3: Restructure `RestaurantRegister`**

Replace the contents of `DeliverTableSharedLibrary/Dtos/Auth/RestaurantRegister.cs` with:

```csharp
using System.ComponentModel.DataAnnotations;
using DeliverTableSharedLibrary.Dtos.Restaurant;

namespace DeliverTableSharedLibrary.Dtos.Auth;

public class RestaurantRegister
{
    [Required(ErrorMessage = "Le prénom est requis")]
    [MaxLength(50, ErrorMessage = "Le prénom ne peut pas dépasser 50 caractères")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Le nom est requis")]
    [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Le numéro de téléphone est requis")]
    [MinLength(10, ErrorMessage = "Ce numéro de téléphone n'est pas valide")]
    [MaxLength(20, ErrorMessage = "Le numéro de téléphone ne peut pas dépasser 20 caractères")]
    public string ContactPhoneNumber { get; set; } = "";

    [Required(ErrorMessage = "L'adresse mail est requise")]
    [MaxLength(50, ErrorMessage = "L'adresse mail ne peut pas dépasser 50 caractères")]
    [EmailAddress(ErrorMessage = "L'adresse mail n'est pas valide")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(12, ErrorMessage = "Le mot de passe doit contenir au moins 12 caractères")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Veuillez confirmer votre mot de passe")]
    [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Les informations du restaurant sont requises")]
    public CreateRestaurantDto Restaurant { get; set; } = new();
}
```

- [ ] **Step 4: Update `ServerEntityFactory.CreateValidRestaurantOwner`**

In `DeliverTableTests/Server/Factories/ServerEntityFactory.cs`, replace the `CreateValidRestaurantOwner` body (around lines 47-58) with:

```csharp
public static User CreateValidRestaurantOwner(string? email = null)
{
    var user = CreateValidUser(email);
    user.RestaurantOwner = new RestaurantOwner
    {
        ContactPhoneNumber = "+32470123456"
    };
    user.Customer = new Customer();
    return user;
}
```

- [ ] **Step 4b: Update `SharedLibraryDtoFactory.CreateValidRestaurantRegister`**

In `DeliverTableTests/SharedLibrary/Factories/SharedLibraryDtoFactory.cs`, add `using DeliverTableSharedLibrary.Dtos.Restaurant;` and `using DeliverTableSharedLibrary.Enums;` at the top if not present, then replace the `CreateValidRestaurantRegister` body (around lines 29-39) with:

```csharp
public static RestaurantRegister CreateValidRestaurantRegister() => new()
{
    FirstName = "Marie",
    LastName = "Curie",
    ContactPhoneNumber = "+32470123456",
    Email = "contact@restaurant.be",
    Password = "SecurePass123!",
    ConfirmPassword = "SecurePass123!",
    Restaurant = new CreateRestaurantDto
    {
        Name = "Le Bon Restaurant",
        Description = "Une description",
        AdressLine1 = "1 rue Test",
        City = "Paris",
        ZipCode = "75001",
        Country = AvailableCountries.France.ToString(),
        Type = RestaurantType.Autre.ToString(),
        Siret = "73282932000074",
        LegalName = "Le Bon Restaurant SAS",
        LegalAddress = "1 rue Test",
        LegalForm = "SAS",
        IsVatRegistered = true,
        VatNumber = "FR12345678901",
    }
};
```

- [ ] **Step 4c: Delete the obsolete CompanyName and VatNumber regions from `RestaurantRegisterValidationTests`**

In `DeliverTableTests/SharedLibrary/Unit/Dtos/Auth/RestaurantRegisterValidationTests.cs`, delete the entire `#region CompanyName` block (lines 84-113) and the entire `#region VatNumber` block (lines 115-162). Both regions must be removed completely — these properties no longer exist on `RestaurantRegister`. The other regions (FirstName, LastName, ContactPhoneNumber, Email, Password, ConfirmPassword) remain unchanged.

After the edit, the file should still compile and run; only those 6 tests are removed.

- [ ] **Step 4d: Update client `AuthServiceTests` register-restaurant tests**

In `DeliverTableTests/Client/Unit/Services/Auth/AuthServiceTests.cs`, the three tests in `#region RegisterRestaurant` (around lines 242-308) instantiate `RestaurantRegister` with the removed `CompanyName` and `VatNumber` properties. Replace each of the three `new RestaurantRegister { ... }` literals with the new shape. The pattern for each is:

```csharp
new RestaurantRegister
{
    FirstName = "Marie",
    LastName = "Curie",
    ContactPhoneNumber = "+32470123456",
    Email = "contact@restaurant.be",
    Password = "SecurePass123!",
    ConfirmPassword = "SecurePass123!",
    Restaurant = new CreateRestaurantDto
    {
        Name = "Le Bon Restaurant",
        Description = "Une description",
        AdressLine1 = "1 rue Test",
        City = "Paris",
        ZipCode = "75001",
        Country = "France",
        Type = "Autre",
        Siret = "73282932000074",
        LegalName = "Le Bon Restaurant SAS",
        LegalAddress = "1 rue Test",
        LegalForm = "SAS",
        IsVatRegistered = true,
        VatNumber = "FR12345678901",
    }
}
```

Add `using DeliverTableSharedLibrary.Dtos.Restaurant;` at the top of the file if not already present.

The behavior of these tests does not change (they only assert HTTP layer behavior — endpoint, response handling). Only the request payload literal changes shape.

### 7.B — Rewire `AuthService` (TDD)

The flow becomes: validate restaurant first (before any DB write) → create user → assign role → create restaurant. On any failure after user creation, delete the user.

`AuthService` gains a constructor dependency on `IRestaurantService`. Tests must mock it.

- [ ] **Step 5: Add `IRestaurantService` dependency to `AuthService`**

In `DeliverTableServer/Services/AuthService.cs`, change the primary constructor to add the parameter:

```csharp
public sealed class AuthService(
    IUserRepository userRepository,
    ITokenService tokenService,
    IEmailJobService emailJobService,
    IRestaurantService restaurantService
) : IAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IEmailJobService _emailJobService = emailJobService;
    private readonly IRestaurantService _restaurantService = restaurantService;
    private readonly string _defaultRole = nameof(UserRole.Customer);
```

- [ ] **Step 6: Write failing test — email already used → no user created**

Add this `using` to the top of `AuthServiceTests.cs` if not already there:

```csharp
using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using DeliverTableServer.Constants;
using DeliverTableServer.Services.Interfaces;
```

Update the `SetUp` and add tests. The full `AuthServiceTests.cs` becomes:

```csharp
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Auth;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Server.Factories;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class AuthServiceTests
{
    private IUserRepository _userRepository = null!;
    private ITokenService _tokenService = null!;
    private IEmailJobService _emailJobService = null!;
    private IRestaurantService _restaurantService = null!;
    private AuthService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _tokenService = Substitute.For<ITokenService>();
        _emailJobService = Substitute.For<IEmailJobService>();
        _restaurantService = Substitute.For<IRestaurantService>();
        _sut = new AuthService(_userRepository, _tokenService, _emailJobService, _restaurantService);
    }

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

    #region RegisterRestaurantAsync

    [Test]
    public async Task RegisterRestaurantAsync_EmailAlreadyUsed_ReturnsError_NoUserCreated()
    {
        var request = BuildValidRestaurantRegister();
        _userRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.RegisterRestaurantAsync(request, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.EmailAlreadyUsed));
        await _userRepository.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<string>());
        await _restaurantService.DidNotReceive().ValidateLegalAndLocateAsync(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task RegisterRestaurantAsync_RestaurantValidationFails_NoUserCreated()
    {
        var request = BuildValidRestaurantRegister();
        _userRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _restaurantService.ValidateLegalAndLocateAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new ServiceError(ErrorMessages.SiretInvalid));

        var result = await _sut.RegisterRestaurantAsync(request, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.SiretInvalid));
        await _userRepository.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<string>());
    }

    [Test]
    public async Task RegisterRestaurantAsync_ValidPayload_CreatesUserOwnerRoleAndRestaurant()
    {
        var request = BuildValidRestaurantRegister();
        _userRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _restaurantService.ValidateLegalAndLocateAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((48.85, 2.35));
        _userRepository.CreateAsync(Arg.Any<User>(), Arg.Any<string>())
            .Returns((true, Enumerable.Empty<string>()));
        _userRepository.AddToRoleAsync(Arg.Any<User>(), nameof(UserRole.RestaurantOwner))
            .Returns((true, Enumerable.Empty<string>()));
        _restaurantService.CreateValidatedAsync(
                Arg.Any<CreateRestaurantDto>(), Arg.Any<int>(),
                Arg.Any<(double lat, double lon)>(), Arg.Any<CancellationToken>())
            .Returns(new RestaurantDto { Id = 99, Name = "Test" });
        _userRepository.GetPrimaryRoleAsync(Arg.Any<User>())
            .Returns(nameof(UserRole.RestaurantOwner));
        _tokenService.GenerateToken(Arg.Any<User>(), Arg.Any<string>())
            .Returns("test-token");

        var result = await _sut.RegisterRestaurantAsync(request, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _userRepository.Received(1).CreateAsync(Arg.Any<User>(), request.Password);
        await _userRepository.Received(1).AddToRoleAsync(Arg.Any<User>(), nameof(UserRole.RestaurantOwner));
        await _restaurantService.Received(1).CreateValidatedAsync(
            request.Restaurant, Arg.Any<int>(),
            Arg.Is<(double lat, double lon)>(c => c.lat == 48.85 && c.lon == 2.35),
            Arg.Any<CancellationToken>());
        await _emailJobService.Received(1).QueueWelcomeEmailAsync(request.Email, Arg.Any<string>());
    }

    [Test]
    public async Task RegisterRestaurantAsync_RestaurantInsertFails_DeletesUser()
    {
        var request = BuildValidRestaurantRegister();
        _userRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _restaurantService.ValidateLegalAndLocateAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((48.85, 2.35));
        _userRepository.CreateAsync(Arg.Any<User>(), Arg.Any<string>())
            .Returns((true, Enumerable.Empty<string>()));
        _userRepository.AddToRoleAsync(Arg.Any<User>(), nameof(UserRole.RestaurantOwner))
            .Returns((true, Enumerable.Empty<string>()));
        _restaurantService.CreateValidatedAsync(
                Arg.Any<CreateRestaurantDto>(), Arg.Any<int>(),
                Arg.Any<(double lat, double lon)>(), Arg.Any<CancellationToken>())
            .Returns(new ServiceError(ErrorMessages.InternalError, 500));
        _userRepository.DeleteAsync(Arg.Any<User>())
            .Returns((true, Enumerable.Empty<string>()));

        var result = await _sut.RegisterRestaurantAsync(request, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        await _userRepository.Received(1).DeleteAsync(Arg.Any<User>());
        await _emailJobService.DidNotReceive().QueueWelcomeEmailAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    private static RestaurantRegister BuildValidRestaurantRegister() => new()
    {
        FirstName = "Jean",
        LastName = "Dupont",
        Email = "owner@example.com",
        ContactPhoneNumber = "+33612345678",
        Password = "SecurePass1234!",
        ConfirmPassword = "SecurePass1234!",
        Restaurant = new CreateRestaurantDto
        {
            Name = "Le Bon Resto",
            Description = "Une description",
            AdressLine1 = "1 rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "France",
            Type = RestaurantType.Autre.ToString(),
            Siret = "73282932000074",
            LegalName = "Le Bon Resto SAS",
            LegalAddress = "1 rue Test",
            LegalForm = "SAS",
            IsVatRegistered = true,
            VatNumber = "FR12345678901",
        }
    };
}
```

- [ ] **Step 7: Run tests to confirm they fail**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~AuthServiceTests.RegisterRestaurantAsync"
```

Expected: 4 tests FAIL (current `RegisterRestaurantAsync` ignores `IRestaurantService` and references removed fields, so it will not compile until step 8 — at this point the *test project* itself may fail to build).

If the build fails: that confirms the tests reference the new shape correctly. Proceed to step 8 to align the production code, then re-run.

- [ ] **Step 8: Rewire `AuthService.RegisterRestaurantAsync`**

In `AuthService.cs`, replace the body of `RegisterRestaurantAsync` (current lines 66-99) with:

```csharp
public async Task<ServiceResult<ConnectionResponse>> RegisterRestaurantAsync(RestaurantRegister request, CancellationToken ct = default)
{
    var normalizedEmail = request.Email?.ToUpperInvariant();
    if (await _userRepository.EmailExistsAsync(normalizedEmail!, ct))
        return new ServiceError(ErrorMessages.EmailAlreadyUsed);

    var legalAndCoords = await _restaurantService.ValidateLegalAndLocateAsync(
        request.Restaurant.Siret,
        request.Restaurant.LegalName,
        request.Restaurant.LegalAddress,
        request.Restaurant.LegalForm,
        request.Restaurant.AdressLine1,
        request.Restaurant.City,
        request.Restaurant.ZipCode);
    if (!legalAndCoords.IsSuccess) return legalAndCoords.Error!;

    var user = new User
    {
        UserName = request.Email,
        Email = request.Email,
        FirstName = request.FirstName,
        LastName = request.LastName,
        RestaurantOwner = new RestaurantOwner
        {
            ContactPhoneNumber = request.ContactPhoneNumber
        },
        Customer = new Customer()
    };

    var (created, errors) = await _userRepository.CreateAsync(user, request.Password);
    if (!created)
        return ServiceError.FromIdentityErrors(errors);

    var (roleOk, _) = await _userRepository.AddToRoleAsync(user, nameof(UserRole.RestaurantOwner));
    if (!roleOk)
    {
        await _userRepository.DeleteAsync(user);
        return new ServiceError(ErrorMessages.InternalError, 500);
    }

    var restaurantResult = await _restaurantService.CreateValidatedAsync(
        request.Restaurant, user.Id, legalAndCoords.Value, ct);
    if (!restaurantResult.IsSuccess)
    {
        await _userRepository.DeleteAsync(user);
        return restaurantResult.Error!;
    }

    var ownerName = user.GetFullName();
    await _emailJobService.QueueWelcomeEmailAsync(user.Email!, ownerName);

    return await BuildConnectionResponse(user);
}
```

- [ ] **Step 9: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

Expected: Build succeeded. If you get errors about unused `using` statements or other artifacts of the rewrite, fix them in place.

- [ ] **Step 10: Run all tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --filter "FullyQualifiedName~AuthServiceTests"
```

Expected: All `AuthServiceTests` PASS (5 tests: 1 existing UpdateProfile + 4 new RegisterRestaurantAsync).

- [ ] **Step 11: Run the full test suite to catch regressions**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj
```

Expected: All tests PASS except the known pre-existing `AppEnvironmentTests.Load_AppliesDefaults_WhenOptionalVarsAreMissing` failure (mentioned in CLAUDE.md). If any *other* test fails (e.g. integration tests that reference `RestaurantOwner.CompanyName`), update the test fixture to drop the field reference.

- [ ] **Step 12: Commit**

```bash
git add DeliverTableInfrastructure/Models/RestaurantOwner.cs \
        DeliverTableInfrastructure/Data/ModelConfiguration/RestaurantOwnerConfiguration.cs \
        DeliverTableSharedLibrary/Dtos/Auth/RestaurantRegister.cs \
        DeliverTableServer/Services/AuthService.cs \
        DeliverTableTests/Server/Factories/ServerEntityFactory.cs \
        DeliverTableTests/SharedLibrary/Factories/SharedLibraryDtoFactory.cs \
        DeliverTableTests/SharedLibrary/Unit/Dtos/Auth/RestaurantRegisterValidationTests.cs \
        DeliverTableTests/Client/Unit/Services/Auth/AuthServiceTests.cs \
        DeliverTableTests/Server/Unit/Services/AuthServiceTests.cs
git commit -m "feat(server): combine restaurant owner registration with first-restaurant creation"
```

---

## Task 8: EF migration `OwnerRestaurantCompanyFieldsShift`

**Why:** Apply the schema changes to the actual database. Generates a single migration that drops two columns from `RestaurantOwners` and adds `VatNumber` to `Restaurants`.

**Files:**
- Create: `DeliverTableInfrastructure/Migrations/<timestamp>_OwnerRestaurantCompanyFieldsShift.cs` (+ `.Designer.cs`) — generated.
- Modify: `DeliverTableInfrastructure/Migrations/DeliverTableContextModelSnapshot.cs` — auto-updated.

- [ ] **Step 1: Generate the migration**

```bash
docker compose -f docker-dev.yaml exec backend dotnet ef migrations add OwnerRestaurantCompanyFieldsShift \
    --project /src/DeliverTableInfrastructure \
    --startup-project /src/DeliverTableServer \
    --output-dir Migrations
```

Expected: three new/modified files. The new timestamp prefix should be ≥ `20260504084520` (the current latest, `AddUserBillingAddress`).

- [ ] **Step 2: Verify the generated migration body**

Open the new `<timestamp>_OwnerRestaurantCompanyFieldsShift.cs`. The `Up` method should contain:

- Two `migrationBuilder.DropColumn(...)` calls on table `"RestaurantOwners"` for columns `"CompanyName"` and `"VatNumber"`.
- One `migrationBuilder.AddColumn<string>(...)` on table `"Restaurants"` for `"VatNumber"`, with `nullable: true` and `maxLength: 20`.

The `Down` method should reverse both.

If anything else appears (e.g. unrelated columns), revert the migration with:

```bash
docker compose -f docker-dev.yaml exec backend dotnet ef migrations remove \
    --project /src/DeliverTableInfrastructure \
    --startup-project /src/DeliverTableServer
```

investigate the snapshot drift, and try again.

- [ ] **Step 3: Apply the migration to the dev database**

```bash
make dev-migrate
```

Expected: `Done.` and no errors.

- [ ] **Step 4: Sanity-check the schema**

```bash
docker compose -f docker-dev.yaml exec postgres psql -U postgres -d delivertable -c '\d "RestaurantOwners"' \
  | grep -E 'CompanyName|VatNumber|ContactPhoneNumber'
```

Expected: only `ContactPhoneNumber` is listed for `RestaurantOwners`.

```bash
docker compose -f docker-dev.yaml exec postgres psql -U postgres -d delivertable -c '\d "Restaurants"' \
  | grep VatNumber
```

Expected: `VatNumber | character varying(20) |` (nullable).

(If the postgres credentials or db name in your local stack differ from the defaults, adjust the `-U` / `-d` arguments — the dev stack reads them from `.env`. The check is optional confirmation; the migration applying cleanly is sufficient.)

- [ ] **Step 5: Commit**

```bash
git add DeliverTableInfrastructure/Migrations/
git commit -m "feat(db): migration for owner-restaurant company fields shift"
```

---

## Task 9: Convert `RestaurantRegisterPage` into a multi-step wizard

**Why:** The single-page form is replaced by a 2-step wizard that posts the new combined `RestaurantRegister` shape (account fields + nested `Restaurant` of type `CreateRestaurantDto`).

This task is non-TDD (Blazor UI). After implementation you must verify visually in the browser per CLAUDE.md.

**Files:**
- Modify: `DeliverTableClient/Pages/Auth/RestaurantRegister/RestaurantRegisterPage.razor`
- Modify: `DeliverTableClient/Pages/Auth/RestaurantRegister/RestaurantRegisterPage.razor.scss`

- [ ] **Step 1: Replace the page contents with the wizard**

Replace the entire body of `RestaurantRegisterPage.razor` (keep the `@page`, `@layout`, `@inject` directives unchanged). The full new file:

```razor
@page "/owner/register"
@layout AuthLayout
@using DeliverTableSharedLibrary.Dtos.Auth
@using DeliverTableSharedLibrary.Dtos.Restaurant
@using DeliverTableSharedLibrary.Enums
@inject AuthService AuthService
@inject NavigationManager Nav

<PageTitle>Inscription Gérant — DeliverTable</PageTitle>

<div class="auth-split">
    <section class="auth-showcase">
        <div class="showcase-cards">
            <div class="showcase-card showcase-card--1">
                <div class="showcase-card-icon showcase-card-icon--coral">
                    <Blazicon Svg="Lucide.UtensilsCrossed" />
                </div>
                <div>
                    <p class="showcase-card-name">Truffle Tagliatelle</p>
                    <p class="showcase-card-meta">La Bottega · Italian</p>
                    <p class="showcase-card-price">€24.00</p>
                    <p class="showcase-card-rating">★ 4.9</p>
                </div>
            </div>

            <div class="showcase-card showcase-card--2">
                <div class="showcase-card-icon showcase-card-icon--sage">
                    <Blazicon Svg="Lucide.Flame" />
                </div>
                <div>
                    <p class="showcase-card-name">Wagyu Steak</p>
                    <p class="showcase-card-meta">Le Boeuf · Steakhouse</p>
                    <p class="showcase-card-price">€42.00</p>
                </div>
            </div>

            <div class="showcase-card showcase-card--3">
                <div class="showcase-card-icon showcase-card-icon--gold">
                    <Blazicon Svg="Lucide.Globe" />
                </div>
                <div>
                    <p class="showcase-card-name">Omakase Set</p>
                    <p class="showcase-card-meta">Sakura · Japanese</p>
                    <p class="showcase-card-price">€38.00</p>
                    <p class="showcase-card-rating">★ 5.0</p>
                </div>
            </div>
        </div>

        <div class="showcase-content">
            <h1 class="showcase-tagline">
                Every meal<br/>
                deserves a <span class="text-accent">proper<br/>table.</span>
            </h1>
            <p class="showcase-description">
                Discover restaurants, book your table, pre-order your meal
                — all in one place. From intimate dinners to grand events.
            </p>
        </div>
    </section>

    <section class="auth-panel">
        <div class="auth-panel-content">
            <h2 class="auth-heading">Espace gérant</h2>
            <p class="auth-subheading">Référencez votre restaurant et touchez de nouveaux clients</p>

            <div class="wizard-steps">
                <span class="wizard-step @(_currentStep == 1 ? "active" : "")">1. Votre compte</span>
                <span class="wizard-step @(_currentStep == 2 ? "active" : "")">2. Premier restaurant</span>
            </div>

            <EditForm Model="@_model" OnValidSubmit="HandleValidSubmit" class="auth-form">
                <DataAnnotationsValidator />

                @if (_currentStep == 1)
                {
                    <div class="auth-field">
                        <label class="auth-label">Prénom</label>
                        <InputText @bind-Value="_model.FirstName" class="auth-input" placeholder="Votre prénom" />
                        <ValidationMessage For="@(() => _model.FirstName)" class="validation-message" />
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">Nom</label>
                        <InputText @bind-Value="_model.LastName" class="auth-input" placeholder="Votre nom" />
                        <ValidationMessage For="@(() => _model.LastName)" class="validation-message" />
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">Adresse email</label>
                        <InputText @bind-Value="_model.Email" class="auth-input" placeholder="votre@email.com" />
                        <ValidationMessage For="@(() => _model.Email)" class="validation-message" />
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">N° de téléphone</label>
                        <InputText @bind-Value="_model.ContactPhoneNumber" class="auth-input" placeholder="+33 6 00 00 00 00" />
                        <ValidationMessage For="@(() => _model.ContactPhoneNumber)" class="validation-message" />
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">Mot de passe (12 caractères min.)</label>
                        <InputText @bind-Value="_model.Password" type="password" class="auth-input" placeholder="••••••••••••" />
                        <ValidationMessage For="@(() => _model.Password)" class="validation-message" />
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">Confirmer le mot de passe</label>
                        <InputText @bind-Value="_model.ConfirmPassword" type="password" class="auth-input" placeholder="••••••••••••" />
                        <ValidationMessage For="@(() => _model.ConfirmPassword)" class="validation-message" />
                    </div>

                    @if (!string.IsNullOrEmpty(_errorMessage))
                    {
                        <div class="alert alert-danger">@_errorMessage</div>
                    }

                    <button type="button" class="auth-submit" @onclick="GoToStep2">
                        Suivant
                    </button>
                }
                else
                {
                    <div class="auth-field">
                        <label class="auth-label">Nom du restaurant</label>
                        <InputText @bind-Value="_model.Restaurant.Name" class="auth-input" placeholder="Nom du restaurant" />
                        <ValidationMessage For="@(() => _model.Restaurant.Name)" class="validation-message" />
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">Description</label>
                        <InputTextArea rows="3" @bind-Value="_model.Restaurant.Description" class="auth-input" placeholder="Description courte" />
                        <ValidationMessage For="@(() => _model.Restaurant.Description)" class="validation-message" />
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">Type de restaurant</label>
                        <InputSelect @bind-Value="_model.Restaurant.Type" class="auth-input">
                            @foreach (var t in Enum.GetValues<RestaurantType>())
                            {
                                <option value="@t">@t.ToString()</option>
                            }
                        </InputSelect>
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">Adresse</label>
                        <InputText @bind-Value="_model.Restaurant.AdressLine1" class="auth-input" placeholder="N° et rue" />
                        <ValidationMessage For="@(() => _model.Restaurant.AdressLine1)" class="validation-message" />
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">Complément d'adresse</label>
                        <InputText @bind-Value="_model.Restaurant.AdressLine2" class="auth-input" placeholder="Optionnel" />
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">Ville</label>
                        <InputText @bind-Value="_model.Restaurant.City" class="auth-input" placeholder="Ville" />
                        <ValidationMessage For="@(() => _model.Restaurant.City)" class="validation-message" />
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">Code postal</label>
                        <InputText @bind-Value="_model.Restaurant.ZipCode" class="auth-input" placeholder="75001" maxlength="5" />
                        <ValidationMessage For="@(() => _model.Restaurant.ZipCode)" class="validation-message" />
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">Pays</label>
                        <InputSelect @bind-Value="_model.Restaurant.Country" class="auth-input">
                            @foreach (var c in Enum.GetValues<AvailableCountries>())
                            {
                                <option value="@c">@c.ToString()</option>
                            }
                        </InputSelect>
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">Numéro SIRET</label>
                        <InputText @bind-Value="_model.Restaurant.Siret" class="auth-input" placeholder="14 chiffres" maxlength="14" />
                        <ValidationMessage For="@(() => _model.Restaurant.Siret)" class="validation-message" />
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">Raison sociale</label>
                        <InputText @bind-Value="_model.Restaurant.LegalName" class="auth-input" placeholder="Raison sociale" />
                        <ValidationMessage For="@(() => _model.Restaurant.LegalName)" class="validation-message" />
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">Adresse légale</label>
                        <InputText @bind-Value="_model.Restaurant.LegalAddress" class="auth-input" placeholder="Adresse légale" />
                        <ValidationMessage For="@(() => _model.Restaurant.LegalAddress)" class="validation-message" />
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">Forme juridique</label>
                        <InputSelect @bind-Value="_model.Restaurant.LegalForm" class="auth-input">
                            <option value="">-- Sélectionnez une forme juridique --</option>
                            <option value="SAS">SAS</option>
                            <option value="SARL">SARL</option>
                            <option value="EURL">EURL</option>
                            <option value="EI">EI</option>
                            <option value="SA">SA</option>
                        </InputSelect>
                        <ValidationMessage For="@(() => _model.Restaurant.LegalForm)" class="validation-message" />
                    </div>

                    <div class="auth-field">
                        <label class="auth-label">
                            <InputCheckbox @bind-Value="_model.Restaurant.IsVatRegistered" />
                            <span>Entreprise assujettie à la TVA</span>
                        </label>
                    </div>

                    @if (_model.Restaurant.IsVatRegistered)
                    {
                        <div class="auth-field">
                            <label class="auth-label">Numéro de TVA</label>
                            <InputText @bind-Value="_model.Restaurant.VatNumber" class="auth-input" placeholder="FR12345678901" maxlength="20" />
                            <ValidationMessage For="@(() => _model.Restaurant.VatNumber)" class="validation-message" />
                        </div>
                    }

                    @if (!string.IsNullOrEmpty(_errorMessage))
                    {
                        <div class="alert alert-danger">@_errorMessage</div>
                    }

                    <div class="wizard-actions">
                        <button type="button" class="auth-submit auth-submit--secondary" @onclick="GoToStep1">
                            Précédent
                        </button>
                        <button type="submit" class="auth-submit" disabled="@_isSubmitting">
                            @if (_isSubmitting)
                            {
                                <span class="auth-spinner"></span>
                                <span>Création du compte...</span>
                            }
                            else
                            {
                                <span>Créer mon compte</span>
                            }
                        </button>
                    </div>
                }
            </EditForm>

            <p class="auth-footer">
                Déjà un compte ? <a href="/login">Connectez-vous</a>
            </p>
        </div>
    </section>
</div>

@code {
    private RestaurantRegister _model = new();
    private int _currentStep = 1;
    private bool _isSubmitting;
    private string? _errorMessage;

    private static readonly string[] Step1Fields =
    {
        nameof(RestaurantRegister.FirstName),
        nameof(RestaurantRegister.LastName),
        nameof(RestaurantRegister.Email),
        nameof(RestaurantRegister.ContactPhoneNumber),
        nameof(RestaurantRegister.Password),
        nameof(RestaurantRegister.ConfirmPassword),
    };

    private void GoToStep1() => _currentStep = 1;

    private void GoToStep2()
    {
        _errorMessage = null;
        var ctx = new EditContext(_model);
        ctx.Validate();
        var hasStep1Errors = ctx.GetValidationMessages()
            .Any() && Step1Fields.Any(f => ctx.GetValidationMessages(new FieldIdentifier(_model, f)).Any());
        if (hasStep1Errors)
        {
            _errorMessage = "Merci de corriger les champs en erreur avant de continuer.";
            return;
        }
        _currentStep = 2;
    }

    private async Task HandleValidSubmit()
    {
        _isSubmitting = true;
        _errorMessage = null;

        try
        {
            var response = await AuthService.RegisterRestaurant(_model);

            if (response is null)
            {
                _errorMessage = "Une erreur inattendue est survenue. Veuillez réessayer.";
                return;
            }

            if (response.Success)
            {
                Nav.NavigateTo("/");
                return;
            }

            _errorMessage = response.Error;

            if (!string.IsNullOrEmpty(_errorMessage)
                && _errorMessage.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                _currentStep = 1;
            }
        }
        catch
        {
            _errorMessage = "Une erreur inattendue est survenue. Veuillez réessayer.";
        }
        finally
        {
            _isSubmitting = false;
        }
    }
}
```

> Note on `EditContext` import: `EditContext` and `FieldIdentifier` come from `Microsoft.AspNetCore.Components.Forms`, which is already globally imported in `_Imports.razor`. If the build complains about missing types, add `@using Microsoft.AspNetCore.Components.Forms` to the page.

- [ ] **Step 2: Add wizard styles**

Append to `RestaurantRegisterPage.razor.scss` (do not duplicate existing rules — append at the end):

```scss
.wizard-steps {
    display: flex;
    gap: 1rem;
    margin-bottom: 1.5rem;
    font-size: 0.875rem;

    .wizard-step {
        padding: 0.5rem 0.75rem;
        border-radius: 999px;
        background: rgba(0, 0, 0, 0.05);
        color: rgba(0, 0, 0, 0.5);
        font-weight: 500;

        &.active {
            background: var(--accent, #E85D3A);
            color: #fff;
        }
    }
}

.wizard-actions {
    display: flex;
    gap: 0.75rem;

    .auth-submit {
        flex: 1;
    }

    .auth-submit--secondary {
        background: transparent;
        color: inherit;
        border: 1px solid rgba(0, 0, 0, 0.15);
    }
}
```

- [ ] **Step 3: Build the client**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTableClient/DeliverTableClient.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Manual UI verification**

The dev stack should already be running (`make dev-detach` from pre-flight). Open the app in your browser:

1. Navigate to `/owner/register`. You should see step 1 with the wizard pill at top showing "1. Votre compte" highlighted.
2. Try clicking "Suivant" with empty fields — should stay on step 1 with validation messages and an error banner.
3. Fill all step-1 fields, click "Suivant" — wizard should advance to step 2 with the "2. Premier restaurant" pill highlighted.
4. Click "Précédent" — should return to step 1 with the data still filled in.
5. Click "Suivant" again, fill the restaurant fields including a valid SIRET (`73282932000074` from existing tests), tick "Entreprise assujettie à la TVA", confirm the VAT number field appears, fill it.
6. Click "Créer mon compte" — should submit and navigate to `/`. Confirm in the database (or via the existing user-restaurants endpoint) that one restaurant was created with the new owner as `OwnerId`.
7. Repeat with an invalid SIRET (e.g. `12345678900012`) — should show an error and remain on step 2.
8. Repeat with an existing email — should show "email" error and bounce back to step 1.

If any of the above fails, fix and re-test before committing. Use the browser's network tab to inspect the request payload — confirm it has the nested `Restaurant` property.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableClient/Pages/Auth/RestaurantRegister/RestaurantRegisterPage.razor \
        DeliverTableClient/Pages/Auth/RestaurantRegister/RestaurantRegisterPage.razor.scss
git commit -m "feat(client): combined restaurant owner registration wizard"
```

---

## Task 10: Add `VatNumber` field to `RestaurantCreationForm` (standalone create-restaurant page)

**Why:** Existing owners adding additional restaurants use this form. It must collect the new `VatNumber` field, conditionally on `IsVatRegistered`.

**Files:**
- Modify: `DeliverTableClient/Components/Forms/RestaurantCreation/RestaurantCreationForm.razor`

- [ ] **Step 1: Wrap the VAT checkbox and add a conditional VAT-number field**

In `RestaurantCreationForm.razor`, replace the existing `IsVatRegistered` block (lines 99-104) with:

```razor
<div class="col gap-1 mb-1">
    <label class="row gap-1">
        <InputCheckbox @bind-Value="_creationDto.IsVatRegistered" id="restaurant-vat-registered" />
        <span>TVA applicable</span>
    </label>
</div>

@if (_creationDto.IsVatRegistered)
{
    <div class="col gap-1 mb-1">
        <label for="restaurant-vat-number">Numéro de TVA</label>
        <InputText id="restaurant-vat-number" @bind-Value="_creationDto.VatNumber" class="app-input" placeholder="FR12345678901" maxlength="20" />
        <ValidationMessage For="@(() => _creationDto.VatNumber)" class="validation-message" />
    </div>
}
```

- [ ] **Step 2: Build the client**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTableClient/DeliverTableClient.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Manual UI verification**

1. Log in as an existing restaurant owner (or use the wizard from Task 9 to create one).
2. Navigate to `/restaurant/create`.
3. Fill the form, untick "TVA applicable" — confirm the VAT-number field is hidden.
4. Tick it — confirm the field appears. Submit with VatNumber empty — server should return the conditional-VAT error.
5. Submit with VatNumber filled — restaurant should be created.

- [ ] **Step 4: Commit**

```bash
git add DeliverTableClient/Components/Forms/RestaurantCreation/RestaurantCreationForm.razor
git commit -m "feat(client): add VatNumber field to standalone restaurant creation"
```

---

## Task 11: Final formatting and full test pass

**Why:** Per CLAUDE.md end-of-feature checklist.

- [ ] **Step 1: Format check**

```bash
make format-check
```

If this exits non-zero, run:

```bash
make format-fix
```

- [ ] **Step 2: Full test suite**

```bash
make test
```

Expected: all tests PASS except the documented `AppEnvironmentTests.Load_AppliesDefaults_WhenOptionalVarsAreMissing` Docker-only failure.

- [ ] **Step 3: Commit any formatting fixes (if `format-fix` changed anything)**

```bash
git status
# if there are pending diffs from format-fix only:
git add -u
git commit -m "style: apply formatting fixes"
```

If `format-check` was already clean, skip this step.

---

## Done

All commits should be on the `feature/account-restaurant-owner` branch. Verify with:

```bash
git log --oneline main..HEAD
```

You should see 7-8 commits in this order:

1. `feat(shared): add VatNumber field to restaurant DTOs`
2. `feat(server): add VatNumber column to Restaurant entity and mapper`
3. `feat(server): conditional VAT validation in RestaurantService.CreateAsync`
4. `feat(server): conditional VAT validation in RestaurantService.UpdateAsync`
5. `refactor(server): expose ValidateLegalAndLocateAsync and CreateValidatedAsync on IRestaurantService`
6. `feat(server): combine restaurant owner registration with first-restaurant creation`
7. `feat(db): migration for owner-restaurant company fields shift`
8. `feat(client): combined restaurant owner registration wizard`
9. `feat(client): add VatNumber field to standalone restaurant creation`
10. (optional) `style: apply formatting fixes`
