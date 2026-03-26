using System.Security.Claims;
using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Rating;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class RatingControllerTests
{
    private IRatingService _ratingService = null!;
    private RatingController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _ratingService = Substitute.For<IRatingService>();
        _sut = new RatingController(_ratingService);
        SetupUser(10);
    }

    private void SetupUser(int userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };
    }

    #region Create

    [Test]
    public async Task Create_WhenSuccess_ReturnsCreatedAtAction()
    {
        var dto = new RatingDto { Id = 1, OrderId = 1, Rating = 5 };
        _ratingService
            .CreateAsync(1, 10, Arg.Any<CreateRatingRequest>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RatingDto>.Success(dto));

        var result = await _sut.Create(1, new CreateRatingRequest { Rating = 5 }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
        var created = (CreatedAtActionResult)result;
        Assert.That(created.Value, Is.EqualTo(dto));
    }

    [Test]
    public async Task Create_WhenError_ReturnsError()
    {
        _ratingService
            .CreateAsync(1, 10, Arg.Any<CreateRatingRequest>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RatingDto>.Failure(new ServiceError("Erreur", 400)));

        var result = await _sut.Create(1, new CreateRatingRequest { Rating = 5 }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task Create_WhenNoUser_ReturnsUnauthorized()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };

        var result = await _sut.Create(1, new CreateRatingRequest { Rating = 5 }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    #endregion

    #region GetByOrder

    [Test]
    public async Task GetByOrder_WhenSuccess_ReturnsOk()
    {
        var dto = new RatingDto { Id = 1, OrderId = 1, Rating = 5 };
        _ratingService
            .GetByOrderAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RatingDto>.Success(dto));

        var result = await _sut.GetByOrder(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetByOrder_WhenNotFound_Returns404()
    {
        _ratingService
            .GetByOrderAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RatingDto>.Failure(new ServiceError("Avis introuvable", 404)));

        var result = await _sut.GetByOrder(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region Update

    [Test]
    public async Task Update_WhenSuccess_ReturnsOk()
    {
        var dto = new RatingDto { Id = 1, OrderId = 1, Rating = 4 };
        _ratingService
            .UpdateAsync(1, 10, Arg.Any<UpdateRatingRequest>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RatingDto>.Success(dto));

        var result = await _sut.Update(
            1,
            new UpdateRatingRequest { Rating = 4 },
            CancellationToken.None
        );

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Update_WhenNotFound_Returns404()
    {
        _ratingService
            .UpdateAsync(1, 10, Arg.Any<UpdateRatingRequest>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RatingDto>.Failure(new ServiceError("Avis introuvable", 404)));

        var result = await _sut.Update(
            1,
            new UpdateRatingRequest { Rating = 4 },
            CancellationToken.None
        );

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region Delete

    [Test]
    public async Task Delete_WhenSuccess_ReturnsNoContent()
    {
        _ratingService
            .DeleteAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_WhenNotFound_Returns404()
    {
        _ratingService
            .DeleteAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure(new ServiceError("Avis introuvable", 404)));

        var result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion
}
