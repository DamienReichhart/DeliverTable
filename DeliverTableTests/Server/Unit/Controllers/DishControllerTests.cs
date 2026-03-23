using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dish;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class DishControllerTests
{
    private IDishService _dishService = null!;
    private DishController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dishService = Substitute.For<IDishService>();
        _sut = new DishController(_dishService);
    }

    [Test]
    public async Task GetAllDishes_ReturnsOkWithPaginatedResult()
    {
        var query = new DishQuery();
        var paginated = new PaginatedResult<DishDto>
        {
            Items = [
                new DishDto { Id = 1, Name = "Pizza Margherita" },
                new DishDto { Id = 2, Name = "Pasta Carbonara" }
            ],
            TotalCount = 2,
            Page = 1,
            PageSize = 2
        };
        _dishService.GetAllAsync(query, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<DishDto>>.Success(paginated));

        var result = await _sut.GetAllDishes(query, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetDishById_WhenExists_ReturnsOk()
    {
        var dto = new DishDto { Id = 1, Name = "Pizza" };
        _dishService.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DishDto>.Success(dto));

        var result = await _sut.GetDishById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetDishById_WhenNotFound_ReturnsError()
    {
        _dishService.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DishDto>.Failure(new ServiceError("Plat introuvable", 404)));

        var result = await _sut.GetDishById(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task DeleteDish_WhenSuccessful_ReturnsNoContent()
    {
        _dishService.DeleteAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.DeleteDish(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }
}
