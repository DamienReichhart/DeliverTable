using DeliverTableInfrastructure.Invoicing;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableInfrastructure.Services.Interfaces;
using DeliverTableServer.Configuration;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Factories;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class InvoiceServiceTests
{
    private IInvoiceRepository _invoiceRepo = null!;
    private IOrderRepository _orderRepo = null!;
    private IInvoiceNumberingService _numbering = null!;
    private IPaymentRepository _paymentRepo = null!;
    private IRestaurantRepository _restaurantRepo = null!;
    private IObjectStorageService _objectStorage = null!;
    private IEmailJobRepository _emailJobRepo = null!;
    private IMessagePublisher _publisher = null!;
    private AppEnvironment _env = null!;
    private InvoiceService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _invoiceRepo = Substitute.For<IInvoiceRepository>();
        _orderRepo = Substitute.For<IOrderRepository>();
        _numbering = Substitute.For<IInvoiceNumberingService>();
        _paymentRepo = Substitute.For<IPaymentRepository>();
        _restaurantRepo = Substitute.For<IRestaurantRepository>();
        _objectStorage = Substitute.For<IObjectStorageService>();
        _emailJobRepo = Substitute.For<IEmailJobRepository>();
        _publisher = Substitute.For<IMessagePublisher>();
        _env = TestEnvironmentFactory.Create();
        _sut = new InvoiceService(
            _invoiceRepo,
            _orderRepo,
            _numbering,
            _paymentRepo,
            _restaurantRepo,
            _objectStorage,
            _emailJobRepo,
            _publisher,
            _env);
    }

    [Test]
    public async Task CreatePendingInvoices_HappyPath_CreatesTwoAndReturnsMessages()
    {
        var restaurant = new Restaurant
        {
            Id = 5,
            Name = "Test",
            Siret = "73282932000074",
            LegalName = "Test SAS",
            LegalAddress = "1 rue",
            LegalForm = "SAS",
            IsVatRegistered = true,
        };
        var customer = new User { Id = 1, Email = "cust@example.fr", FirstName = "Jean", LastName = "Dupont" };
        var dish = new Dish { Id = 10, VatRate = VatRate.Intermediate10 };
        var order = new Order
        {
            Id = 42,
            CustomerId = 1,
            RestaurantId = 5,
            TotalAmount = 20m,
            Status = OrderStatus.Pending,
            Customer = customer,
            Restaurant = restaurant,
            Items = new List<OrderItem>
            {
                new() { DishId = 10, Dish = dish, DishName = "Plat 1", Quantity = 2, UnitPrice = 10m },
            },
        };

        _orderRepo.GetByIdWithFullDetailsAsync(42, Arg.Any<CancellationToken>()).Returns(order);
        _invoiceRepo
            .ExistsForOrderAndKindAsync(42, InvoiceKind.OrderInvoiceToCustomer, Arg.Any<CancellationToken>())
            .Returns(false);
        _numbering
            .IssueNumberAsync(InvoiceIssuerType.Restaurant, 5, Arg.Any<int>(), false, Arg.Any<CancellationToken>())
            .Returns("R0005-2026-000001");
        _numbering
            .IssueNumberAsync(InvoiceIssuerType.Platform, null, Arg.Any<int>(), false, Arg.Any<CancellationToken>())
            .Returns("DT-2026-000123");

        int nextId = 100;
        _invoiceRepo
            .CreateAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var inv = ci.Arg<Invoice>();
                inv.Id = nextId++;
                return inv;
            });

        var result = await _sut.CreatePendingInvoicesForCapturedOrderAsync(42, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
        await _invoiceRepo.Received(2).CreateAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePendingInvoices_AlreadyExists_SkipsAndReturnsEmpty()
    {
        _orderRepo
            .GetByIdWithFullDetailsAsync(42, Arg.Any<CancellationToken>())
            .Returns(new Order { Id = 42 });
        _invoiceRepo
            .ExistsForOrderAndKindAsync(42, InvoiceKind.OrderInvoiceToCustomer, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.CreatePendingInvoicesForCapturedOrderAsync(42, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(0));
        await _invoiceRepo.DidNotReceive().CreateAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePendingInvoices_VatExemptRestaurant_CustomerLinesAllZeroVat()
    {
        var restaurant = new Restaurant
        {
            Id = 5,
            Siret = "73282932000074",
            LegalName = "X",
            LegalAddress = "Y",
            LegalForm = "EI",
            IsVatRegistered = false,
        };
        var dish = new Dish { Id = 10, VatRate = VatRate.Intermediate10 };
        var order = new Order
        {
            Id = 42,
            CustomerId = 1,
            RestaurantId = 5,
            TotalAmount = 10m,
            Status = OrderStatus.Pending,
            Customer = new User { Id = 1, Email = "x@y.fr" },
            Restaurant = restaurant,
            Items = new List<OrderItem>
            {
                new() { DishId = 10, Dish = dish, DishName = "Plat", Quantity = 1, UnitPrice = 10m },
            },
        };

        _orderRepo.GetByIdWithFullDetailsAsync(42, Arg.Any<CancellationToken>()).Returns(order);
        _invoiceRepo
            .ExistsForOrderAndKindAsync(42, InvoiceKind.OrderInvoiceToCustomer, Arg.Any<CancellationToken>())
            .Returns(false);
        _numbering
            .IssueNumberAsync(
                Arg.Any<InvoiceIssuerType>(),
                Arg.Any<int?>(),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns("TEST-000001");

        Invoice? customerInvoice = null;
        _invoiceRepo
            .CreateAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var inv = ci.Arg<Invoice>();
                if (inv.Kind == InvoiceKind.OrderInvoiceToCustomer)
                    customerInvoice = inv;
                inv.Id = 1;
                return inv;
            });

        await _sut.CreatePendingInvoicesForCapturedOrderAsync(42, CancellationToken.None);

        Assert.That(customerInvoice, Is.Not.Null);
        Assert.That(customerInvoice!.Lines, Has.All.Matches<InvoiceLine>(l => l.VatRate == 0m));
        Assert.That(customerInvoice.TotalVat, Is.EqualTo(0m));
    }

    [Test]
    public async Task CreateCreditNotes_FullRefund_MirrorsOriginalLinesNegatively()
    {
        var refund = new Refund { Id = 7, PaymentId = 1, Amount = 20m };
        var payment = new Payment { Id = 1, OrderId = 42, Amount = 20m };
        var originalCustomer = new Invoice
        {
            Id = 100,
            OrderId = 42,
            Kind = InvoiceKind.OrderInvoiceToCustomer,
            Number = "R0005-2026-000001",
            TotalTtc = 20m,
            TotalHt = 18.18m,
            TotalVat = 1.82m,
            IssuerType = InvoiceIssuerType.Restaurant,
            IssuerRestaurantId = 5,
            RecipientUserId = 1,
            IssuerLegalSnapshotJson = "{}",
            RecipientSnapshotJson = "{}",
            Lines = new List<InvoiceLine>
            {
                new()
                {
                    Description = "Plat",
                    Quantity = 2m,
                    UnitPriceTtc = 10m,
                    UnitPriceHt = 9.09m,
                    VatRate = 10m,
                    LineHt = 18.18m,
                    LineVat = 1.82m,
                    LineTtc = 20m,
                    SortOrder = 0,
                },
            },
        };
        var originalCommission = new Invoice
        {
            Id = 101,
            OrderId = 42,
            Kind = InvoiceKind.CommissionInvoiceToRestaurant,
            Number = "DT-2026-000001",
            TotalTtc = 2.40m,
            TotalHt = 2m,
            TotalVat = 0.40m,
            IssuerType = InvoiceIssuerType.Platform,
            RecipientRestaurantId = 5,
            IssuerLegalSnapshotJson = "{}",
            RecipientSnapshotJson = "{}",
            Lines = new List<InvoiceLine>
            {
                new()
                {
                    Description = "Commission",
                    Quantity = 1m,
                    UnitPriceHt = 2m,
                    UnitPriceTtc = 2.40m,
                    VatRate = 20m,
                    LineHt = 2m,
                    LineVat = 0.40m,
                    LineTtc = 2.40m,
                    SortOrder = 0,
                },
            },
        };

        _paymentRepo.GetRefundByIdAsync(7, Arg.Any<CancellationToken>()).Returns(refund);
        _paymentRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(payment);
        _invoiceRepo
            .ListByOrderIdAsync(42, Arg.Any<CancellationToken>())
            .Returns(new List<Invoice> { originalCustomer, originalCommission });
        _numbering
            .IssueNumberAsync(
                Arg.Any<InvoiceIssuerType>(),
                Arg.Any<int?>(),
                Arg.Any<int>(),
                true,
                Arg.Any<CancellationToken>())
            .Returns("AV-TEST-000002");
        int nextId = 200;
        _invoiceRepo
            .CreateAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var inv = ci.Arg<Invoice>();
                inv.Id = nextId++;
                return inv;
            });

        var result = await _sut.CreateCreditNotesForRefundAsync(7, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
        await _invoiceRepo.Received().CreateAsync(
            Arg.Is<Invoice>(i =>
                i.Kind == InvoiceKind.CreditNoteToCustomer
                && i.RelatedInvoiceId == 100
                && i.TotalTtc == -20m
                && i.Lines[0].Quantity == -2m),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateCreditNotes_PartialRefund_ProratesNegativeLines()
    {
        var refund = new Refund { Id = 7, PaymentId = 1, Amount = 5m }; // 25% of 20
        var payment = new Payment { Id = 1, OrderId = 42, Amount = 20m };
        var originalCustomer = new Invoice
        {
            Id = 100,
            OrderId = 42,
            Kind = InvoiceKind.OrderInvoiceToCustomer,
            Number = "R0005-2026-000001",
            TotalTtc = 20m,
            TotalHt = 18.18m,
            TotalVat = 1.82m,
            IssuerType = InvoiceIssuerType.Restaurant,
            IssuerRestaurantId = 5,
            RecipientUserId = 1,
            IssuerLegalSnapshotJson = "{}",
            RecipientSnapshotJson = "{}",
            Lines = new List<InvoiceLine>
            {
                new()
                {
                    Description = "Plat",
                    Quantity = 2m,
                    UnitPriceTtc = 10m,
                    UnitPriceHt = 9.09m,
                    VatRate = 10m,
                    LineHt = 18.18m,
                    LineVat = 1.82m,
                    LineTtc = 20m,
                    SortOrder = 0,
                },
            },
        };

        _paymentRepo.GetRefundByIdAsync(7, Arg.Any<CancellationToken>()).Returns(refund);
        _paymentRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(payment);
        _invoiceRepo
            .ListByOrderIdAsync(42, Arg.Any<CancellationToken>())
            .Returns(new List<Invoice> { originalCustomer });
        _numbering
            .IssueNumberAsync(
                Arg.Any<InvoiceIssuerType>(),
                Arg.Any<int?>(),
                Arg.Any<int>(),
                true,
                Arg.Any<CancellationToken>())
            .Returns("AV-TEST-000002");
        Invoice? cn = null;
        _invoiceRepo
            .CreateAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                cn = ci.Arg<Invoice>();
                cn.Id = 201;
                return cn;
            });

        await _sut.CreateCreditNotesForRefundAsync(7, CancellationToken.None);

        Assert.That(cn, Is.Not.Null);
        Assert.That(cn!.Lines[0].Quantity, Is.EqualTo(-0.5m)); // 2 * -0.25
        Assert.That(cn.TotalTtc, Is.EqualTo(-5m));
    }

    [Test]
    public async Task CreateCreditNotes_CommissionOnly_FallsBackToPaymentAmount()
    {
        // Only commissionOriginal is present; no customer original.
        // The commission credit note ratio must be based on payment.Amount.
        var refund = new Refund { Id = 7, PaymentId = 1, Amount = 5m };
        var payment = new Payment { Id = 1, OrderId = 42, Amount = 20m };
        var commissionOriginal = new Invoice
        {
            Id = 101,
            OrderId = 42,
            Kind = InvoiceKind.CommissionInvoiceToRestaurant,
            Number = "DT-2026-000001",
            TotalTtc = 2.40m,
            TotalHt = 2m,
            TotalVat = 0.40m,
            IssuerType = InvoiceIssuerType.Platform,
            RecipientRestaurantId = 5,
            IssuerLegalSnapshotJson = "{}",
            RecipientSnapshotJson = "{}",
            Lines = new List<InvoiceLine>
            {
                new()
                {
                    Description = "Commission",
                    Quantity = 1m,
                    UnitPriceHt = 2m,
                    UnitPriceTtc = 2.40m,
                    VatRate = 20m,
                    LineHt = 2m,
                    LineVat = 0.40m,
                    LineTtc = 2.40m,
                    SortOrder = 0,
                },
            },
        };

        _paymentRepo.GetRefundByIdAsync(7, Arg.Any<CancellationToken>()).Returns(refund);
        _paymentRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(payment);
        // No customer original — only the commission invoice is returned.
        _invoiceRepo
            .ListByOrderIdAsync(42, Arg.Any<CancellationToken>())
            .Returns(new List<Invoice> { commissionOriginal });
        _numbering
            .IssueNumberAsync(
                Arg.Any<InvoiceIssuerType>(),
                Arg.Any<int?>(),
                Arg.Any<int>(),
                true,
                Arg.Any<CancellationToken>())
            .Returns("AV-TEST-000003");

        Invoice? creditNote = null;
        _invoiceRepo
            .CreateAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                creditNote = ci.Arg<Invoice>();
                creditNote.Id = 202;
                return creditNote;
            });

        var result = await _sut.CreateCreditNotesForRefundAsync(7, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(creditNote, Is.Not.Null);

        // Expected ratio: refund.Amount / payment.Amount = 5 / 20 = 0.25
        // Line qty: 1 * -0.25 = -0.25
        Assert.That(creditNote!.Lines[0].Quantity, Is.EqualTo(-0.25m));
        Assert.That(creditNote.TotalTtc, Is.EqualTo(Math.Round(-2.40m * 0.25m, 2, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public async Task CreateCreditNotes_RefundNotFound_ReturnsEmpty()
    {
        _paymentRepo.GetRefundByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Refund?)null);

        var result = await _sut.CreateCreditNotesForRefundAsync(99, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(0));
    }

    // ─── ListForMeAsync ───────────────────────────────────────────────────

    [Test]
    public async Task ListForMeAsync_ReturnsRecipientUserInvoices()
    {
        var invoices = new List<Invoice>
        {
            new()
            {
                Id = 1,
                Number = "R0005-2026-000001",
                Kind = InvoiceKind.OrderInvoiceToCustomer,
                OrderId = 42,
                IssuedAt = DateTime.UtcNow,
                TotalTtc = 20m,
                Currency = "EUR",
                Status = InvoiceStatus.Generated,
            },
        };
        _invoiceRepo
            .ListForRecipientUserAsync(7, 1, 20, Arg.Any<CancellationToken>())
            .Returns((invoices, 1));

        var result = await _sut.ListForMeAsync(7, 1, 20, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Items, Has.Count.EqualTo(1));
        Assert.That(result.Value.TotalCount, Is.EqualTo(1));
        Assert.That(result.Value.Items[0].Number, Is.EqualTo("R0005-2026-000001"));
    }

    // ─── ListForRestaurantAsync ───────────────────────────────────────────

    [Test]
    public async Task ListForRestaurantAsync_NonOwnerNonAdmin_ReturnsAccessDenied()
    {
        var restaurant = new Restaurant { Id = 5, OwnerId = 99 };
        _restaurantRepo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(restaurant);

        var result = await _sut.ListForRestaurantAsync(5, 7, false, 1, 20, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(403));
    }

    [Test]
    public async Task ListForRestaurantAsync_OwnerMatches_ReturnsList()
    {
        var restaurant = new Restaurant { Id = 5, OwnerId = 7 };
        _restaurantRepo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(restaurant);

        var invoices = new List<Invoice>
        {
            new()
            {
                Id = 10,
                Number = "DT-2026-000001",
                Kind = InvoiceKind.CommissionInvoiceToRestaurant,
                OrderId = 42,
                IssuedAt = DateTime.UtcNow,
                TotalTtc = 2.40m,
                Currency = "EUR",
                Status = InvoiceStatus.Generated,
            },
        };
        _invoiceRepo
            .ListForRecipientRestaurantAsync(5, 1, 20, Arg.Any<CancellationToken>())
            .Returns((invoices, 1));

        var result = await _sut.ListForRestaurantAsync(5, 7, false, 1, 20, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Items, Has.Count.EqualTo(1));
        Assert.That(result.Value.Items[0].Number, Is.EqualTo("DT-2026-000001"));
    }

    [Test]
    public async Task ListForRestaurantAsync_Admin_BypassesOwnerCheck()
    {
        var invoices = new List<Invoice>
        {
            new()
            {
                Id = 10,
                Number = "DT-2026-000001",
                Kind = InvoiceKind.CommissionInvoiceToRestaurant,
                OrderId = 42,
                IssuedAt = DateTime.UtcNow,
                TotalTtc = 2.40m,
                Currency = "EUR",
                Status = InvoiceStatus.Generated,
            },
        };
        _invoiceRepo
            .ListForRecipientRestaurantAsync(5, 1, 20, Arg.Any<CancellationToken>())
            .Returns((invoices, 1));

        var result = await _sut.ListForRestaurantAsync(5, 999, true, 1, 20, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _restaurantRepo.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // ─── GetPdfStreamAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetPdfStreamAsync_NotFound_ReturnsNotFound()
    {
        _invoiceRepo.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Invoice?)null);

        var result = await _sut.GetPdfStreamAsync(99, 7, false, false, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task GetPdfStreamAsync_NotGenerated_Returns409Style()
    {
        var invoice = new Invoice
        {
            Id = 1,
            Number = "R0005-2026-000001",
            RecipientUserId = 7,
            Status = InvoiceStatus.Queued,
            StoragePath = null,
        };
        _invoiceRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.GetPdfStreamAsync(1, 7, false, false, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(409));
    }

    [Test]
    public async Task GetPdfStreamAsync_WrongUser_ReturnsAccessDenied()
    {
        var invoice = new Invoice
        {
            Id = 1,
            Number = "R0005-2026-000001",
            RecipientUserId = 99,
            RecipientRestaurantId = null,
            Status = InvoiceStatus.Generated,
            StoragePath = "invoices/1.pdf",
        };
        _invoiceRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.GetPdfStreamAsync(1, 7, false, false, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(403));
    }

    [Test]
    public async Task GetPdfStreamAsync_Authorized_ReturnsStream()
    {
        var invoice = new Invoice
        {
            Id = 1,
            Number = "R0005-2026-000001",
            RecipientUserId = 7,
            Status = InvoiceStatus.Generated,
            StoragePath = "invoices/1.pdf",
        };
        _invoiceRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(invoice);

        var stream = new MemoryStream(new byte[] { 0x25, 0x50 });
        _objectStorage
            .GetObjectAsync("invoices/1.pdf", Arg.Any<CancellationToken>())
            .Returns(new ObjectStorageResult(stream, "application/pdf", 2));

        var result = await _sut.GetPdfStreamAsync(1, 7, false, false, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.ContentType, Is.EqualTo("application/pdf"));
        Assert.That(result.Value.FileName, Is.EqualTo("R0005-2026-000001.pdf"));
    }

    [Test]
    public async Task GetPdfStreamAsync_Admin_BypassesUserCheck()
    {
        var invoice = new Invoice
        {
            Id = 1,
            Number = "DT-2026-000001",
            RecipientUserId = 99,
            RecipientRestaurantId = 5,
            Status = InvoiceStatus.Generated,
            StoragePath = "invoices/1.pdf",
        };
        _invoiceRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(invoice);

        var stream = new MemoryStream(new byte[] { 0x25, 0x50 });
        _objectStorage
            .GetObjectAsync("invoices/1.pdf", Arg.Any<CancellationToken>())
            .Returns(new ObjectStorageResult(stream, "application/pdf", 2));

        var result = await _sut.GetPdfStreamAsync(1, 42, true, false, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task GetPdfStreamAsync_RestaurantOwner_WithRecipientRestaurant_ChecksOwnership()
    {
        var invoice = new Invoice
        {
            Id = 1,
            Number = "DT-2026-000001",
            RecipientUserId = null,
            RecipientRestaurantId = 5,
            Status = InvoiceStatus.Generated,
            StoragePath = "invoices/1.pdf",
        };
        _invoiceRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(invoice);
        _restaurantRepo.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(new Restaurant { Id = 5, OwnerId = 7 });

        var stream = new MemoryStream(new byte[] { 0x25, 0x50 });
        _objectStorage
            .GetObjectAsync("invoices/1.pdf", Arg.Any<CancellationToken>())
            .Returns(new ObjectStorageResult(stream, "application/pdf", 2));

        var result = await _sut.GetPdfStreamAsync(1, 7, false, true, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
    }

    // ─── AdminListAsync ───────────────────────────────────────────────────

    [Test]
    public async Task AdminListAsync_FiltersByYear_ReturnsMatchingRows()
    {
        var issuerSnapshot = new InvoiceLegalSnapshotDto("Restaurant SAS", "SAS", "12345", "", "1 rue");
        var recipientSnapshot = new InvoiceLegalSnapshotDto("Jean Dupont", "", "", "", "jean@example.fr");
        var invoices = new List<Invoice>
        {
            new()
            {
                Id = 1,
                Number = "R0005-2026-000001",
                Kind = InvoiceKind.OrderInvoiceToCustomer,
                IssuerType = InvoiceIssuerType.Restaurant,
                IssuedAt = new DateTime(2026, 1, 15),
                TotalTtc = 20m,
                Status = InvoiceStatus.Generated,
                IssuerLegalSnapshotJson = System.Text.Json.JsonSerializer.Serialize(issuerSnapshot),
                RecipientSnapshotJson = System.Text.Json.JsonSerializer.Serialize(recipientSnapshot),
            },
        };

        _invoiceRepo
            .AdminListAsync(2026, null, null, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((invoices, 1));

        var query = new InvoiceAdminQuery { Year = 2026, Page = 1, PageSize = 20 };
        var result = await _sut.AdminListAsync(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Items, Has.Count.EqualTo(1));
        Assert.That(result.Value.Items[0].IssuerName, Is.EqualTo("Restaurant SAS"));
        Assert.That(result.Value.Items[0].RecipientName, Is.EqualTo("Jean Dupont"));
        Assert.That(result.Value.TotalCount, Is.EqualTo(1));
    }

    // ─── AdminGetDetailAsync ──────────────────────────────────────────────

    [Test]
    public async Task AdminGetDetailAsync_Existing_ReturnsDetailWithSnapshots()
    {
        var issuerSnapshot = new InvoiceLegalSnapshotDto("Platform", "SAS", "99999", "FR12345", "10 rue");
        var recipientSnapshot = new InvoiceLegalSnapshotDto("Restaurant XYZ", "SARL", "11111", "", "2 avenue");
        var invoice = new Invoice
        {
            Id = 10,
            Number = "DT-2026-000001",
            Kind = InvoiceKind.CommissionInvoiceToRestaurant,
            IssuerType = InvoiceIssuerType.Platform,
            OrderId = 42,
            IssuedAt = new DateTime(2026, 3, 1),
            TotalTtc = 2.40m,
            Currency = "EUR",
            Status = InvoiceStatus.Generated,
            RelatedInvoiceId = null,
            IssuerLegalSnapshotJson = System.Text.Json.JsonSerializer.Serialize(issuerSnapshot),
            RecipientSnapshotJson = System.Text.Json.JsonSerializer.Serialize(recipientSnapshot),
            Lines =
            [
                new InvoiceLine
                {
                    Description = "Commission",
                    Quantity = 1m,
                    UnitPriceHt = 2m,
                    UnitPriceTtc = 2.40m,
                    VatRate = 20m,
                    LineHt = 2m,
                    LineVat = 0.40m,
                    LineTtc = 2.40m,
                    SortOrder = 0,
                },
            ],
        };

        _invoiceRepo
            .GetByIdWithLinesAsync(10, Arg.Any<CancellationToken>())
            .Returns(invoice);

        var result = await _sut.AdminGetDetailAsync(10, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Header.Id, Is.EqualTo(10));
        Assert.That(result.Value.Issuer.Name, Is.EqualTo("Platform"));
        Assert.That(result.Value.Recipient.Name, Is.EqualTo("Restaurant XYZ"));
        Assert.That(result.Value.Lines, Has.Count.EqualTo(1));
        Assert.That(result.Value.RelatedInvoiceId, Is.Null);
    }

    [Test]
    public async Task AdminGetDetailAsync_NotFound_Returns404()
    {
        _invoiceRepo
            .GetByIdWithLinesAsync(99, Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);

        var result = await _sut.AdminGetDetailAsync(99, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    // ─── AdminResendEmailAsync ────────────────────────────────────────────

    [Test]
    public async Task AdminResendEmailAsync_NotGenerated_ReturnsError()
    {
        var invoice = new Invoice
        {
            Id = 1,
            Number = "R0005-2026-000001",
            Status = InvoiceStatus.Queued,
            StoragePath = null,
            Kind = InvoiceKind.OrderInvoiceToCustomer,
            IssuerLegalSnapshotJson = "{}",
            RecipientSnapshotJson = "{}",
        };
        _invoiceRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.AdminResendEmailAsync(1, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(409));
    }

    [Test]
    public async Task AdminResendEmailAsync_Generated_RepublishesJob()
    {
        var recipientSnapshot = new InvoiceLegalSnapshotDto("Jean Dupont", "", "", "", "jean@example.fr");
        var invoice = new Invoice
        {
            Id = 1,
            Number = "R0005-2026-000001",
            Kind = InvoiceKind.OrderInvoiceToCustomer,
            IssuerType = InvoiceIssuerType.Restaurant,
            Status = InvoiceStatus.Generated,
            StoragePath = "invoices/2026/01/R0005-2026-000001.pdf",
            IssuedAt = new DateTime(2026, 1, 15),
            OrderId = 42,
            TotalTtc = 20m,
            Currency = "EUR",
            IssuerLegalSnapshotJson = "{}",
            RecipientSnapshotJson = System.Text.Json.JsonSerializer.Serialize(recipientSnapshot),
        };
        _invoiceRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.AdminResendEmailAsync(1, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _emailJobRepo.Received(1).CreateAsync(Arg.Any<EmailJob>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync("email", Arg.Any<EmailJobMessage>(), Arg.Any<CancellationToken>());
    }
}
