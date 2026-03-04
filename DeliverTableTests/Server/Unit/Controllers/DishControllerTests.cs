using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeliverTableServer.Controllers;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.Dish;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class DishControllerTests
{
    private IDishRepository _dishRepository = null!;
    private DishController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dishRepository = Substitute.For<IDishRepository>();
        _sut = new DishController(_dishRepository);
    }

    [Test]
    public async Task GetAllDishes_ReturnsOkWithDishes()
    {
        // Arrange
        var query = new DishQuery();
        var dishes = new List<Dish>
        {
            new Dish { Id = 1, Name = "Pizza Margherita", BasePrice = 10, Description = "Classic", ImageKey = "img1.png" },
            new Dish { Id = 2, Name = "Pasta Carbonara", BasePrice = 12, Description = "Creamy", ImageKey = "img2.png" }
        };
        _dishRepository.GetAllDishes(query).Returns(dishes);

        // Act
        var result = await _sut.GetAllDishes(query);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result!;
        var returnedDishes = okResult.Value as IEnumerable<DishDto>;
        Assert.That(returnedDishes, Is.Not.Null);
        Assert.That(returnedDishes!.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetDishById_WhenDishExists_ReturnsOk()
    {
        // Arrange
        var dishId = 1;
        var dish = new Dish { Id = dishId, Name = "Pizza Margherita", BasePrice = 10 };
        _dishRepository.GetDishById(dishId).Returns(dish);

        // Act
        var result = await _sut.GetDishById(dishId);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        var returnedDish = okResult.Value as DishDto;
        Assert.That(returnedDish, Is.Not.Null);
        Assert.That(returnedDish!.Id, Is.EqualTo(dishId));
    }

    [Test]
    public void GetDishById_WhenDishNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dishId = 99;
        _dishRepository.GetDishById(dishId).Returns((Dish)null!);

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _sut.GetDishById(dishId));
    }

    [Test]
    public async Task GetDishesByRestaurantId_ReturnsOk()
    {
        // Arrange
        var restaurantId = 1;
        var query = new DishQuery();
        var dishes = new List<Dish>
        {
            new Dish { Id = 1, Name = "Pizza Margherita", BasePrice = 10, RestaurantId = restaurantId }
        };
        _dishRepository.GetDishesByRestaurantId(query, restaurantId).Returns(dishes);

        // Act
        var result = await _sut.GetDishesByRestaurantId(query, restaurantId);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result!;
        var returnedDishes = okResult.Value as IEnumerable<DishDto>;
        Assert.That(returnedDishes, Is.Not.Null);
        Assert.That(returnedDishes!.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateDish_ReturnsOk()
    {
        // Arrange
        var restaurantId = 1;
        var dto = new CreateDishDto { Name = "New Dish", BasePrice = 15 };
        var fileMock = Substitute.For<IFormFile>();

        var createdDish = new Dish { Id = 10, Name = "New Dish", BasePrice = 15, RestaurantId = restaurantId };
        _dishRepository.CreateDish(dto, restaurantId, fileMock).Returns(createdDish);

        // Act
        var result = await _sut.CreateDish(dto, restaurantId, fileMock);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result!;
        var returnedDish = okResult.Value as DishDto;
        Assert.That(returnedDish, Is.Not.Null);
        Assert.That(returnedDish!.Id, Is.EqualTo(10));
    }

    [Test]
    public async Task UpdateDish_ReturnsOk()
    {
        // Arrange
        var dishId = 1;
        var dto = new CreateDishDto { Name = "Updated Dish", BasePrice = 20 };
        var fileMock = Substitute.For<IFormFile>();

        var updatedDish = new Dish { Id = dishId, Name = "Updated Dish", BasePrice = 20 };
        _dishRepository.UpdateDish(dishId, dto, fileMock).Returns(updatedDish);

        // Act
        var result = await _sut.UpdateDish(dishId, dto, fileMock);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result!;
        var returnedDish = okResult.Value as DishDto;
        Assert.That(returnedDish, Is.Not.Null);
        Assert.That(returnedDish!.Name, Is.EqualTo("Updated Dish"));
    }

    [Test]
    public async Task DeleteDish_ReturnsNoContent()
    {
        // Arrange
        var dishId = 1;
        _dishRepository.DeleteDish(dishId).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeleteDish(dishId);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
        await _dishRepository.Received(1).DeleteDish(dishId);
    }
}
