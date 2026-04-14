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
    private AppEnvironment _env = null!;
    private InvoiceService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _invoiceRepo = Substitute.For<IInvoiceRepository>();
        _orderRepo = Substitute.For<IOrderRepository>();
        _numbering = Substitute.For<IInvoiceNumberingService>();
        _env = TestEnvironmentFactory.Create();
        _sut = new InvoiceService(_invoiceRepo, _orderRepo, _numbering, _env);
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
}
