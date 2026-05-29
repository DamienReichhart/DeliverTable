# Monthly Commission Statements Implementation Plan — Part 3

Continuation of [part 2](2026-05-29-monthly-commission-statements-part-2.md). Same goal, same architecture, same AB refs (`PBI: AB#5994` / `Task: AB#6012`).

---

## Phase 8 — Wire refund into `PaymentService`

### Task 24: Failing test for `HandleChargeRefundedAsync` calling commission refund handler

**Files:**
- Modify: `DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs` (or create if missing)

- [ ] **Step 1: Add test**

```csharp
[Test]
public async Task HandleChargeRefundedAsync_InvokesCommissionRefundHandler_ForEachNewRefund()
{
    // Arrange a Stripe.Charge with one refund event.
    var charge = StripeFakes.ChargeWithRefund(
        chargePaymentIntentId: "pi_123",
        refundId: "re_abc",
        refundAmount: 30m);

    var payment = TestEntities.Payment(id: 11, paymentIntentId: "pi_123", amount: 100m);
    _paymentRepo.GetByStripePaymentIntentIdAsync("pi_123", default).Returns(payment);
    _paymentRepo.GetRefundByStripeIdAsync("re_abc", default).Returns((Refund?)null);
    _paymentRepo.AddRefundAsync(Arg.Any<Refund>(), default)
        .Returns(ci => Task.FromResult(((Refund)ci[0]).Tap(r => r.Id = 77)));
    _paymentRepo.GetTotalRefundedAsync(11, default).Returns(30m);
    _orderRepo.GetByIdAsync(payment.OrderId, default).Returns(TestEntities.DeliveredOrder(id: payment.OrderId));

    var deferred = new List<Func<Task>>();
    await _sut.HandleStripeEventAsync(StripeFakes.RefundedEvent(charge), default);

    await _commissionService.Received(1).HandleRefundForPriorPeriodAsync(
        payment.OrderId, 77, "re_abc", 30m, default);
}
```

(`Tap` is a tiny test extension: `public static T Tap<T>(this T x, Action<T> act) { act(x); return x; }` — add to `DeliverTableTests/Server/Fixtures/`.)

- [ ] **Step 2: Run, expect failure**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~HandleChargeRefundedAsync_InvokesCommissionRefundHandler"
```
Expected: FAIL — `_commissionService` is not yet injected.

- [ ] **Step 3: Commit**

```bash
git add DeliverTableTests/Server/Unit/Services/PaymentServiceTests.cs DeliverTableTests/Server/Fixtures/
git commit -m "$(cat <<'EOF'
test(server): add failing test wiring refund webhook to commission handler

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

### Task 25: Wire `ICommissionStatementService` into `PaymentService`

**Files:**
- Modify: `DeliverTableServer/Services/PaymentService.cs`

- [ ] **Step 1: Inject `ICommissionStatementService`**

Add `ICommissionStatementService commissionStatementService` to `PaymentService`'s primary constructor (alongside the existing `invoiceService`).

- [ ] **Step 2: Inside `HandleChargeRefundedAsync` loop over `newRefundIds`, call the new service**

Locate the existing block:

```csharp
foreach (var newRefundId in newRefundIds)
{
    var cnResult = await invoiceService.CreateCreditNotesForRefundAsync(newRefundId, ct);
    // ...
}
```

After the existing `invoiceService.CreateCreditNotesForRefundAsync` call (which we keep for legacy pre-cutover commission invoices and customer credit notes), add — inside the same loop — a call to the new monthly handler:

```csharp
// Look up the newly-created refund row to get its stripe id + amount.
var newRefund = await paymentRepository.GetRefundByIdAsync(newRefundId, ct);
if (newRefund is not null)
{
    await commissionStatementService.HandleRefundForPriorPeriodAsync(
        order.Id, newRefund.Id, newRefund.StripeRefundId, newRefund.Amount, ct);
}
```

If `GetRefundByIdAsync` does not exist on `IPaymentRepository`, add it as a thin wrapper.

- [ ] **Step 3: Run test, expect pass**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~HandleChargeRefundedAsync"
```
Expected: PASS (and pre-existing tests still pass).

- [ ] **Step 4: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```
Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableServer/Services/PaymentService.cs DeliverTableInfrastructure/Repositories/PaymentRepository.cs DeliverTableInfrastructure/Repositories/Interfaces/IPaymentRepository.cs
git commit -m "$(cat <<'EOF'
feat(server): route refund webhook to commission statement handler

PBI: AB#5994
Task: AB#6012
EOF
)"
```

(Adjust paths if `GetRefundByIdAsync` was already present and didn't need changes.)

---

## Phase 9 — Admin controller (TDD)

### Task 26: Failing controller test

**Files:**
- Create: `DeliverTableTests/Server/Unit/Controllers/AdminCommissionStatementControllerTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminCommissionStatementControllerTests
{
    private ICommissionStatementService _service = null!;
    private AdminCommissionStatementController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<ICommissionStatementService>();
        _sut = new AdminCommissionStatementController(_service);
    }

    [Test]
    public async Task Run_DefaultsToPreviousMonth_WhenBodyOmitted()
    {
        // Freeze "now" via a test clock if available; otherwise compute expected from DateTime.UtcNow.
        var nowParis = new DateTime(2026, 6, 5, 9, 0, 0, DateTimeKind.Utc); // mid-June
        _sut.UtcNowOverride = nowParis; // see below — small test seam on controller
        var expectedDto = new CommissionStatementGenerationResultDto { PeriodYear = 2026, PeriodMonth = 5 };
        _service.GenerateForPeriodAsync(2026, 5, default)
                .ReturnsForAnyArgs(ServiceResult<CommissionStatementGenerationResultDto>.Success(expectedDto));

        var result = await _sut.Run(body: null, default);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        await _service.Received(1).GenerateForPeriodAsync(2026, 5, default);
    }

    [Test]
    public async Task Run_UsesProvidedPeriod()
    {
        _service.GenerateForPeriodAsync(2026, 3, default)
                .Returns(ServiceResult<CommissionStatementGenerationResultDto>.Success(new()));

        var result = await _sut.Run(new CommissionStatementsRunRequest { Year = 2026, Month = 3 }, default);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        await _service.Received(1).GenerateForPeriodAsync(2026, 3, default);
    }
}
```

You'll also need a small request DTO — add `CommissionStatementsRunRequest` to `DeliverTableSharedLibrary/Dtos/` in the same task (since the spec calls it out as the admin endpoint body).

- [ ] **Step 2: Run, expect compile failure**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~AdminCommissionStatementControllerTests"
```
Expected: build fails (controller missing).

- [ ] **Step 3: Commit**

```bash
git add DeliverTableTests/Server/Unit/Controllers/AdminCommissionStatementControllerTests.cs
git commit -m "$(cat <<'EOF'
test(server): add failing tests for admin commission statement controller

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

### Task 27: Implement controller

**Files:**
- Create: `DeliverTableServer/Controllers/AdminCommissionStatementController.cs`
- Create: `DeliverTableSharedLibrary/Dtos/CommissionStatementsRunRequest.cs`

- [ ] **Step 1: Add request DTO**

```csharp
namespace DeliverTableSharedLibrary.Dtos;

public sealed class CommissionStatementsRunRequest
{
    public int? Year { get; set; }
    public int? Month { get; set; }
}
```

- [ ] **Step 2: Add controller**

```csharp
using DeliverTableServer.Extensions;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Admin.Base)]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminCommissionStatementController(
    ICommissionStatementService commissionStatementService) : ControllerBase
{
    // Test seam — overridden in unit tests; in production reads UtcNow.
    public DateTime? UtcNowOverride { get; set; }

    private static readonly TimeZoneInfo ParisTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    [HttpPost(ApiRoutes.Admin.CommissionStatementsRunRoute)]
    public async Task<IActionResult> Run(
        [FromBody] CommissionStatementsRunRequest? body, CancellationToken ct)
    {
        int year, month;
        if (body?.Year is int y && body.Month is int m)
        {
            year = y;
            month = m;
        }
        else
        {
            var nowUtc = UtcNowOverride ?? DateTime.UtcNow;
            var nowParis = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ParisTz);
            var prev = nowParis.AddMonths(-1);
            year = prev.Year;
            month = prev.Month;
        }

        var result = await commissionStatementService.GenerateForPeriodAsync(year, month, ct);
        return result.ToOkResult();
    }
}
```

- [ ] **Step 3: Run tests, expect pass**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~AdminCommissionStatementControllerTests"
```
Expected: PASS.

- [ ] **Step 4: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```
Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableServer/Controllers/AdminCommissionStatementController.cs DeliverTableSharedLibrary/Dtos/CommissionStatementsRunRequest.cs
git commit -m "$(cat <<'EOF'
feat(server): add admin commission statement run controller

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

## Phase 10 — Scheduler: Quartz + monthly job

### Task 28: Add Quartz packages and SchedulerEnvironment confirmation

**Files:**
- Modify: `DeliverTableScheduler/DeliverTableScheduler.csproj`

- [ ] **Step 1: Add Quartz packages**

```bash
docker compose -f docker-dev.yaml exec backend dotnet add /src/DeliverTableScheduler/DeliverTableScheduler.csproj package Quartz
docker compose -f docker-dev.yaml exec backend dotnet add /src/DeliverTableScheduler/DeliverTableScheduler.csproj package Quartz.Extensions.Hosting
```

- [ ] **Step 2: Build**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
```
Expected: build succeeds.

- [ ] **Step 3: Confirm Europe/Paris timezone resolves in the scheduler image**

```bash
docker compose -f docker-dev.yaml exec scheduler dotnet fsi --use:- <<'EOF'
printfn "%s" (System.TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris").DisplayName)
EOF
```
If this fails with "TimeZoneNotFoundException", edit `docker/images/scheduler.dev.dockerfile` to install `tzdata`:

```dockerfile
RUN apt-get update && apt-get install -y --no-install-recommends tzdata && rm -rf /var/lib/apt/lists/*
ENV TZ=Etc/UTC
```

Rebuild the scheduler image:

```bash
docker compose -f docker-dev.yaml build scheduler
```

Re-verify.

- [ ] **Step 4: Commit**

```bash
git add DeliverTableScheduler/DeliverTableScheduler.csproj DeliverTableScheduler/packages.lock.json docker/images/scheduler.dev.dockerfile
git commit -m "$(cat <<'EOF'
build(scheduler): add Quartz.NET and ensure Europe/Paris tzdata

PBI: AB#5994
Task: AB#6012
EOF
)"
```

(If the Dockerfile didn't need changes, omit it.)

---

### Task 29: Quartz job — failing test

**Files:**
- Create: `DeliverTableTests/Scheduler/Unit/MonthlyCommissionStatementJobTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using DeliverTableScheduler.Jobs;
using DeliverTableServer.Common;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos;
using NSubstitute;
using NUnit.Framework;
using Quartz;

namespace DeliverTableTests.Scheduler.Unit;

[TestFixture]
public class MonthlyCommissionStatementJobTests
{
    [Test]
    public async Task Execute_DelegatesToService_WithPreviousMonth()
    {
        var service = Substitute.For<ICommissionStatementService>();
        var sut = new MonthlyCommissionStatementJob(service, NullLoggerFactory.Instance)
        {
            UtcNowOverride = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        await sut.Execute(Substitute.For<IJobExecutionContext>());

        await service.Received(1).GenerateForPeriodAsync(2026, 5, default);
    }

    [Test]
    public async Task Execute_HandlesDstTransition_Correctly()
    {
        var service = Substitute.For<ICommissionStatementService>();
        var sut = new MonthlyCommissionStatementJob(service, NullLoggerFactory.Instance)
        {
            // April 1 2026 00:00 UTC = April 1 02:00 Paris (summer time start in late March).
            UtcNowOverride = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        await sut.Execute(Substitute.For<IJobExecutionContext>());

        await service.Received(1).GenerateForPeriodAsync(2026, 3, default);
    }
}
```

- [ ] **Step 2: Run, expect compile failure**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~MonthlyCommissionStatementJobTests"
```
Expected: FAIL (job class missing).

- [ ] **Step 3: Commit**

```bash
git add DeliverTableTests/Scheduler/Unit/MonthlyCommissionStatementJobTests.cs
git commit -m "$(cat <<'EOF'
test(scheduler): add failing tests for monthly commission job

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

### Task 30: Implement Quartz job

**Files:**
- Create: `DeliverTableScheduler/Jobs/MonthlyCommissionStatementJob.cs`

- [ ] **Step 1: Implement**

```csharp
using DeliverTableServer.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DeliverTableScheduler.Jobs;

[DisallowConcurrentExecution]
public sealed class MonthlyCommissionStatementJob(
    ICommissionStatementService service,
    ILoggerFactory loggerFactory) : IJob
{
    private static readonly TimeZoneInfo ParisTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
    private readonly ILogger _logger = loggerFactory.CreateLogger<MonthlyCommissionStatementJob>();

    public DateTime? UtcNowOverride { get; set; }

    public async Task Execute(IJobExecutionContext context)
    {
        var nowUtc = UtcNowOverride ?? DateTime.UtcNow;
        var nowParis = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ParisTz);
        var prev = nowParis.AddMonths(-1);
        var year = prev.Year;
        var month = prev.Month;

        _logger.LogInformation("Running monthly commission statement job for {Year}-{Month:D2}", year, month);
        var result = await service.GenerateForPeriodAsync(year, month, context.CancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("Monthly commission job failed: {Reason}", result.Error?.Message);
            return;
        }
        var v = result.Value!;
        _logger.LogInformation(
            "Monthly commission job done: processed={Processed} created={Created} skipped={Skipped} failed={Failed}",
            v.RestaurantsProcessed, v.StatementsCreated, v.RestaurantsSkipped, v.Failures.Count);
    }
}
```

- [ ] **Step 2: Run, expect pass**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~MonthlyCommissionStatementJobTests"
```
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add DeliverTableScheduler/Jobs/MonthlyCommissionStatementJob.cs
git commit -m "$(cat <<'EOF'
feat(scheduler): implement monthly commission statement Quartz job

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

### Task 31: Wire Quartz into scheduler Program.cs + register service dependencies

**Files:**
- Modify: `DeliverTableScheduler/Program.cs`

- [ ] **Step 1: Add scheduler-side registration of `ICommissionStatementService` dependencies**

The scheduler currently has its own DI graph (it doesn't call `AddDeliverTableServices`). Register only what the new job actually needs.

Add to `DeliverTableScheduler/Program.cs` after the existing `AddScoped` lines and before `await builder.Build().RunAsync();`:

```csharp
// Repositories required by the commission statement service.
builder.Services.AddScoped<DeliverTableInfrastructure.Repositories.Interfaces.ICommissionStatementRepository,
    DeliverTableInfrastructure.Repositories.CommissionStatementRepository>();
builder.Services.AddScoped<DeliverTableInfrastructure.Repositories.Interfaces.IRestaurantRepository,
    DeliverTableInfrastructure.Repositories.RestaurantRepository>();

// AppEnvironment from the server project (loads commission rate, VAT, legal info from env vars).
var appEnv = DeliverTableServer.Configuration.AppEnvironment.Load();
builder.Services.AddSingleton(appEnv);
builder.Services.AddScoped<DeliverTableServer.Services.ICommissionStatementService,
    DeliverTableServer.Services.CommissionStatementService>();

// RabbitMQ publisher (same config wiring as DeliverTableWorker/Program.cs).
var rabbitConfig = new DeliverTableInfrastructure.Messaging.RabbitMqConfig
{
    Host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq",
    Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672"),
    User = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
    Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest",
};
builder.Services.AddSingleton(rabbitConfig);
builder.Services.AddSingleton<DeliverTableInfrastructure.Messaging.IMessagePublisher>(sp =>
    DeliverTableInfrastructure.Messaging.RabbitMqPublisher.CreateAsync(
        sp.GetRequiredService<DeliverTableInfrastructure.Messaging.RabbitMqConfig>())
      .GetAwaiter().GetResult());
```

If `RabbitMqConfig`'s property names differ from `Host/Port/User/Password`, match whatever the existing worker uses — find it in `DeliverTableInfrastructure/Messaging/RabbitMqConfig.cs`.

- [ ] **Step 2: Add Quartz**

```csharp
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey(nameof(MonthlyCommissionStatementJob));
    q.AddJob<MonthlyCommissionStatementJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(t => t
        .ForJob(jobKey)
        .WithIdentity($"{nameof(MonthlyCommissionStatementJob)}-trigger")
        .WithCronSchedule("0 0 2 1 * ?", c =>
            c.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"))
             .WithMisfireHandlingInstructionFireAndProceed()));
});

builder.Services.AddQuartzHostedService(opts => opts.WaitForJobsToComplete = true);
```

- [ ] **Step 3: Add scheduler env vars to docker-dev.yaml**

In `docker-dev.yaml` under the `scheduler:` service, add the platform legal + commission env vars that `AppEnvironment.Load` requires (mirror the worker block):

```yaml
PLATFORM_LEGAL_NAME: ${PLATFORM_LEGAL_NAME}
PLATFORM_LEGAL_FORM: ${PLATFORM_LEGAL_FORM}
PLATFORM_SIRET: ${PLATFORM_SIRET}
PLATFORM_VAT_NUMBER: ${PLATFORM_VAT_NUMBER}
PLATFORM_ADDRESS: ${PLATFORM_ADDRESS}
PLATFORM_VAT_APPLICABLE: ${PLATFORM_VAT_APPLICABLE:-true}
PLATFORM_COMMISSION_RATE: ${PLATFORM_COMMISSION_RATE:-0.10}
RABBITMQ_HOST: ${RABBITMQ_HOST:-rabbitmq}
RABBITMQ_PORT: "5672"
RABBITMQ_USER: ${RABBITMQ_USER}
RABBITMQ_PASSWORD: ${RABBITMQ_PASSWORD}
```

Mirror the change to `docker-prod.yaml` for the scheduler service.

- [ ] **Step 4: Build + restart stack**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml restart scheduler
```
Expected: build succeeds, scheduler comes up with log line indicating Quartz scheduled the trigger.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableScheduler/Program.cs docker-dev.yaml docker-prod.yaml
git commit -m "$(cat <<'EOF'
feat(scheduler): wire Quartz cron for monthly commission statements

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

## Phase 11 — Worker: PDF renderer + consumer

### Task 32: PDF renderer

**Files:**
- Create: `DeliverTableWorker/Services/ICommissionStatementPdfRenderer.cs`
- Create: `DeliverTableWorker/Services/CommissionStatementPdfRenderer.cs`
- Create: `DeliverTableTests/Worker/Unit/Services/CommissionStatementPdfRendererTests.cs`

- [ ] **Step 1: Interface**

```csharp
using DeliverTableInfrastructure.Models;

namespace DeliverTableWorker.Services;

public interface ICommissionStatementPdfRenderer
{
    byte[] Render(CommissionStatement statement);
}
```

- [ ] **Step 2: Failing test (smoke)**

```csharp
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Server.Factories;
using DeliverTableWorker.Services;
using NUnit.Framework;

namespace DeliverTableTests.Worker.Unit.Services;

[TestFixture]
public class CommissionStatementPdfRendererTests
{
    [Test]
    public void Render_ReturnsNonEmptyPdfBytes_ForInvoice()
    {
        var statement = CommissionStatementFactory.CreateInvoice(7, 2026, 5);
        statement.IssuerLegalSnapshotJson =
            """{"Name":"Platform","LegalForm":"SAS","Siret":"123","VatNumber":"FR1","Address":"X"}""";
        statement.RecipientSnapshotJson =
            """{"Name":"Resto","LegalForm":"SARL","Siret":"456","VatNumber":"FR2","Address":"Y"}""";
        statement.Lines.Add(new()
        {
            OrderId = 1, OrderNumber = "1", OrderCompletedAt = new DateTime(2026, 5, 10),
            OrderTotalAmount = 100m, CommissionRateSnapshot = 0.10m, VatRate = 20m,
            LineHt = 10m, LineVat = 2m, LineTtc = 12m,
        });
        statement.TotalHt = 10m; statement.TotalVat = 2m; statement.TotalTtc = 12m;

        var sut = new CommissionStatementPdfRenderer();

        var bytes = sut.Render(statement);

        Assert.That(bytes.Length, Is.GreaterThan(1000));
        Assert.That(System.Text.Encoding.ASCII.GetString(bytes, 0, 5), Is.EqualTo("%PDF-"));
    }
}
```

- [ ] **Step 3: Run, expect compile failure**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~CommissionStatementPdfRendererTests"
```
Expected: FAIL.

- [ ] **Step 4: Implement renderer**

Mirror [InvoicePdfRenderer.cs](../../../DeliverTableWorker/Services/InvoicePdfRenderer.cs) — same QuestPDF document structure, but:
- Title: `"RELEVÉ DE COMMISSIONS"` (Invoice kind) or `"AVOIR DE COMMISSIONS"` (CreditNote kind).
- Period banner: `$"Période du 1er {MoisFrancais(month)} {year} au {DateTime.DaysInMonth(year, month)} {MoisFrancais(month)} {year}"`.
- Order table columns: `N° commande | Date livraison | Montant TTC | Taux | Commission HT | TVA | Commission TTC`.
- Totals: `Total HT`, `TVA (xx%)`, `Total TTC`.
- Footer: statement number + platform legal mentions (from issuer snapshot).

Write the helper `MoisFrancais(int month)` returning `"janvier"`, `"février"`, etc.

- [ ] **Step 5: Run, expect pass**

```bash
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --filter "FullyQualifiedName~CommissionStatementPdfRendererTests"
```
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add DeliverTableWorker/Services/ICommissionStatementPdfRenderer.cs DeliverTableWorker/Services/CommissionStatementPdfRenderer.cs DeliverTableTests/Worker/Unit/Services/CommissionStatementPdfRendererTests.cs
git commit -m "$(cat <<'EOF'
feat(worker): add commission statement PDF renderer

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

### Task 33: Worker consumer

**Files:**
- Create: `DeliverTableWorker/Consumers/CommissionStatementJobConsumer.cs`
- Modify: `DeliverTableWorker/Program.cs`

- [ ] **Step 1: Implement consumer**

Mirror [InvoiceJobConsumer.cs](../../../DeliverTableWorker/Consumers/InvoiceJobConsumer.cs) end-to-end. Replace:
- `MainQueue` constant → `"delivertable.jobs.commission_statement"`.
- `RoutingKey` → `MessagingExchanges.CommissionStatement`.
- Message type → `CommissionStatementJobMessage`.
- Repository → `ICommissionStatementRepository`.
- Renderer → `ICommissionStatementPdfRenderer`.
- S3 folder → `$"commission-statements/{statement.PeriodYear}/{statement.PeriodMonth:D2}"`.
- File name → `$"{statement.Number}.pdf"`.
- Status updates → `CommissionStatementStatus.Generated` / `Failed`.
- Email job creation: subject and template depend on `statement.Kind`:
  - Invoice: subject = `$"Votre relevé de commissions de {MoisFrancais(statement.PeriodMonth)} {statement.PeriodYear} est disponible"`, template = `"CommissionStatementInvoice"`.
  - CreditNote: subject = `$"Avoir sur commissions — commande {firstLine.OrderNumber}"`, template = `"CommissionStatementCreditNote"`.
- Recipient: `statement.RecipientEmailSnapshot ?? statement.RecipientRestaurant.Owner.Email`.

- [ ] **Step 2: Register in worker Program.cs**

Append to `DeliverTableWorker/Program.cs`:

```csharp
builder.Services.AddScoped<ICommissionStatementRepository, CommissionStatementRepository>();
builder.Services.AddSingleton<ICommissionStatementPdfRenderer, CommissionStatementPdfRenderer>();
builder.Services.AddHostedService<CommissionStatementJobConsumer>();
```

- [ ] **Step 3: Add minimal email templates**

Create two empty placeholder templates in the email templates folder (look for the existing folder via `find DeliverTableWorker -name "*.cshtml"`):

- `CommissionStatementInvoice.cshtml`:
  ```
  Bonjour,
  Veuillez trouver ci-joint votre relevé de commissions @Model.PeriodLabel.
  Cordialement,
  L'équipe DeliverTable
  ```
- `CommissionStatementCreditNote.cshtml`:
  ```
  Bonjour,
  Veuillez trouver ci-joint l'avoir sur commissions pour la commande #@Model.OrderNumber.
  Cordialement,
  L'équipe DeliverTable
  ```

Populate `templateData` accordingly in the consumer.

- [ ] **Step 4: Build + restart worker**

```bash
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln
docker compose -f docker-dev.yaml restart worker
docker compose -f docker-dev.yaml logs worker --tail 50
```
Expected: worker comes up, binds the new queue, logs no errors.

- [ ] **Step 5: Commit**

```bash
git add DeliverTableWorker/Consumers/CommissionStatementJobConsumer.cs DeliverTableWorker/Program.cs DeliverTableWorker/Templates/
git commit -m "$(cat <<'EOF'
feat(worker): add commission statement job consumer and email templates

PBI: AB#5994
Task: AB#6012
EOF
)"
```

(Adjust template path to match the project's actual template folder.)

---

## Phase 12 — End-to-end manual verification + final polish

### Task 34: Manual smoke via admin endpoint

- [ ] **Step 1: Seed test data in the dev DB**

Either via the existing seed scripts or via an ad-hoc SQL/EF script:
- One restaurant with full legal info.
- One delivered, paid, non-refunded order in the previous calendar month (`DeliveredAt` set, `Status=Delivered`, `PaymentStatus=Completed`).

- [ ] **Step 2: Call the admin endpoint**

Use `curl` (or the dev frontend) authenticated as an Administrator:

```bash
curl -X POST http://localhost/api/v1/admin/commission-statements/run \
  -H "Authorization: Bearer <admin-jwt>" \
  -H "Content-Type: application/json" \
  -d '{}'
```
Expected: JSON response with `restaurantsProcessed >= 1`, `statementsCreated >= 1`.

- [ ] **Step 3: Verify**

- Query the `CommissionStatements` table: one row exists with `Status` transitioning `Queued → Generated`.
- Verify the PDF was uploaded to S3 under `commission-statements/YYYY/MM/COMM-...pdf`.
- Verify an `EmailJob` row exists with the right recipient.

- [ ] **Step 4: Re-run for the same period — should be idempotent**

```bash
curl -X POST http://localhost/api/v1/admin/commission-statements/run \
  -H "Authorization: Bearer <admin-jwt>" \
  -H "Content-Type: application/json" \
  -d '{}'
```
Expected: response shows `restaurantsSkipped >= 1`, `statementsCreated == 0`.

- [ ] **Step 5: (No commit — verification only)**

---

### Task 35: Final formatting and full test run

- [ ] **Step 1: Format**

```bash
make format-fix
```

- [ ] **Step 2: Run full test suite**

```bash
make test
```
Expected: all tests pass (ignoring the documented `AppEnvironmentTests.Load_AppliesDefaults_WhenOptionalVarsAreMissing` Docker-only failure).

- [ ] **Step 3: Stage any formatting-only diffs**

```bash
git diff --stat
```

- [ ] **Step 4: Commit (only if there are formatting changes)**

```bash
git add -A
git commit -m "$(cat <<'EOF'
style: apply formatting fixes

PBI: AB#5994
Task: AB#6012
EOF
)"
```

---

## End of plan

**Summary of commits the engineer will produce** (in order):

1. `feat(shared): add commission statement enums`
2. `feat(server): add commission statement messaging routing key`
3. `feat(shared): add admin commission statement run route`
4. `feat(server): add commission statement error messages`
5. `feat(server): add commission statement data model`
6. `feat(server): add commission invoicing cutover constant`
7. `feat(server): register commission statement DbSets`
8. `feat(server): add EF configuration for commission statements`
9. `feat(db): add migration for commission statements`
10. `feat(shared): add commission statement DTOs`
11. `feat(server): add commission statement mapper`
12. `feat(server): add commission statement repository interface`
13. `feat(server): add commission statement repository`
14. `test(server): add commission statement repository tests`
15. `feat(server): set Order.DeliveredAt on Delivered transition`
16. `feat(server): add commission statement service interface`
17. `test(server): add failing test for commission statement generation`
18. `feat(server): implement commission statement generation service`
19. `test(server): cover edge cases in commission statement generation`
20. `test(server): add failing tests for prior-period refund handling`
21. `feat(server): implement prior-period refund credit-note flow`
22. `test(server): add failing tests for commission invoice cutover gate`
23. `feat(server): gate per-order commission invoice creation behind cutover`
24. `test(server): add failing test wiring refund webhook to commission handler`
25. `feat(server): route refund webhook to commission statement handler`
26. `test(server): add failing tests for admin commission statement controller`
27. `feat(server): add admin commission statement run controller`
28. `build(scheduler): add Quartz.NET and ensure Europe/Paris tzdata`
29. `test(scheduler): add failing tests for monthly commission job`
30. `feat(scheduler): implement monthly commission statement Quartz job`
31. `feat(scheduler): wire Quartz cron for monthly commission statements`
32. `feat(worker): add commission statement PDF renderer`
33. `feat(worker): add commission statement job consumer and email templates`
34. (verification — no commit)
35. `style: apply formatting fixes` (if needed)
