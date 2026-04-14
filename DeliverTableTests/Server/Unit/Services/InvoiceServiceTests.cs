using DeliverTableInfrastructure.Invoicing;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Configuration;
using DeliverTableServer.Services;
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
    private AppEnvironment _env = null!;
    private InvoiceService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _invoiceRepo = Substitute.For<IInvoiceRepository>();
        _orderRepo = Substitute.For<IOrderRepository>();
        _numbering = Substitute.For<IInvoiceNumberingService>();
        _paymentRepo = Substitute.For<IPaymentRepository>();
        _env = TestEnvironmentFactory.Create();
        _sut = new InvoiceService(_invoiceRepo, _orderRepo, _numbering, _paymentRepo, _env);
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
    public async Task CreateCreditNotes_RefundNotFound_ReturnsEmpty()
    {
        _paymentRepo.GetRefundByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Refund?)null);

        var result = await _sut.CreateCreditNotesForRefundAsync(99, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(0));
    }
}
