using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Server.Fixtures;

namespace DeliverTableTests.Infrastructure.Unit.Repositories;

[TestFixture]
public class OrderRepositoryTests
{
    private TestDatabase _testDb = null!;
    private OrderRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _testDb = new TestDatabase();
        _sut = new OrderRepository(_testDb.Context);
    }

    [TearDown]
    public void TearDown()
    {
        _testDb.Dispose();
    }

    [Test]
    public async Task GetByIdWithFullDetailsAsync_LoadsDiscountsAndItemDish()
    {
        User customer = new User { UserName = "c@example.fr", Email = "c@example.fr", FirstName = "Jean", LastName = "Dupont" };
        User owner = new User { UserName = "o@example.fr", Email = "o@example.fr", FirstName = "Owner", LastName = "X" };
        _testDb.Context.Users.AddRange(customer, owner);
        await _testDb.Context.SaveChangesAsync();

        Restaurant restaurant = new Restaurant
        {
            OwnerId = owner.Id,
            Name = "Resto",
            LegalName = "Resto SAS",
            LegalAddress = "1 rue",
            LegalForm = "SAS",
            Siret = "73282932000074",
            IsVatRegistered = true,
        };
        _testDb.Context.Restaurants.Add(restaurant);
        await _testDb.Context.SaveChangesAsync();

        Dish dish = new Dish { RestaurantId = restaurant.Id, Name = "Plat", BasePrice = 10m, VatRate = VatRate.Intermediate10 };
        _testDb.Context.Dishes.Add(dish);
        await _testDb.Context.SaveChangesAsync();

        Order order = new Order
        {
            CustomerId = customer.Id,
            RestaurantId = restaurant.Id,
            OrderType = OrderType.Delivery,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            OriginalAmount = 20m,
            DiscountAmount = 2m,
            TotalAmount = 18m,
            Source = BookingSource.CustomerApp,
            Items = new List<OrderItem>
            {
                new() { DishId = dish.Id, DishName = dish.Name, Quantity = 2, UnitPrice = 10m },
            },
            Discounts = new List<OrderDiscount>
            {
                new() { Source = OrderDiscountSource.Promotion, Description = "Promo X", Amount = 2m },
            },
        };
        _testDb.Context.Orders.Add(order);
        await _testDb.Context.SaveChangesAsync();

        _testDb.Context.ChangeTracker.Clear();

        Order? result = await _sut.GetByIdWithFullDetailsAsync(order.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Discounts, Has.Count.EqualTo(1));
        Assert.That(result.Discounts[0].Description, Is.EqualTo("Promo X"));
        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items[0].Dish, Is.Not.Null);
        Assert.That(result.Items[0].Dish.Id, Is.EqualTo(dish.Id));
    }
}
