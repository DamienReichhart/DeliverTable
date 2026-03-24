# Promotions, Discount Codes & Loyalty Program Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add promotions (auto/item/threshold), discount codes, and per-restaurant loyalty programs — integrated into checkout with full stacking, restaurant owner management, and customer-facing UI.

**Architecture:** Three new domains (Promotion, DiscountCode, Loyalty) following Controller → Service → Repository. Checkout integration modifies `OrderService.CreateFromCartAsync` to apply discounts in order: promotions → discount code → loyalty points. Loyalty points earned on `Delivered` status. Each domain has its own CRUD endpoints for restaurant owners.

**Tech Stack:** .NET 10, EF Core, PostgreSQL, NUnit 4 + NSubstitute, Blazor WASM

---

## Task 1: Add Enums

**Files:**
- Create: `DeliverTableSharedLibrary/Enums/PromotionType.cs`
- Create: `DeliverTableSharedLibrary/Enums/DiscountType.cs`
- Create: `DeliverTableSharedLibrary/Enums/LoyaltyTransactionType.cs`
- Create: `DeliverTableSharedLibrary/Enums/OrderDiscountSource.cs`

**Step 1: Create all four enums**

```csharp
// PromotionType.cs
namespace DeliverTableSharedLibrary.Enums;

public enum PromotionType
{
    Automatic,
    ItemBased,
    Threshold
}
```

```csharp
// DiscountType.cs
namespace DeliverTableSharedLibrary.Enums;

public enum DiscountType
{
    Percentage,
    FixedAmount
}
```

```csharp
// LoyaltyTransactionType.cs
namespace DeliverTableSharedLibrary.Enums;

public enum LoyaltyTransactionType
{
    Earn,
    Redeem,
    Adjust
}
```

```csharp
// OrderDiscountSource.cs
namespace DeliverTableSharedLibrary.Enums;

public enum OrderDiscountSource
{
    Promotion,
    DiscountCode,
    LoyaltyPoints
}
```

**Step 2: Commit**

```bash
git add DeliverTableSharedLibrary/Enums/PromotionType.cs DeliverTableSharedLibrary/Enums/DiscountType.cs DeliverTableSharedLibrary/Enums/LoyaltyTransactionType.cs DeliverTableSharedLibrary/Enums/OrderDiscountSource.cs
git commit -m "feat(shared): add enums for promotions, discounts, and loyalty"
```

---

## Task 2: Add Promotion Entities

**Files:**
- Create: `DeliverTableServer/Models/Promotion.cs`
- Create: `DeliverTableServer/Models/PromotionDish.cs`

**Step 1: Create entities**

```csharp
// Promotion.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Models;

public class Promotion
{
    [Key]
    public int Id { get; set; }

    public int RestaurantId { get; set; }

    [ForeignKey("RestaurantId")]
    public Restaurant Restaurant { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public PromotionType PromotionType { get; set; }

    public DiscountType DiscountType { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal DiscountValue { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal? MinOrderAmount { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public bool IsActive { get; set; } = true;

    public List<PromotionDish> PromotionDishes { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

```csharp
// PromotionDish.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableServer.Models;

public class PromotionDish
{
    [Key]
    public int Id { get; set; }

    public int PromotionId { get; set; }

    [ForeignKey("PromotionId")]
    public Promotion Promotion { get; set; } = null!;

    public int DishId { get; set; }

    [ForeignKey("DishId")]
    public Dish Dish { get; set; } = null!;
}
```

**Step 2: Commit**

```bash
git add DeliverTableServer/Models/Promotion.cs DeliverTableServer/Models/PromotionDish.cs
git commit -m "feat(server): add Promotion and PromotionDish entities"
```

---

## Task 3: Add Discount Code Entities

**Files:**
- Create: `DeliverTableServer/Models/DiscountCode.cs`
- Create: `DeliverTableServer/Models/DiscountCodeRedemption.cs`

**Step 1: Create entities**

```csharp
// DiscountCode.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Models;

public class DiscountCode
{
    [Key]
    public int Id { get; set; }

    public int RestaurantId { get; set; }

    [ForeignKey("RestaurantId")]
    public Restaurant Restaurant { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public DiscountType DiscountType { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal DiscountValue { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal? MinOrderAmount { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidUntil { get; set; }

    public int? MaxRedemptions { get; set; }

    public int PerUserLimit { get; set; } = 1;

    public int CurrentRedemptions { get; set; }

    public bool IsActive { get; set; } = true;

    public List<DiscountCodeRedemption> Redemptions { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

```csharp
// DiscountCodeRedemption.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableServer.Models;

public class DiscountCodeRedemption
{
    [Key]
    public int Id { get; set; }

    public int DiscountCodeId { get; set; }

    [ForeignKey("DiscountCodeId")]
    public DiscountCode DiscountCode { get; set; } = null!;

    public int CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public User Customer { get; set; } = null!;

    public int OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**Step 2: Commit**

```bash
git add DeliverTableServer/Models/DiscountCode.cs DeliverTableServer/Models/DiscountCodeRedemption.cs
git commit -m "feat(server): add DiscountCode and DiscountCodeRedemption entities"
```

---

## Task 4: Add Loyalty Entities

**Files:**
- Create: `DeliverTableServer/Models/LoyaltyProgram.cs`
- Create: `DeliverTableServer/Models/LoyaltyAccount.cs`
- Create: `DeliverTableServer/Models/LoyaltyTransaction.cs`

**Step 1: Create entities**

```csharp
// LoyaltyProgram.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableServer.Models;

public class LoyaltyProgram
{
    [Key]
    public int Id { get; set; }

    public int RestaurantId { get; set; }

    [ForeignKey("RestaurantId")]
    public Restaurant Restaurant { get; set; } = null!;

    [Column(TypeName = "decimal(9, 2)")]
    public decimal PointsPerEuro { get; set; } = 1.0m;

    [Column(TypeName = "decimal(9, 4)")]
    public decimal EurosPerPoint { get; set; } = 0.10m;

    public bool IsActive { get; set; } = true;

    public List<LoyaltyAccount> Accounts { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

```csharp
// LoyaltyAccount.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableServer.Models;

public class LoyaltyAccount
{
    [Key]
    public int Id { get; set; }

    public int LoyaltyProgramId { get; set; }

    [ForeignKey("LoyaltyProgramId")]
    public LoyaltyProgram LoyaltyProgram { get; set; } = null!;

    public int CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public User Customer { get; set; } = null!;

    public int PointsBalance { get; set; }

    public List<LoyaltyTransaction> Transactions { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

```csharp
// LoyaltyTransaction.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Models;

public class LoyaltyTransaction
{
    [Key]
    public int Id { get; set; }

    public int LoyaltyAccountId { get; set; }

    [ForeignKey("LoyaltyAccountId")]
    public LoyaltyAccount LoyaltyAccount { get; set; } = null!;

    public LoyaltyTransactionType Type { get; set; }

    public int Points { get; set; }

    public int? OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**Step 2: Commit**

```bash
git add DeliverTableServer/Models/LoyaltyProgram.cs DeliverTableServer/Models/LoyaltyAccount.cs DeliverTableServer/Models/LoyaltyTransaction.cs
git commit -m "feat(server): add LoyaltyProgram, LoyaltyAccount, and LoyaltyTransaction entities"
```

---

## Task 5: Add OrderDiscount Entity and Modify Order

**Files:**
- Create: `DeliverTableServer/Models/OrderDiscount.cs`
- Modify: `DeliverTableServer/Models/Order.cs`
- Modify: `DeliverTableSharedLibrary/Dtos/Order/OrderDto.cs`
- Modify: `DeliverTableSharedLibrary/Dtos/Order/CreateOrderRequest.cs`

**Step 1: Create OrderDiscount entity**

```csharp
// OrderDiscount.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Models;

public class OrderDiscount
{
    [Key]
    public int Id { get; set; }

    public int OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;

    public OrderDiscountSource Source { get; set; }

    public int? SourceId { get; set; }

    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(9, 2)")]
    public decimal Amount { get; set; }
}
```

**Step 2: Add fields to Order entity**

In `DeliverTableServer/Models/Order.cs`, add after `TotalAmount`:

```csharp
[Range(0, 999999.99)]
public decimal OriginalAmount { get; set; }

[Range(0, 999999.99)]
public decimal DiscountAmount { get; set; }

public int LoyaltyPointsUsed { get; set; }

public int LoyaltyPointsEarned { get; set; }

public int? DiscountCodeId { get; set; }

[ForeignKey("DiscountCodeId")]
public DiscountCode? DiscountCode { get; set; }

public List<OrderDiscount> Discounts { get; set; } = [];
```

**Step 3: Add fields to OrderDto**

In `DeliverTableSharedLibrary/Dtos/Order/OrderDto.cs`, add after `TotalAmount`:

```csharp
public decimal OriginalAmount { get; set; }
public decimal DiscountAmount { get; set; }
public int LoyaltyPointsUsed { get; set; }
public int LoyaltyPointsEarned { get; set; }
public List<OrderDiscountDto> Discounts { get; set; } = [];
```

**Step 4: Create OrderDiscountDto**

Create `DeliverTableSharedLibrary/Dtos/Order/OrderDiscountDto.cs`:

```csharp
namespace DeliverTableSharedLibrary.Dtos.Order;

public class OrderDiscountDto
{
    public string Source { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
```

**Step 5: Add optional fields to CreateOrderRequest**

In `DeliverTableSharedLibrary/Dtos/Order/CreateOrderRequest.cs`, add:

```csharp
[MaxLength(50)]
public string? DiscountCode { get; set; }

[Range(0, int.MaxValue)]
public int LoyaltyPointsToRedeem { get; set; }
```

**Step 6: Commit**

```bash
git add DeliverTableServer/Models/OrderDiscount.cs DeliverTableServer/Models/Order.cs DeliverTableSharedLibrary/Dtos/Order/OrderDto.cs DeliverTableSharedLibrary/Dtos/Order/OrderDiscountDto.cs DeliverTableSharedLibrary/Dtos/Order/CreateOrderRequest.cs
git commit -m "feat(server): add OrderDiscount entity and extend Order with discount fields"
```

---

## Task 6: Add EF Configurations

**Files:**
- Create: `DeliverTableServer/Data/ModelConfiguration/PromotionConfiguration.cs`
- Create: `DeliverTableServer/Data/ModelConfiguration/PromotionDishConfiguration.cs`
- Create: `DeliverTableServer/Data/ModelConfiguration/DiscountCodeConfiguration.cs`
- Create: `DeliverTableServer/Data/ModelConfiguration/DiscountCodeRedemptionConfiguration.cs`
- Create: `DeliverTableServer/Data/ModelConfiguration/LoyaltyProgramConfiguration.cs`
- Create: `DeliverTableServer/Data/ModelConfiguration/LoyaltyAccountConfiguration.cs`
- Create: `DeliverTableServer/Data/ModelConfiguration/LoyaltyTransactionConfiguration.cs`
- Create: `DeliverTableServer/Data/ModelConfiguration/OrderDiscountConfiguration.cs`

**Step 1: Create all configurations**

```csharp
// PromotionConfiguration.cs
using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.Restaurant)
            .WithMany()
            .HasForeignKey(p => p.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.PromotionType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.DiscountType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.DiscountValue)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.Property(p => p.MinOrderAmount)
            .HasColumnType("decimal(9, 2)");

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(p => p.RestaurantId);
        builder.HasIndex(p => p.IsActive);
    }
}
```

```csharp
// PromotionDishConfiguration.cs
using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class PromotionDishConfiguration : IEntityTypeConfiguration<PromotionDish>
{
    public void Configure(EntityTypeBuilder<PromotionDish> builder)
    {
        builder.HasKey(pd => pd.Id);

        builder.HasOne(pd => pd.Promotion)
            .WithMany(p => p.PromotionDishes)
            .HasForeignKey(pd => pd.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pd => pd.Dish)
            .WithMany()
            .HasForeignKey(pd => pd.DishId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pd => new { pd.PromotionId, pd.DishId }).IsUnique();
    }
}
```

```csharp
// DiscountCodeConfiguration.cs
using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
{
    public void Configure(EntityTypeBuilder<DiscountCode> builder)
    {
        builder.HasKey(dc => dc.Id);

        builder.HasOne(dc => dc.Restaurant)
            .WithMany()
            .HasForeignKey(dc => dc.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(dc => dc.DiscountType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(dc => dc.DiscountValue)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.Property(dc => dc.MinOrderAmount)
            .HasColumnType("decimal(9, 2)");

        builder.Property(dc => dc.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(dc => dc.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(dc => new { dc.RestaurantId, dc.Code }).IsUnique();
        builder.HasIndex(dc => dc.IsActive);
    }
}
```

```csharp
// DiscountCodeRedemptionConfiguration.cs
using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class DiscountCodeRedemptionConfiguration : IEntityTypeConfiguration<DiscountCodeRedemption>
{
    public void Configure(EntityTypeBuilder<DiscountCodeRedemption> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.DiscountCode)
            .WithMany(dc => dc.Redemptions)
            .HasForeignKey(r => r.DiscountCodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.HasIndex(r => new { r.DiscountCodeId, r.CustomerId });
    }
}
```

```csharp
// LoyaltyProgramConfiguration.cs
using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class LoyaltyProgramConfiguration : IEntityTypeConfiguration<LoyaltyProgram>
{
    public void Configure(EntityTypeBuilder<LoyaltyProgram> builder)
    {
        builder.HasKey(lp => lp.Id);

        builder.HasOne(lp => lp.Restaurant)
            .WithMany()
            .HasForeignKey(lp => lp.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(lp => lp.PointsPerEuro)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.Property(lp => lp.EurosPerPoint)
            .HasColumnType("decimal(9, 4)")
            .IsRequired();

        builder.Property(lp => lp.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(lp => lp.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(lp => lp.RestaurantId).IsUnique();
    }
}
```

```csharp
// LoyaltyAccountConfiguration.cs
using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
    {
        builder.HasKey(la => la.Id);

        builder.HasOne(la => la.LoyaltyProgram)
            .WithMany(lp => lp.Accounts)
            .HasForeignKey(la => la.LoyaltyProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(la => la.Customer)
            .WithMany()
            .HasForeignKey(la => la.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(la => la.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(la => la.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(la => new { la.LoyaltyProgramId, la.CustomerId }).IsUnique();
    }
}
```

```csharp
// LoyaltyTransactionConfiguration.cs
using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.HasKey(lt => lt.Id);

        builder.HasOne(lt => lt.LoyaltyAccount)
            .WithMany(la => la.Transactions)
            .HasForeignKey(lt => lt.LoyaltyAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(lt => lt.Order)
            .WithMany()
            .HasForeignKey(lt => lt.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(lt => lt.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(lt => lt.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.HasIndex(lt => lt.LoyaltyAccountId);
    }
}
```

```csharp
// OrderDiscountConfiguration.cs
using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class OrderDiscountConfiguration : IEntityTypeConfiguration<OrderDiscount>
{
    public void Configure(EntityTypeBuilder<OrderDiscount> builder)
    {
        builder.HasKey(od => od.Id);

        builder.HasOne(od => od.Order)
            .WithMany(o => o.Discounts)
            .HasForeignKey(od => od.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(od => od.Source)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(od => od.Amount)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.HasIndex(od => od.OrderId);
    }
}
```

**Step 2: Update OrderConfiguration for new Order fields**

In `DeliverTableServer/Data/ModelConfiguration/OrderConfiguration.cs`, add inside the `Configure` method:

```csharp
builder.Property(o => o.OriginalAmount)
    .HasColumnType("decimal(9, 2)")
    .IsRequired()
    .HasDefaultValue(0m);

builder.Property(o => o.DiscountAmount)
    .HasColumnType("decimal(9, 2)")
    .IsRequired()
    .HasDefaultValue(0m);

builder.Property(o => o.LoyaltyPointsUsed)
    .IsRequired()
    .HasDefaultValue(0);

builder.Property(o => o.LoyaltyPointsEarned)
    .IsRequired()
    .HasDefaultValue(0);

builder.HasOne(o => o.DiscountCode)
    .WithMany()
    .HasForeignKey(o => o.DiscountCodeId)
    .OnDelete(DeleteBehavior.SetNull);
```

**Step 3: Commit**

```bash
git add DeliverTableServer/Data/ModelConfiguration/
git commit -m "feat(db): add EF configurations for promotions, discounts, loyalty, and order discounts"
```

---

## Task 7: Add DbSets and Generate Migration

**Files:**
- Create: `DeliverTableServer/Data/Contexts/DeliverTableContext.Promotion.cs`
- Create: `DeliverTableServer/Data/Contexts/DeliverTableContext.Loyalty.cs`

**Step 1: Create partial context files**

```csharp
// DeliverTableContext.Promotion.cs
using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Data
{
    public partial class DeliverTableContext
    {
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<PromotionDish> PromotionDishes { get; set; }
        public DbSet<DiscountCode> DiscountCodes { get; set; }
        public DbSet<DiscountCodeRedemption> DiscountCodeRedemptions { get; set; }
        public DbSet<OrderDiscount> OrderDiscounts { get; set; }
    }
}
```

```csharp
// DeliverTableContext.Loyalty.cs
using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Data
{
    public partial class DeliverTableContext
    {
        public DbSet<LoyaltyProgram> LoyaltyPrograms { get; set; }
        public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; }
        public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
    }
}
```

**Step 2: Generate and apply migration**

```bash
docker compose -f docker-dev.yaml exec backend dotnet ef migrations add AddPromotionsDiscountsLoyalty --project /src/DeliverTableServer/DeliverTableServer.csproj
make dev-migrate
```

**Step 3: Commit**

```bash
git add DeliverTableServer/Data/Contexts/ DeliverTableServer/Migrations/
git commit -m "feat(db): add migration for promotions, discount codes, loyalty, and order discounts"
```

---

## Task 8: Add Error Messages and API Routes

**Files:**
- Modify: `DeliverTableServer/Constants/ErrorMessages.cs`
- Modify: `DeliverTableSharedLibrary/Constants/ApiRoutes.cs`

**Step 1: Add error messages**

In `ErrorMessages.cs`, add at the end before the closing brace:

```csharp
// Promotions
public const string PromotionNotFound = "Promotion introuvable";
public const string InvalidPromotionDates = "La date de fin doit être postérieure à la date de début";
public const string PromotionDishNotFromRestaurant = "Un ou plusieurs plats n'appartiennent pas à ce restaurant";

// Discount Codes
public const string DiscountCodeNotFound = "Code promo introuvable";
public const string DiscountCodeInvalid = "Code promo invalide ou expiré";
public const string DiscountCodeMaxRedemptions = "Ce code promo a atteint le nombre maximum d'utilisations";
public const string DiscountCodePerUserLimit = "Vous avez déjà utilisé ce code promo le nombre de fois autorisé";
public const string DiscountCodeMinOrderNotMet = "Le montant minimum de commande n'est pas atteint pour ce code promo";
public const string DiscountCodeAlreadyExists = "Un code promo avec ce code existe déjà pour ce restaurant";

// Loyalty
public const string LoyaltyProgramNotFound = "Programme de fidélité introuvable";
public const string LoyaltyProgramAlreadyExists = "Ce restaurant possède déjà un programme de fidélité";
public const string LoyaltyAccountNotFound = "Compte fidélité introuvable";
public const string InsufficientLoyaltyPoints = "Nombre de points de fidélité insuffisant";
public const string LoyaltyProgramNotActive = "Le programme de fidélité n'est pas actif";
```

**Step 2: Add API routes**

In `ApiRoutes.cs`, add after the `RestaurantAccount` class:

```csharp
/// <summary>Promotion routes (RestaurantOwner).</summary>
public static class Promotion
{
    public const string RestaurantBaseRoute = "api/v1/restaurant/{id:int}/promotions";
    public const string Base = "api/v1/promotion";
    public const string ByIdRoute = "{id:int}";
}

/// <summary>Discount code routes (RestaurantOwner).</summary>
public static class DiscountCode
{
    public const string RestaurantBaseRoute = "api/v1/restaurant/{id:int}/discount-codes";
    public const string Base = "api/v1/discount-code";
    public const string ByIdRoute = "{id:int}";
}

/// <summary>Loyalty program routes.</summary>
public static class Loyalty
{
    public const string RestaurantBaseRoute = "api/v1/restaurant/{id:int}/loyalty";
    public const string MyAccountRoute = "my-account";
}
```

**Step 3: Commit**

```bash
git add DeliverTableServer/Constants/ErrorMessages.cs DeliverTableSharedLibrary/Constants/ApiRoutes.cs
git commit -m "feat(shared): add error messages and API routes for promotions, discounts, and loyalty"
```

---

## Task 9: Add DTOs

**Files:**
- Create: `DeliverTableSharedLibrary/Dtos/Promotion/PromotionDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Promotion/CreatePromotionRequest.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Promotion/UpdatePromotionRequest.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Promotion/PromotionQuery.cs`
- Create: `DeliverTableSharedLibrary/Dtos/DiscountCode/DiscountCodeDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/DiscountCode/CreateDiscountCodeRequest.cs`
- Create: `DeliverTableSharedLibrary/Dtos/DiscountCode/UpdateDiscountCodeRequest.cs`
- Create: `DeliverTableSharedLibrary/Dtos/DiscountCode/DiscountCodeQuery.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Loyalty/LoyaltyProgramDto.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Loyalty/CreateLoyaltyProgramRequest.cs`
- Create: `DeliverTableSharedLibrary/Dtos/Loyalty/LoyaltyAccountDto.cs`

**Step 1: Create Promotion DTOs**

```csharp
// PromotionDto.cs
namespace DeliverTableSharedLibrary.Dtos.Promotion;

public class PromotionDto
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PromotionType { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; }
    public List<int> DishIds { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
```

```csharp
// CreatePromotionRequest.cs
using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Promotion;

public class CreatePromotionRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string PromotionType { get; set; } = string.Empty;

    [Required]
    public string DiscountType { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999999.99)]
    public decimal DiscountValue { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? MinOrderAmount { get; set; }

    [Required]
    public DateTime StartsAt { get; set; }

    [Required]
    public DateTime EndsAt { get; set; }

    public List<int> DishIds { get; set; } = [];
}
```

```csharp
// UpdatePromotionRequest.cs
using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Promotion;

public class UpdatePromotionRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string PromotionType { get; set; } = string.Empty;

    [Required]
    public string DiscountType { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999999.99)]
    public decimal DiscountValue { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? MinOrderAmount { get; set; }

    [Required]
    public DateTime StartsAt { get; set; }

    [Required]
    public DateTime EndsAt { get; set; }

    public bool IsActive { get; set; } = true;

    public List<int> DishIds { get; set; } = [];
}
```

```csharp
// PromotionQuery.cs
namespace DeliverTableSharedLibrary.Dtos.Promotion;

public class PromotionQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

**Step 2: Create Discount Code DTOs**

```csharp
// DiscountCodeDto.cs
namespace DeliverTableSharedLibrary.Dtos.DiscountCode;

public class DiscountCodeDto
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public int? MaxRedemptions { get; set; }
    public int PerUserLimit { get; set; }
    public int CurrentRedemptions { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

```csharp
// CreateDiscountCodeRequest.cs
using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.DiscountCode;

public class CreateDiscountCodeRequest
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string DiscountType { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999999.99)]
    public decimal DiscountValue { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? MinOrderAmount { get; set; }

    [Required]
    public DateTime ValidFrom { get; set; }

    [Required]
    public DateTime ValidUntil { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxRedemptions { get; set; }

    [Range(1, int.MaxValue)]
    public int PerUserLimit { get; set; } = 1;
}
```

```csharp
// UpdateDiscountCodeRequest.cs
using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.DiscountCode;

public class UpdateDiscountCodeRequest
{
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string DiscountType { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999999.99)]
    public decimal DiscountValue { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? MinOrderAmount { get; set; }

    [Required]
    public DateTime ValidFrom { get; set; }

    [Required]
    public DateTime ValidUntil { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxRedemptions { get; set; }

    [Range(1, int.MaxValue)]
    public int PerUserLimit { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}
```

```csharp
// DiscountCodeQuery.cs
namespace DeliverTableSharedLibrary.Dtos.DiscountCode;

public class DiscountCodeQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

**Step 3: Create Loyalty DTOs**

```csharp
// LoyaltyProgramDto.cs
namespace DeliverTableSharedLibrary.Dtos.Loyalty;

public class LoyaltyProgramDto
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public decimal PointsPerEuro { get; set; }
    public decimal EurosPerPoint { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

```csharp
// CreateLoyaltyProgramRequest.cs
using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Loyalty;

public class CreateLoyaltyProgramRequest
{
    [Required]
    [Range(0.01, 999.99)]
    public decimal PointsPerEuro { get; set; } = 1.0m;

    [Required]
    [Range(0.0001, 999.99)]
    public decimal EurosPerPoint { get; set; } = 0.10m;
}
```

```csharp
// LoyaltyAccountDto.cs
namespace DeliverTableSharedLibrary.Dtos.Loyalty;

public class LoyaltyAccountDto
{
    public int Id { get; set; }
    public int PointsBalance { get; set; }
    public decimal EuroEquivalent { get; set; }
    public decimal PointsPerEuro { get; set; }
    public decimal EurosPerPoint { get; set; }
}
```

**Step 4: Commit**

```bash
git add DeliverTableSharedLibrary/Dtos/Promotion/ DeliverTableSharedLibrary/Dtos/DiscountCode/ DeliverTableSharedLibrary/Dtos/Loyalty/
git commit -m "feat(shared): add DTOs for promotions, discount codes, and loyalty"
```

---

## Task 10: Add Mappers

**Files:**
- Create: `DeliverTableServer/Mappers/PromotionMapper.cs`
- Create: `DeliverTableServer/Mappers/DiscountCodeMapper.cs`
- Create: `DeliverTableServer/Mappers/LoyaltyMapper.cs`
- Create: `DeliverTableServer/Mappers/OrderDiscountMapper.cs`
- Modify: `DeliverTableServer/Mappers/OrderMapper.cs`

**Step 1: Create mappers**

```csharp
// PromotionMapper.cs
using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.Promotion;

namespace DeliverTableServer.Mappers;

public static class PromotionMapper
{
    public static PromotionDto ToDto(this Promotion promotion)
    {
        return new PromotionDto
        {
            Id = promotion.Id,
            RestaurantId = promotion.RestaurantId,
            Name = promotion.Name,
            Description = promotion.Description,
            PromotionType = promotion.PromotionType.ToString(),
            DiscountType = promotion.DiscountType.ToString(),
            DiscountValue = promotion.DiscountValue,
            MinOrderAmount = promotion.MinOrderAmount,
            StartsAt = promotion.StartsAt,
            EndsAt = promotion.EndsAt,
            IsActive = promotion.IsActive,
            DishIds = promotion.PromotionDishes.Select(pd => pd.DishId).ToList(),
            CreatedAt = promotion.CreatedAt
        };
    }
}
```

```csharp
// DiscountCodeMapper.cs
using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.DiscountCode;

namespace DeliverTableServer.Mappers;

public static class DiscountCodeMapper
{
    public static DiscountCodeDto ToDto(this DiscountCode code)
    {
        return new DiscountCodeDto
        {
            Id = code.Id,
            RestaurantId = code.RestaurantId,
            Code = code.Code,
            Description = code.Description,
            DiscountType = code.DiscountType.ToString(),
            DiscountValue = code.DiscountValue,
            MinOrderAmount = code.MinOrderAmount,
            ValidFrom = code.ValidFrom,
            ValidUntil = code.ValidUntil,
            MaxRedemptions = code.MaxRedemptions,
            PerUserLimit = code.PerUserLimit,
            CurrentRedemptions = code.CurrentRedemptions,
            IsActive = code.IsActive,
            CreatedAt = code.CreatedAt
        };
    }
}
```

```csharp
// LoyaltyMapper.cs
using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.Loyalty;

namespace DeliverTableServer.Mappers;

public static class LoyaltyMapper
{
    public static LoyaltyProgramDto ToDto(this LoyaltyProgram program)
    {
        return new LoyaltyProgramDto
        {
            Id = program.Id,
            RestaurantId = program.RestaurantId,
            PointsPerEuro = program.PointsPerEuro,
            EurosPerPoint = program.EurosPerPoint,
            IsActive = program.IsActive,
            CreatedAt = program.CreatedAt
        };
    }

    public static LoyaltyAccountDto ToDto(this LoyaltyAccount account, LoyaltyProgram program)
    {
        return new LoyaltyAccountDto
        {
            Id = account.Id,
            PointsBalance = account.PointsBalance,
            EuroEquivalent = account.PointsBalance * program.EurosPerPoint,
            PointsPerEuro = program.PointsPerEuro,
            EurosPerPoint = program.EurosPerPoint
        };
    }
}
```

```csharp
// OrderDiscountMapper.cs
using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.Order;

namespace DeliverTableServer.Mappers;

public static class OrderDiscountMapper
{
    public static OrderDiscountDto ToDto(this OrderDiscount discount)
    {
        return new OrderDiscountDto
        {
            Source = discount.Source.ToString(),
            Description = discount.Description,
            Amount = discount.Amount
        };
    }
}
```

**Step 2: Update OrderMapper.ToDto to include new fields**

In `DeliverTableServer/Mappers/OrderMapper.cs`, add to the `ToDto` method's return object:

```csharp
OriginalAmount = order.OriginalAmount,
DiscountAmount = order.DiscountAmount,
LoyaltyPointsUsed = order.LoyaltyPointsUsed,
LoyaltyPointsEarned = order.LoyaltyPointsEarned,
Discounts = order.Discounts.Select(d => d.ToDto()).ToList(),
```

**Step 3: Commit**

```bash
git add DeliverTableServer/Mappers/
git commit -m "feat(server): add mappers for promotions, discount codes, loyalty, and order discounts"
```

---

## Task 11: Add Repositories

**Files:**
- Create: `DeliverTableServer/Repositories/Interfaces/IPromotionRepository.cs`
- Create: `DeliverTableServer/Repositories/PromotionRepository.cs`
- Create: `DeliverTableServer/Repositories/Interfaces/IDiscountCodeRepository.cs`
- Create: `DeliverTableServer/Repositories/DiscountCodeRepository.cs`
- Create: `DeliverTableServer/Repositories/Interfaces/ILoyaltyRepository.cs`
- Create: `DeliverTableServer/Repositories/LoyaltyRepository.cs`

**Step 1: Create Promotion repository**

```csharp
// IPromotionRepository.cs
using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.Promotion;

namespace DeliverTableServer.Repositories.Interfaces;

public interface IPromotionRepository
{
    Task<Promotion> CreateAsync(Promotion promotion, CancellationToken ct = default);
    Task<Promotion?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(List<Promotion> Items, int TotalCount)> GetByRestaurantAsync(int restaurantId, PromotionQuery query, CancellationToken ct = default);
    Task<List<Promotion>> GetActiveByRestaurantAsync(int restaurantId, CancellationToken ct = default);
    Task<Promotion> UpdateAsync(Promotion promotion, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
```

```csharp
// PromotionRepository.cs
using DeliverTableServer.Data;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.Promotion;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories;

public class PromotionRepository(DeliverTableContext dbContext) : IPromotionRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<Promotion> CreateAsync(Promotion promotion, CancellationToken ct = default)
    {
        _dbContext.Promotions.Add(promotion);
        await _dbContext.SaveChangesAsync(ct);
        return promotion;
    }

    public async Task<Promotion?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Promotions
            .Include(p => p.PromotionDishes)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<(List<Promotion> Items, int TotalCount)> GetByRestaurantAsync(
        int restaurantId, PromotionQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.Promotions
            .Include(p => p.PromotionDishes)
            .Where(p => p.RestaurantId == restaurantId)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await q.CountAsync(ct);
        int page = query.PageNumber > 0 ? query.PageNumber : 1;
        int skip = (page - 1) * query.PageSize;
        var items = await q.Skip(skip).Take(query.PageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<List<Promotion>> GetActiveByRestaurantAsync(int restaurantId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _dbContext.Promotions
            .Include(p => p.PromotionDishes)
            .Where(p => p.RestaurantId == restaurantId && p.IsActive && p.StartsAt <= now && p.EndsAt >= now)
            .ToListAsync(ct);
    }

    public async Task<Promotion> UpdateAsync(Promotion promotion, CancellationToken ct = default)
    {
        promotion.UpdatedAt = DateTime.UtcNow;
        _dbContext.Promotions.Update(promotion);
        await _dbContext.SaveChangesAsync(ct);
        return promotion;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var promotion = await _dbContext.Promotions.FindAsync([id], ct);
        if (promotion is null) return false;
        _dbContext.Promotions.Remove(promotion);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }
}
```

**Step 2: Create DiscountCode repository**

```csharp
// IDiscountCodeRepository.cs
using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.DiscountCode;

namespace DeliverTableServer.Repositories.Interfaces;

public interface IDiscountCodeRepository
{
    Task<DiscountCode> CreateAsync(DiscountCode code, CancellationToken ct = default);
    Task<DiscountCode?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<DiscountCode?> GetByCodeAndRestaurantAsync(string code, int restaurantId, CancellationToken ct = default);
    Task<(List<DiscountCode> Items, int TotalCount)> GetByRestaurantAsync(int restaurantId, DiscountCodeQuery query, CancellationToken ct = default);
    Task<int> GetRedemptionCountByUserAsync(int discountCodeId, int customerId, CancellationToken ct = default);
    Task<DiscountCode> UpdateAsync(DiscountCode code, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<DiscountCodeRedemption> CreateRedemptionAsync(DiscountCodeRedemption redemption, CancellationToken ct = default);
}
```

```csharp
// DiscountCodeRepository.cs
using DeliverTableServer.Data;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.DiscountCode;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories;

public class DiscountCodeRepository(DeliverTableContext dbContext) : IDiscountCodeRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<DiscountCode> CreateAsync(DiscountCode code, CancellationToken ct = default)
    {
        _dbContext.DiscountCodes.Add(code);
        await _dbContext.SaveChangesAsync(ct);
        return code;
    }

    public async Task<DiscountCode?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.DiscountCodes.FirstOrDefaultAsync(dc => dc.Id == id, ct);
    }

    public async Task<DiscountCode?> GetByCodeAndRestaurantAsync(string code, int restaurantId, CancellationToken ct = default)
    {
        return await _dbContext.DiscountCodes
            .FirstOrDefaultAsync(dc => dc.Code == code && dc.RestaurantId == restaurantId, ct);
    }

    public async Task<(List<DiscountCode> Items, int TotalCount)> GetByRestaurantAsync(
        int restaurantId, DiscountCodeQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.DiscountCodes
            .Where(dc => dc.RestaurantId == restaurantId)
            .OrderByDescending(dc => dc.CreatedAt);

        var totalCount = await q.CountAsync(ct);
        int page = query.PageNumber > 0 ? query.PageNumber : 1;
        int skip = (page - 1) * query.PageSize;
        var items = await q.Skip(skip).Take(query.PageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<int> GetRedemptionCountByUserAsync(int discountCodeId, int customerId, CancellationToken ct = default)
    {
        return await _dbContext.DiscountCodeRedemptions
            .CountAsync(r => r.DiscountCodeId == discountCodeId && r.CustomerId == customerId, ct);
    }

    public async Task<DiscountCode> UpdateAsync(DiscountCode code, CancellationToken ct = default)
    {
        code.UpdatedAt = DateTime.UtcNow;
        _dbContext.DiscountCodes.Update(code);
        await _dbContext.SaveChangesAsync(ct);
        return code;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var code = await _dbContext.DiscountCodes.FindAsync([id], ct);
        if (code is null) return false;
        _dbContext.DiscountCodes.Remove(code);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<DiscountCodeRedemption> CreateRedemptionAsync(DiscountCodeRedemption redemption, CancellationToken ct = default)
    {
        _dbContext.DiscountCodeRedemptions.Add(redemption);
        await _dbContext.SaveChangesAsync(ct);
        return redemption;
    }
}
```

**Step 3: Create Loyalty repository**

```csharp
// ILoyaltyRepository.cs
using DeliverTableServer.Models;

namespace DeliverTableServer.Repositories.Interfaces;

public interface ILoyaltyRepository
{
    Task<LoyaltyProgram> CreateAsync(LoyaltyProgram program, CancellationToken ct = default);
    Task<LoyaltyProgram?> GetByRestaurantAsync(int restaurantId, CancellationToken ct = default);
    Task<LoyaltyProgram> UpdateAsync(LoyaltyProgram program, CancellationToken ct = default);
    Task<LoyaltyAccount?> GetAccountAsync(int programId, int customerId, CancellationToken ct = default);
    Task<LoyaltyAccount> CreateAccountAsync(LoyaltyAccount account, CancellationToken ct = default);
    Task<LoyaltyAccount> UpdateAccountAsync(LoyaltyAccount account, CancellationToken ct = default);
    Task<LoyaltyTransaction> CreateTransactionAsync(LoyaltyTransaction transaction, CancellationToken ct = default);
}
```

```csharp
// LoyaltyRepository.cs
using DeliverTableServer.Data;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories;

public class LoyaltyRepository(DeliverTableContext dbContext) : ILoyaltyRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<LoyaltyProgram> CreateAsync(LoyaltyProgram program, CancellationToken ct = default)
    {
        _dbContext.LoyaltyPrograms.Add(program);
        await _dbContext.SaveChangesAsync(ct);
        return program;
    }

    public async Task<LoyaltyProgram?> GetByRestaurantAsync(int restaurantId, CancellationToken ct = default)
    {
        return await _dbContext.LoyaltyPrograms
            .FirstOrDefaultAsync(lp => lp.RestaurantId == restaurantId, ct);
    }

    public async Task<LoyaltyProgram> UpdateAsync(LoyaltyProgram program, CancellationToken ct = default)
    {
        program.UpdatedAt = DateTime.UtcNow;
        _dbContext.LoyaltyPrograms.Update(program);
        await _dbContext.SaveChangesAsync(ct);
        return program;
    }

    public async Task<LoyaltyAccount?> GetAccountAsync(int programId, int customerId, CancellationToken ct = default)
    {
        return await _dbContext.LoyaltyAccounts
            .FirstOrDefaultAsync(la => la.LoyaltyProgramId == programId && la.CustomerId == customerId, ct);
    }

    public async Task<LoyaltyAccount> CreateAccountAsync(LoyaltyAccount account, CancellationToken ct = default)
    {
        _dbContext.LoyaltyAccounts.Add(account);
        await _dbContext.SaveChangesAsync(ct);
        return account;
    }

    public async Task<LoyaltyAccount> UpdateAccountAsync(LoyaltyAccount account, CancellationToken ct = default)
    {
        account.UpdatedAt = DateTime.UtcNow;
        _dbContext.LoyaltyAccounts.Update(account);
        await _dbContext.SaveChangesAsync(ct);
        return account;
    }

    public async Task<LoyaltyTransaction> CreateTransactionAsync(LoyaltyTransaction transaction, CancellationToken ct = default)
    {
        _dbContext.LoyaltyTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync(ct);
        return transaction;
    }
}
```

**Step 4: Commit**

```bash
git add DeliverTableServer/Repositories/
git commit -m "feat(server): add repositories for promotions, discount codes, and loyalty"
```

---

## Task 12: Add PromotionService with Tests (TDD)

**Files:**
- Create: `DeliverTableServer/Services/Interfaces/IPromotionService.cs`
- Create: `DeliverTableServer/Services/PromotionService.cs`
- Create: `DeliverTableTests/Server/Unit/Services/PromotionServiceTests.cs`

The service must validate: restaurant ownership, date ranges, dish ownership (for ItemBased), enum parsing. Follow TDD — write tests first, then implementation.

Key test scenarios:
- CreateAsync: success for each promotion type, invalid dates, invalid enums, dish not from restaurant
- GetByRestaurantAsync: success, not owner
- UpdateAsync: success, not found, not owner
- DeleteAsync: success, not found, not owner

Pattern: mock `IPromotionRepository`, `IRestaurantRepository`, `IDishRepository`. Return `ServiceResult<PromotionDto>` or `ServiceResult<PaginatedResult<PromotionDto>>`.

**Commit message:** `feat(server): add PromotionService with tests`

---

## Task 13: Add DiscountCodeService with Tests (TDD)

**Files:**
- Create: `DeliverTableServer/Services/Interfaces/IDiscountCodeService.cs`
- Create: `DeliverTableServer/Services/DiscountCodeService.cs`
- Create: `DeliverTableTests/Server/Unit/Services/DiscountCodeServiceTests.cs`

Key test scenarios:
- CreateAsync: success, duplicate code, invalid dates, invalid enums
- GetByRestaurantAsync: success, not owner
- UpdateAsync: success, not found, not owner
- DeleteAsync: success, not found, not owner
- ValidateCodeAsync (internal, used by checkout): valid code, expired, max redemptions reached, per-user limit reached, min order not met, inactive

**Commit message:** `feat(server): add DiscountCodeService with tests`

---

## Task 14: Add LoyaltyService with Tests (TDD)

**Files:**
- Create: `DeliverTableServer/Services/Interfaces/ILoyaltyService.cs`
- Create: `DeliverTableServer/Services/LoyaltyService.cs`
- Create: `DeliverTableTests/Server/Unit/Services/LoyaltyServiceTests.cs`

Key test scenarios:
- CreateOrUpdateProgramAsync: create new, update existing, not owner
- GetProgramAsync: success, not found
- GetMyAccountAsync: success, auto-create account if none exists, no program
- EarnPointsAsync (called on Delivered): calculate from original amount, create account if needed
- RedeemPointsAsync: success, insufficient points, program not active

**Commit message:** `feat(server): add LoyaltyService with tests`

---

## Task 15: Add Controllers with Tests (TDD)

**Files:**
- Create: `DeliverTableServer/Controllers/PromotionController.cs`
- Create: `DeliverTableServer/Controllers/DiscountCodeController.cs`
- Create: `DeliverTableServer/Controllers/LoyaltyController.cs`
- Create: `DeliverTableTests/Server/Unit/Controllers/PromotionControllerTests.cs`
- Create: `DeliverTableTests/Server/Unit/Controllers/DiscountCodeControllerTests.cs`
- Create: `DeliverTableTests/Server/Unit/Controllers/LoyaltyControllerTests.cs`

**PromotionController:**
- `[Route(ApiRoutes.Promotion.RestaurantBaseRoute)]` for create/list (takes restaurant `{id}`)
- Separate route `[Route(ApiRoutes.Promotion.Base)]` for update/delete (takes promotion `{id}`)
- All `[Authorize(Roles = nameof(UserRole.RestaurantOwner))]`

**DiscountCodeController:** Same pattern as PromotionController.

**LoyaltyController:**
- `[Route(ApiRoutes.Loyalty.RestaurantBaseRoute)]` (takes restaurant `{id}`)
- POST: create/update program (RestaurantOwner)
- GET: get program info (public, `[AllowAnonymous]`)
- GET `my-account`: get customer's points (Customer role)

**Commit message:** `feat(server): add controllers for promotions, discount codes, and loyalty with tests`

---

## Task 16: Register All Services in DI

**Files:**
- Modify: `DeliverTableServer/Extensions/ServiceCollectionExtensions.cs`

Add to `RegisterRepositories`:
```csharp
services.AddScoped<IPromotionRepository, PromotionRepository>();
services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();
services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();
```

Add to `RegisterServices`:
```csharp
services.AddScoped<IPromotionService, PromotionService>();
services.AddScoped<IDiscountCodeService, DiscountCodeService>();
services.AddScoped<ILoyaltyService, LoyaltyService>();
```

**Commit message:** `feat(server): register promotion, discount code, and loyalty services in DI`

---

## Task 17: Integrate Discounts into OrderService.CreateFromCartAsync (TDD)

**Files:**
- Modify: `DeliverTableServer/Services/OrderService.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/OrderServiceTests.cs`

This is the most complex task. Modify `CreateFromCartAsync` to:

1. Calculate `OriginalAmount` (same as current `TotalAmount` calculation)
2. Apply active promotions (get from `IPromotionRepository.GetActiveByRestaurantAsync`):
   - `Automatic`: apply to full order
   - `Threshold`: apply if OriginalAmount >= MinOrderAmount
   - `ItemBased`: apply to matching dishes only
   - For Percentage: `discount = targetAmount * (DiscountValue / 100)`
   - For FixedAmount: `discount = DiscountValue`
   - Create `OrderDiscount` for each
3. Apply discount code (if `request.DiscountCode` is not null):
   - Validate via service/repository
   - Calculate discount
   - Create `OrderDiscount`, `DiscountCodeRedemption`, increment `CurrentRedemptions`
4. Apply loyalty points (if `request.LoyaltyPointsToRedeem > 0`):
   - Validate account, balance
   - Convert to euros, cap at remaining amount
   - Deduct points, create `LoyaltyTransaction`, create `OrderDiscount`
5. Set `DiscountAmount`, `TotalAmount = OriginalAmount - DiscountAmount` (min 0)
6. Set `LoyaltyPointsUsed`

New dependencies for OrderService: `IPromotionRepository`, `IDiscountCodeRepository`, `ILoyaltyRepository`

**Key test scenarios:**
- Order with no discounts (backwards compatible)
- Order with one automatic promotion (percentage)
- Order with one automatic promotion (fixed amount)
- Order with threshold promotion — met / not met
- Order with item-based promotion
- Order with stacked promotions
- Order with valid discount code
- Order with invalid/expired discount code
- Order with loyalty points redemption
- Order with all three stacking
- Total cannot go below 0

**Commit message:** `feat(server): integrate promotions, discount codes, and loyalty into checkout`

---

## Task 18: Earn Loyalty Points on Delivered (TDD)

**Files:**
- Modify: `DeliverTableServer/Services/OrderService.cs`
- Modify: `DeliverTableTests/Server/Unit/Services/OrderServiceTests.cs`

In `UpdateStatusAsync`, after the existing restaurant crediting logic for `Delivered`:

1. Check if restaurant has an active loyalty program
2. Calculate `pointsEarned = floor(order.OriginalAmount * program.PointsPerEuro)`
3. Get or create customer's loyalty account
4. Credit points to account
5. Create `LoyaltyTransaction` (type: Earn)
6. Set `order.LoyaltyPointsEarned = pointsEarned`
7. Save

**Key test scenarios:**
- Delivered order with active loyalty program → points earned
- Delivered order without loyalty program → no points
- Delivered order with inactive loyalty program → no points
- Points calculated on OriginalAmount (not TotalAmount)

**Commit message:** `feat(server): earn loyalty points when order is delivered`

---

## Task 19: Frontend — Restaurant Management Pages

**Files (per domain):**

### Promotions
- Create: `DeliverTableClient/Services/Interfaces/IPromotionService.cs`
- Create: `DeliverTableClient/Services/PromotionService.cs`
- Create: `DeliverTableClient/Pages/Restaurant/Promotions/RestaurantPromotions.razor`
- Create: `DeliverTableClient/Pages/Restaurant/Promotions/RestaurantPromotions.razor.scss`

### Discount Codes
- Create: `DeliverTableClient/Services/Interfaces/IDiscountCodeService.cs`
- Create: `DeliverTableClient/Services/DiscountCodeService.cs`
- Create: `DeliverTableClient/Pages/Restaurant/DiscountCodes/RestaurantDiscountCodes.razor`
- Create: `DeliverTableClient/Pages/Restaurant/DiscountCodes/RestaurantDiscountCodes.razor.scss`

### Loyalty
- Create: `DeliverTableClient/Services/Interfaces/ILoyaltyService.cs`
- Create: `DeliverTableClient/Services/LoyaltyService.cs`
- Create: `DeliverTableClient/Pages/Restaurant/Loyalty/RestaurantLoyalty.razor`
- Create: `DeliverTableClient/Pages/Restaurant/Loyalty/RestaurantLoyalty.razor.scss`

### Navigation
- Modify: `DeliverTableClient/Components/Lists/RestaurantList.razor` — add buttons for promotions, discount codes, loyalty
- Modify: `DeliverTableClient/Extensions/ApiClientServiceCollectionExtensions.cs` — register new services
- Modify: `DeliverTableClient/_Imports.razor` — add global usings for new DTO namespaces

Each page follows the existing pattern (see RestaurantOrders.razor):
- Route: `/restaurant/{RestaurantId:int}/promotions`, `/restaurant/{RestaurantId:int}/codes-promo`, `/restaurant/{RestaurantId:int}/fidelite`
- `@attribute [Authorize(Roles = nameof(UserRole.RestaurantOwner))]`
- Paginated list + create/edit forms using `EditForm`
- Loading states, error handling

**Commit message:** `feat(client): add restaurant management pages for promotions, discount codes, and loyalty`

---

## Task 20: Frontend — Checkout Integration

**Files:**
- Modify existing checkout/order creation page to show:
  - Auto-applied promotions with line-by-line breakdown
  - Discount code input field with "Appliquer" button
  - Loyalty points section (if restaurant has a program and customer has points)
  - Order summary: original total, each discount line, final total

This requires:
- Adding a preview/calculation endpoint or doing client-side calculation
- Modifying the order creation form to include `DiscountCode` and `LoyaltyPointsToRedeem`
- Displaying `OrderDto.Discounts` after order creation

**Commit message:** `feat(client): integrate promotions, discount codes, and loyalty into checkout`

---

## Task 21: Update ER Diagram Documentation

**Files:**
- Modify: `docs/db/er-diagram.md`

Add all new entities and relationships to the Mermaid diagram. No implementation entities need to be added since they already exist in the ER diagram — verify and update if the implemented schema differs from the original design.

**Commit message:** `docs(db): update ER diagram with implemented promotions, discounts, and loyalty schema`

---

## Task 22: Final Verification

**Step 1:** `make format-fix`
**Step 2:** `make test` — all tests pass
**Step 3:** Commit any formatting fixes: `style: apply formatting fixes`
