# Restaurant Account & Balance Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a per-restaurant financial account with balance tracking, transaction ledger, platform commission on delivered orders, and a withdrawal mechanism.

**Architecture:** Extends the existing Restaurant entity with a `Balance` field and adds a new `RestaurantTransaction` entity as a ledger. Crediting happens inside `OrderService.UpdateStatusAsync` when an order reaches `Delivered` status. A new `RestaurantAccountController` + `RestaurantAccountService` + `RestaurantTransactionRepository` stack handles account queries and withdrawals.

**Tech Stack:** .NET 10, EF Core, PostgreSQL, NUnit 4 + NSubstitute

---

## Task 1: Add `TransactionType` Enum

**Files:**
- Create: `DeliverTableSharedLibrary/Enums/TransactionType.cs`

**Step 1: Create the enum**

```csharp
namespace DeliverTableSharedLibrary.Enums;

public enum TransactionType
{
    Credit,
    Withdrawal
}
```

**Step 2: Commit**

```bash
git add DeliverTableSharedLibrary/Enums/TransactionType.cs
git commit -m "feat(shared): add TransactionType enum"
```

---

## Task 2: Add `RestaurantTransaction` Entity

**Files:**
- Create: `DeliverTableServer/Models/RestaurantTransaction.cs`

**Step 1: Create the entity**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Models;

public class RestaurantTransaction
{
    [Key]
    public int Id { get; set; }

    public int RestaurantId { get; set; }

    [ForeignKey("RestaurantId")]
    public Restaurant Restaurant { get; set; } = null!;

    public int? OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    public TransactionType Type { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal GrossAmount { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal CommissionAmount { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal NetAmount { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal BalanceAfter { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**Step 2: Commit**

```bash
git add DeliverTableServer/Models/RestaurantTransaction.cs
git commit -m "feat(server): add RestaurantTransaction entity"
```

---

## Task 3: Update `Restaurant` Model

**Files:**
- Modify: `DeliverTableServer/Models/Restaurant.cs`

**Step 1: Add `Balance` field and `Transactions` navigation**

Add these two properties to the `Restaurant` class, after the `Dishes` property:

```csharp
[Column(TypeName = "decimal(9, 2)")]
public decimal Balance { get; set; }
public List<RestaurantTransaction> Transactions { get; set; } = [];
```

**Step 2: Commit**

```bash
git add DeliverTableServer/Models/Restaurant.cs
git commit -m "feat(server): add Balance and Transactions to Restaurant entity"
```

---

## Task 4: Add EF Configuration for `RestaurantTransaction`

**Files:**
- Create: `DeliverTableServer/Data/ModelConfiguration/RestaurantTransactionConfiguration.cs`
- Modify: `DeliverTableServer/Data/ModelConfiguration/RestaurantConfiguration.cs`

**Step 1: Create `RestaurantTransactionConfiguration`**

```csharp
using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class RestaurantTransactionConfiguration : IEntityTypeConfiguration<RestaurantTransaction>
{
    public void Configure(EntityTypeBuilder<RestaurantTransaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasOne(t => t.Restaurant)
            .WithMany(r => r.Transactions)
            .HasForeignKey(t => t.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Order)
            .WithMany()
            .HasForeignKey(t => t.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(t => t.GrossAmount)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.Property(t => t.CommissionAmount)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.Property(t => t.NetAmount)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.Property(t => t.BalanceAfter)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.HasIndex(t => t.RestaurantId);
        builder.HasIndex(t => t.OrderId);
        builder.HasIndex(t => t.Type);
    }
}
```

**Step 2: Add `Balance` configuration to `RestaurantConfiguration`**

In `DeliverTableServer/Data/ModelConfiguration/RestaurantConfiguration.cs`, add inside the `Configure` method:

```csharp
builder.Property(r => r.Balance)
    .HasColumnType("decimal(9, 2)")
    .HasDefaultValue(0m)
    .IsRequired();
```

**Step 3: Commit**

```bash
git add DeliverTableServer/Data/ModelConfiguration/RestaurantTransactionConfiguration.cs DeliverTableServer/Data/ModelConfiguration/RestaurantConfiguration.cs
git commit -m "feat(db): add EF configuration for RestaurantTransaction and Restaurant.Balance"
```

---

## Task 5: Add DbSet and Generate Migration

**Files:**
- Create: `DeliverTableServer/Data/Contexts/DeliverTableContext.Account.cs`

**Step 1: Create the partial context file**

```csharp
using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Data
{
    public partial class DeliverTableContext
    {
        public DbSet<RestaurantTransaction> RestaurantTransactions { get; set; }
    }
}
```

**Step 2: Generate the EF migration**

The dev stack must be running (`make dev`). Run inside the backend container:

```bash
docker compose -f docker-dev.yaml exec backend dotnet ef migrations add AddRestaurantAccountAndTransactions --project /src/DeliverTableServer/DeliverTableServer.csproj
```

**Step 3: Apply the migration**

```bash
make dev-migrate
```

**Step 4: Verify the migration was applied**

```bash
docker compose -f docker-dev.yaml exec backend dotnet ef migrations list --project /src/DeliverTableServer/DeliverTableServer.csproj
```

Expected: `AddRestaurantAccountAndTransactions` appears in the list with no `(Pending)` marker.

**Step 5: Commit**

```bash
git add DeliverTableServer/Data/Contexts/DeliverTableContext.Account.cs DeliverTableServer/Migrations/
git commit -m "feat(db): add migration for RestaurantTransaction table and Restaurant.Balance"
```

---

## Task 6: Add `PLATFORM_COMMISSION_RATE` to Configuration

**Files:**
- Modify: `DeliverTableServer/Configuration/AppEnvironment.cs`
- Modify: `docker-dev.yaml`

**Step 1: Add property and env var parsing to `AppEnvironment`**

In `AppEnvironment.cs`:

1. Add property:
```csharp
public decimal PlatformCommissionRate { get; }
```

2. Add to constructor parameters:
```csharp
decimal platformCommissionRate
```

3. Add assignment in constructor body:
```csharp
PlatformCommissionRate = platformCommissionRate;
```

4. In the `Load()` method, before the error check, add:
```csharp
var platformCommissionRate = ParseDecimal("PLATFORM_COMMISSION_RATE", 0.10m, errors);
```

5. Add `platformCommissionRate` to the `return new AppEnvironment(...)` call.

6. Add the `ParseDecimal` helper method:
```csharp
private static decimal ParseDecimal(string name, decimal defaultValue, List<string> errors)
{
    var raw = GetVar(name);
    if (string.IsNullOrWhiteSpace(raw))
        return defaultValue;
    if (decimal.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, out var result))
        return result;
    errors.Add($"{name} (expected decimal, got '{raw}')");
    return defaultValue;
}
```

**Step 2: Add env var to `docker-dev.yaml`**

In the `backend` service `environment` section, add:
```yaml
PLATFORM_COMMISSION_RATE: ${PLATFORM_COMMISSION_RATE:-0.10}
```

**Step 3: Build to verify**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

**Step 4: Commit**

```bash
git add DeliverTableServer/Configuration/AppEnvironment.cs docker-dev.yaml
git commit -m "feat(server): add PLATFORM_COMMISSION_RATE env var configuration"
```

---

## Task 7: Add Error Messages and API Routes

**Files:**
- Modify: `DeliverTableServer/Constants/ErrorMessages.cs`
- Modify: `DeliverTableSharedLibrary/Constants/ApiRoutes.cs`

**Step 1: Add error messages**

In `ErrorMessages.cs`, add at the end of the class (before closing brace):

```csharp
// Restaurant Account
public const string InsufficientBalance = "Solde insuffisant pour effectuer ce retrait";
public const string InvalidWithdrawalAmount = "Le montant du retrait doit être supérieur à 0";
```

**Step 2: Add API routes**

In `ApiRoutes.cs`, add a new nested class after the `Order` class:

```csharp
/// <summary>Restaurant account and transaction routes.</summary>
public static class RestaurantAccount
{
    public const string BaseRoute = "api/v1/restaurant/{id:int}/account";
    public const string WithdrawRoute = "withdraw";
}
```

**Step 3: Commit**

```bash
git add DeliverTableServer/Constants/ErrorMessages.cs DeliverTableSharedLibrary/Constants/ApiRoutes.cs
git commit -m "feat(shared): add restaurant account error messages and API routes"
```

---

## Task 8: Add DTOs

**Files:**
- Create: `DeliverTableSharedLibrary/Dtos/RestaurantAccount/RestaurantTransactionDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/RestaurantAccount/RestaurantAccountDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/RestaurantAccount/WithdrawRequest.cs`
- Create: `DeliverTableSharedLibrary/Dtos/RestaurantAccount/TransactionQuery.cs`

**Step 1: Create `RestaurantTransactionDto`**

```csharp
namespace DeliverTableSharedLibrary.Dtos.RestaurantAccount;

public class RestaurantTransactionDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Step 2: Create `RestaurantAccountDto`**

```csharp
namespace DeliverTableSharedLibrary.Dtos.RestaurantAccount;

public class RestaurantAccountDto
{
    public decimal Balance { get; set; }
    public PaginatedResult<RestaurantTransactionDto> Transactions { get; set; } = null!;
}
```

**Step 3: Create `WithdrawRequest`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.RestaurantAccount;

public class WithdrawRequest
{
    [Required]
    [Range(0.01, 999999.99)]
    public decimal Amount { get; set; }
}
```

**Step 4: Create `TransactionQuery`**

```csharp
namespace DeliverTableSharedLibrary.Dtos.RestaurantAccount;

public class TransactionQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

**Step 5: Commit**

```bash
git add DeliverTableSharedLibrary/Dtos/RestaurantAccount/
git commit -m "feat(shared): add restaurant account DTOs"
```

---

## Task 9: Add Mapper

**Files:**
- Create: `DeliverTableServer/Mappers/RestaurantTransactionMapper.cs`

**Step 1: Create the mapper**

```csharp
using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;

namespace DeliverTableServer.Mappers;

public static class RestaurantTransactionMapper
{
    public static RestaurantTransactionDto ToDto(this RestaurantTransaction transaction)
    {
        return new RestaurantTransactionDto
        {
            Id = transaction.Id,
            Type = transaction.Type.ToString(),
            OrderId = transaction.OrderId,
            GrossAmount = transaction.GrossAmount,
            CommissionAmount = transaction.CommissionAmount,
            NetAmount = transaction.NetAmount,
            BalanceAfter = transaction.BalanceAfter,
            CreatedAt = transaction.CreatedAt
        };
    }
}
```

**Step 2: Commit**

```bash
git add DeliverTableServer/Mappers/RestaurantTransactionMapper.cs
git commit -m "feat(server): add RestaurantTransaction mapper"
```

---

## Task 10: Add Repository Layer

**Files:**
- Create: `DeliverTableServer/Repositories/Interfaces/IRestaurantTransactionRepository.cs`
- Create: `DeliverTableServer/Repositories/RestaurantTransactionRepository.cs`

**Step 1: Create the interface**

```csharp
using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;

namespace DeliverTableServer.Repositories.Interfaces;

public interface IRestaurantTransactionRepository
{
    Task<RestaurantTransaction> CreateAsync(RestaurantTransaction transaction, CancellationToken ct = default);
    Task<(List<RestaurantTransaction> Items, int TotalCount)> GetByRestaurantAsync(int restaurantId, TransactionQuery query, CancellationToken ct = default);
}
```

**Step 2: Create the implementation**

```csharp
using DeliverTableServer.Data;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories;

public class RestaurantTransactionRepository(DeliverTableContext dbContext) : IRestaurantTransactionRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<RestaurantTransaction> CreateAsync(RestaurantTransaction transaction, CancellationToken ct = default)
    {
        _dbContext.RestaurantTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync(ct);
        return transaction;
    }

    public async Task<(List<RestaurantTransaction> Items, int TotalCount)> GetByRestaurantAsync(
        int restaurantId, TransactionQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.RestaurantTransactions
            .Where(t => t.RestaurantId == restaurantId)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await q.CountAsync(ct);

        int page = query.PageNumber > 0 ? query.PageNumber : 1;
        int skip = (page - 1) * query.PageSize;

        var items = await q.Skip(skip).Take(query.PageSize).ToListAsync(ct);
        return (items, totalCount);
    }
}
```

**Step 3: Commit**

```bash
git add DeliverTableServer/Repositories/Interfaces/IRestaurantTransactionRepository.cs DeliverTableServer/Repositories/RestaurantTransactionRepository.cs
git commit -m "feat(server): add RestaurantTransaction repository"
```

---

## Task 11: Add Service Layer — Tests First

**Files:**
- Create: `DeliverTableServer/Services/Interfaces/IRestaurantAccountService.cs`
- Create: `DeliverTableServer/Services/RestaurantAccountService.cs`
- Create: `DeliverTableTests/Server/Unit/Services/RestaurantAccountServiceTests.cs`

**Step 1: Create the service interface**

```csharp
using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;

namespace DeliverTableServer.Services.Interfaces;

public interface IRestaurantAccountService
{
    Task<ServiceResult<RestaurantAccountDto>> GetAccountAsync(int restaurantId, int ownerId, TransactionQuery query, CancellationToken ct = default);
    Task<ServiceResult<RestaurantAccountDto>> WithdrawAsync(int restaurantId, int ownerId, WithdrawRequest request, CancellationToken ct = default);
}
```

**Step 2: Write the failing tests**

```csharp
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class RestaurantAccountServiceTests
{
    private IRestaurantRepository _restaurantRepository = null!;
    private IRestaurantTransactionRepository _transactionRepository = null!;
    private AppEnvironment _appEnvironment = null!;
    private RestaurantAccountService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _transactionRepository = Substitute.For<IRestaurantTransactionRepository>();

        Environment.SetEnvironmentVariable("CONNECTION_STRING_DATABASE", "Host=localhost;Database=test");
        Environment.SetEnvironmentVariable("JWT_KEY", "TestKeyThatIsLongEnoughForHmacSha256Signing!");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "TestIssuer");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "TestAudience");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_SERVICE_URL", "http://localhost:3900");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_ACCESS_KEY", "key");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_SECRET_KEY", "secret");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_BUCKET_NAME", "bucket");
        Environment.SetEnvironmentVariable("PLATFORM_COMMISSION_RATE", "0.10");
        _appEnvironment = AppEnvironment.Load();

        _sut = new RestaurantAccountService(_restaurantRepository, _transactionRepository, _appEnvironment);
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("CONNECTION_STRING_DATABASE", null);
        Environment.SetEnvironmentVariable("JWT_KEY", null);
        Environment.SetEnvironmentVariable("JWT_ISSUER", null);
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_SERVICE_URL", null);
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_ACCESS_KEY", null);
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_SECRET_KEY", null);
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_BUCKET_NAME", null);
        Environment.SetEnvironmentVariable("PLATFORM_COMMISSION_RATE", null);
    }

    // --- GetAccountAsync ---

    [Test]
    public async Task GetAccountAsync_WhenRestaurantNotFound_ReturnsError()
    {
        _restaurantRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        var result = await _sut.GetAccountAsync(99, 1, new TransactionQuery());

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task GetAccountAsync_WhenNotOwner_ReturnsError()
    {
        var restaurant = CreateRestaurant(ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        var result = await _sut.GetAccountAsync(1, 999, new TransactionQuery());

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task GetAccountAsync_WhenOwner_ReturnsAccountWithBalance()
    {
        var restaurant = CreateRestaurant(ownerId: 5, balance: 360m);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _transactionRepository.GetByRestaurantAsync(1, Arg.Any<TransactionQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<RestaurantTransaction>(), 0));

        var result = await _sut.GetAccountAsync(1, 5, new TransactionQuery());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Balance, Is.EqualTo(360m));
    }

    // --- WithdrawAsync ---

    [Test]
    public async Task WithdrawAsync_WhenRestaurantNotFound_ReturnsError()
    {
        _restaurantRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        var result = await _sut.WithdrawAsync(99, 1, new WithdrawRequest { Amount = 100 });

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task WithdrawAsync_WhenNotOwner_ReturnsError()
    {
        var restaurant = CreateRestaurant(ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        var result = await _sut.WithdrawAsync(1, 999, new WithdrawRequest { Amount = 100 });

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task WithdrawAsync_WhenInsufficientBalance_ReturnsError()
    {
        var restaurant = CreateRestaurant(ownerId: 5, balance: 50m);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        var result = await _sut.WithdrawAsync(1, 5, new WithdrawRequest { Amount = 100 });

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.InsufficientBalance));
    }

    [Test]
    public async Task WithdrawAsync_WhenValid_DecreasesBalanceAndCreatesTransaction()
    {
        var restaurant = CreateRestaurant(ownerId: 5, balance: 500m);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _transactionRepository.GetByRestaurantAsync(1, Arg.Any<TransactionQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<RestaurantTransaction>(), 0));

        var result = await _sut.WithdrawAsync(1, 5, new WithdrawRequest { Amount = 200 });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Balance, Is.EqualTo(300m));
        await _transactionRepository.Received(1).CreateAsync(
            Arg.Is<RestaurantTransaction>(t =>
                t.Type == DeliverTableSharedLibrary.Enums.TransactionType.Withdrawal &&
                t.NetAmount == 200m &&
                t.BalanceAfter == 300m),
            Arg.Any<CancellationToken>());
    }

    // --- Helpers ---

    private static Restaurant CreateRestaurant(int ownerId, decimal balance = 0m)
    {
        return new Restaurant
        {
            Id = 1,
            Name = "Test Restaurant",
            OwnerId = ownerId,
            Balance = balance,
            AdressLine1 = "1 Rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "FR"
        };
    }
}
```

**Step 3: Run tests to verify they fail**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~RestaurantAccountServiceTests"
```

Expected: FAIL — `RestaurantAccountService` class does not exist yet.

**Step 4: Implement the service**

```csharp
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Services;

public sealed class RestaurantAccountService(
    IRestaurantRepository restaurantRepository,
    IRestaurantTransactionRepository transactionRepository,
    AppEnvironment appEnvironment
) : IRestaurantAccountService
{
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;
    private readonly IRestaurantTransactionRepository _transactionRepository = transactionRepository;
    private readonly decimal _commissionRate = appEnvironment.PlatformCommissionRate;

    public async Task<ServiceResult<RestaurantAccountDto>> GetAccountAsync(
        int restaurantId, int ownerId, TransactionQuery query, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (restaurant.OwnerId != ownerId)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        var (items, totalCount) = await _transactionRepository.GetByRestaurantAsync(restaurantId, query, ct);

        return new RestaurantAccountDto
        {
            Balance = restaurant.Balance,
            Transactions = new PaginatedResult<RestaurantTransactionDto>
            {
                Items = items.Select(t => t.ToDto()).ToList(),
                TotalCount = totalCount,
                Page = query.PageNumber > 0 ? query.PageNumber : 1,
                PageSize = query.PageSize
            }
        };
    }

    public async Task<ServiceResult<RestaurantAccountDto>> WithdrawAsync(
        int restaurantId, int ownerId, WithdrawRequest request, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (restaurant.OwnerId != ownerId)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (request.Amount > restaurant.Balance)
            return new ServiceError(ErrorMessages.InsufficientBalance);

        restaurant.Balance -= request.Amount;
        await _restaurantRepository.UpdateAsync(restaurant, ct);

        var transaction = new Models.RestaurantTransaction
        {
            RestaurantId = restaurantId,
            OrderId = null,
            Type = TransactionType.Withdrawal,
            GrossAmount = request.Amount,
            CommissionAmount = 0,
            NetAmount = request.Amount,
            BalanceAfter = restaurant.Balance
        };

        await _transactionRepository.CreateAsync(transaction, ct);

        return await GetAccountAsync(restaurantId, ownerId, new TransactionQuery(), ct);
    }
}
```

**Step 5: Build and run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln && docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~RestaurantAccountServiceTests"
```

Expected: All 6 tests PASS.

**Step 6: Commit**

```bash
git add DeliverTableServer/Services/Interfaces/IRestaurantAccountService.cs DeliverTableServer/Services/RestaurantAccountService.cs DeliverTableTests/Server/Unit/Services/RestaurantAccountServiceTests.cs
git commit -m "feat(server): add RestaurantAccountService with tests"
```

---

## Task 12: Add Controller — Tests First

**Files:**
- Create: `DeliverTableServer/Controllers/RestaurantAccountController.cs`
- Create: `DeliverTableTests/Server/Unit/Controllers/RestaurantAccountControllerTests.cs`

**Step 1: Write the failing tests**

```csharp
using System.Security.Claims;
using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class RestaurantAccountControllerTests
{
    private IRestaurantAccountService _accountService = null!;
    private RestaurantAccountController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _accountService = Substitute.For<IRestaurantAccountService>();
        _sut = new RestaurantAccountController(_accountService);
    }

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, nameof(UserRole.RestaurantOwner))
        };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Test]
    public async Task GetAccount_WithValidOwner_ReturnsOk()
    {
        SetupAuthenticatedUser("5");
        var accountDto = new RestaurantAccountDto
        {
            Balance = 360m,
            Transactions = new PaginatedResult<RestaurantTransactionDto>
            {
                Items = [], TotalCount = 0, Page = 1, PageSize = 20
            }
        };
        _accountService.GetAccountAsync(1, 5, Arg.Any<TransactionQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RestaurantAccountDto>.Success(accountDto));

        var result = await _sut.GetAccount(1, new TransactionQuery(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAccount_WhenUnauthorized_ReturnsUnauthorized()
    {
        // No user set up
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _sut.GetAccount(1, new TransactionQuery(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task Withdraw_WithValidRequest_ReturnsOk()
    {
        SetupAuthenticatedUser("5");
        var accountDto = new RestaurantAccountDto
        {
            Balance = 300m,
            Transactions = new PaginatedResult<RestaurantTransactionDto>
            {
                Items = [], TotalCount = 0, Page = 1, PageSize = 20
            }
        };
        _accountService.WithdrawAsync(1, 5, Arg.Any<WithdrawRequest>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RestaurantAccountDto>.Success(accountDto));

        var result = await _sut.Withdraw(1, new WithdrawRequest { Amount = 200 }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Withdraw_WhenServiceFails_ReturnsError()
    {
        SetupAuthenticatedUser("5");
        _accountService.WithdrawAsync(1, 5, Arg.Any<WithdrawRequest>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RestaurantAccountDto>.Failure(new ServiceError("Solde insuffisant", 400)));

        var result = await _sut.Withdraw(1, new WithdrawRequest { Amount = 9999 }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(400));
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~RestaurantAccountControllerTests"
```

Expected: FAIL — `RestaurantAccountController` does not exist.

**Step 3: Implement the controller**

```csharp
using System.Security.Claims;
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.RestaurantAccount.BaseRoute)]
[Authorize(Roles = nameof(UserRole.RestaurantOwner))]
public class RestaurantAccountController(IRestaurantAccountService accountService) : ControllerBase
{
    private readonly IRestaurantAccountService _accountService = accountService;

    [HttpGet]
    public async Task<IActionResult> GetAccount([FromRoute] int id, [FromQuery] TransactionQuery query, CancellationToken ct)
    {
        if (!TryGetUserId(out int userId)) return Unauthorized();

        var result = await _accountService.GetAccountAsync(id, userId, query, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.RestaurantAccount.WithdrawRoute)]
    public async Task<IActionResult> Withdraw([FromRoute] int id, [FromBody] WithdrawRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out int userId)) return Unauthorized();

        var result = await _accountService.WithdrawAsync(id, userId, request, ct);
        return result.ToOkResult();
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
    }
}
```

**Step 4: Build and run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln && docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~RestaurantAccountControllerTests"
```

Expected: All 4 tests PASS.

**Step 5: Commit**

```bash
git add DeliverTableServer/Controllers/RestaurantAccountController.cs DeliverTableTests/Server/Unit/Controllers/RestaurantAccountControllerTests.cs
git commit -m "feat(server): add RestaurantAccountController with tests"
```

---

## Task 13: Register Services in DI

**Files:**
- Modify: `DeliverTableServer/Extensions/ServiceCollectionExtensions.cs`

**Step 1: Add registrations**

In `RegisterRepositories`, add:
```csharp
services.AddScoped<IRestaurantTransactionRepository, RestaurantTransactionRepository>();
```

In `RegisterServices`, add:
```csharp
services.AddScoped<IRestaurantAccountService, RestaurantAccountService>();
```

**Step 2: Build to verify**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```

**Step 3: Commit**

```bash
git add DeliverTableServer/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(server): register RestaurantAccountService and RestaurantTransactionRepository in DI"
```

---

## Task 14: Add Crediting Logic to `OrderService.UpdateStatusAsync` — Tests First

**Files:**
- Modify: `DeliverTableServer/Services/OrderService.cs`
- Create: `DeliverTableTests/Server/Unit/Services/OrderServiceTests.cs`

**Step 1: Write the failing tests**

```csharp
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class OrderServiceTests
{
    private IOrderRepository _orderRepository = null!;
    private ICartRepository _cartRepository = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private IRestaurantTransactionRepository _transactionRepository = null!;
    private AppEnvironment _appEnvironment = null!;
    private OrderService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _orderRepository = Substitute.For<IOrderRepository>();
        _cartRepository = Substitute.For<ICartRepository>();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _transactionRepository = Substitute.For<IRestaurantTransactionRepository>();

        Environment.SetEnvironmentVariable("CONNECTION_STRING_DATABASE", "Host=localhost;Database=test");
        Environment.SetEnvironmentVariable("JWT_KEY", "TestKeyThatIsLongEnoughForHmacSha256Signing!");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "TestIssuer");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "TestAudience");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_SERVICE_URL", "http://localhost:3900");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_ACCESS_KEY", "key");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_SECRET_KEY", "secret");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_BUCKET_NAME", "bucket");
        Environment.SetEnvironmentVariable("PLATFORM_COMMISSION_RATE", "0.10");
        _appEnvironment = AppEnvironment.Load();

        _sut = new OrderService(_orderRepository, _cartRepository, _restaurantRepository, _transactionRepository, _appEnvironment);
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("CONNECTION_STRING_DATABASE", null);
        Environment.SetEnvironmentVariable("JWT_KEY", null);
        Environment.SetEnvironmentVariable("JWT_ISSUER", null);
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_SERVICE_URL", null);
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_ACCESS_KEY", null);
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_SECRET_KEY", null);
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_BUCKET_NAME", null);
        Environment.SetEnvironmentVariable("PLATFORM_COMMISSION_RATE", null);
    }

    [Test]
    public async Task UpdateStatusAsync_WhenDelivered_CreditsRestaurantAccount()
    {
        var restaurant = new Restaurant
        {
            Id = 1, Name = "Test", Balance = 0m,
            AdressLine1 = "1 Rue Test", City = "Paris", ZipCode = "75001", Country = "FR"
        };
        var order = new Order
        {
            Id = 10, RestaurantId = 1, Restaurant = restaurant,
            TotalAmount = 400m, Status = OrderStatus.Ready, Items = []
        };
        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = nameof(OrderStatus.Delivered) });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(restaurant.Balance, Is.EqualTo(360m));
        await _transactionRepository.Received(1).CreateAsync(
            Arg.Is<RestaurantTransaction>(t =>
                t.Type == TransactionType.Credit &&
                t.GrossAmount == 400m &&
                t.CommissionAmount == 40m &&
                t.NetAmount == 360m &&
                t.BalanceAfter == 360m),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStatusAsync_WhenNotDelivered_DoesNotCreditRestaurant()
    {
        var restaurant = new Restaurant
        {
            Id = 1, Name = "Test", Balance = 0m,
            AdressLine1 = "1 Rue Test", City = "Paris", ZipCode = "75001", Country = "FR"
        };
        var order = new Order
        {
            Id = 10, RestaurantId = 1, Restaurant = restaurant,
            TotalAmount = 400m, Status = OrderStatus.Confirmed, Items = []
        };
        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = nameof(OrderStatus.Preparing) });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(restaurant.Balance, Is.EqualTo(0m));
        await _transactionRepository.DidNotReceive().CreateAsync(Arg.Any<RestaurantTransaction>(), Arg.Any<CancellationToken>());
    }
}
```

**Step 3: Run tests to verify they fail**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~OrderServiceTests"
```

Expected: FAIL — `OrderService` constructor does not accept the new dependencies.

**Step 4: Modify `OrderService`**

Update the constructor in `DeliverTableServer/Services/OrderService.cs` to accept the new dependencies:

```csharp
public sealed class OrderService(
    IOrderRepository orderRepository,
    ICartRepository cartRepository,
    IRestaurantRepository restaurantRepository,
    IRestaurantTransactionRepository transactionRepository,
    AppEnvironment appEnvironment
) : IOrderService
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;
    private readonly IRestaurantTransactionRepository _transactionRepository = transactionRepository;
    private readonly decimal _commissionRate = appEnvironment.PlatformCommissionRate;
```

Then update `UpdateStatusAsync` to add crediting logic after the status update:

```csharp
public async Task<ServiceResult<OrderDto>> UpdateStatusAsync(
    int orderId, UpdateOrderStatusRequest request, CancellationToken ct = default)
{
    var order = await _orderRepository.GetByIdAsync(orderId, ct);
    if (order is null)
        return new ServiceError(ErrorMessages.OrderNotFound, 404);

    if (!Enum.TryParse<OrderStatus>(request.Status, out var newStatus))
    {
        var validValues = string.Join(", ", Enum.GetNames<OrderStatus>());
        return new ServiceError(ErrorMessages.InvalidOrderStatus(validValues));
    }

    order.Status = newStatus;
    var updated = await _orderRepository.UpdateAsync(order, ct);

    if (newStatus == OrderStatus.Delivered)
    {
        var restaurant = order.Restaurant;
        var commission = order.TotalAmount * _commissionRate;
        var netAmount = order.TotalAmount - commission;

        restaurant.Balance += netAmount;
        await _restaurantRepository.UpdateAsync(restaurant, ct);

        var transaction = new RestaurantTransaction
        {
            RestaurantId = restaurant.Id,
            OrderId = order.Id,
            Type = TransactionType.Credit,
            GrossAmount = order.TotalAmount,
            CommissionAmount = commission,
            NetAmount = netAmount,
            BalanceAfter = restaurant.Balance
        };

        await _transactionRepository.CreateAsync(transaction, ct);
    }

    return updated.ToDto();
}
```

Add the required using statements at the top of `OrderService.cs`:

```csharp
using DeliverTableServer.Configuration;
```

(`RestaurantTransaction` and `TransactionType` should already be accessible via existing usings.)

**Step 5: Build and run tests**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln && docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~OrderServiceTests"
```

Expected: All 2 tests PASS.

**Step 6: Run all tests to verify no regressions**

```bash
make test
```

Expected: All tests pass (except the known `AppEnvironmentTests` failure in Docker).

**Step 7: Commit**

```bash
git add DeliverTableServer/Services/OrderService.cs DeliverTableTests/Server/Unit/Services/OrderServiceTests.cs
git commit -m "feat(server): credit restaurant account when order is delivered"
```

---

## Task 15: Update ER Diagram Documentation

**Files:**
- Modify: `docs/db/er-diagram.md`

**Step 1: Add `RESTAURANT_TRANSACTION` entity to the Mermaid diagram**

In the `erDiagram` block, add after the `RESTAURANT` entity:

```mermaid
RESTAURANT_TRANSACTION {
    int     id PK
    int     restaurant_id FK
    int     order_id FK          "nullable – null for withdrawals"
    string  type                 "CREDIT | WITHDRAWAL"
    float   gross_amount
    float   commission_amount
    float   net_amount
    float   balance_after
    datetime created_at
}
```

**Step 2: Update the `RESTAURANT` entity to include `balance`**

Add `float balance` to the `RESTAURANT` entity in the Mermaid block.

**Step 3: Add the relationships**

In the relationships section, add:

```mermaid
RESTAURANT ||--o{ RESTAURANT_TRANSACTION : "has transactions"
ORDER ||--o{ RESTAURANT_TRANSACTION : "generates credit"
```

**Step 4: Commit**

```bash
git add docs/db/er-diagram.md
git commit -m "docs(db): add RestaurantTransaction and Restaurant.Balance to ER diagram"
```

---

## Task 16: Final Verification

**Step 1: Run the full CI gate**

```bash
make ci
```

Expected: All checks pass (format, build, test, security, coverage).

**Step 2: Run format fix if needed**

```bash
make format-fix
```

**Step 3: Commit any format fixes**

```bash
git add -A && git commit -m "style: apply formatting fixes"
```

(Only if `format-fix` made changes.)
