using DeliverTableServer.Common;
using DeliverTableServer.Features.Admin;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminRatingControllerTests
{
    private IAdminRatingService _adminRatingService = null!;
    private AdminRatingController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _adminRatingService = Substitute.For<IAdminRatingService>();
        _sut = new AdminRatingController(_adminRatingService);
    }

    #region GetRestaurantRatings

    [Test]
    public async Task GetRestaurantRatings_ReturnsOk()
    {
        List<AdminRestaurantRatingResponse> ratings = new List<AdminRestaurantRatingResponse>
        {
            new() { Id = 1, Rating = 5, Comment = "Excellent" },
            new() { Id = 2, Rating = 3, Comment = "Moyen" }
        };
        _adminRatingService.GetRestaurantRatingsAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminRestaurantRatingResponse>>.Success(ratings));

        IActionResult result = await _sut.GetRestaurantRatings(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetRestaurantRatings_WhenError_ReturnsError()
    {
        _adminRatingService.GetRestaurantRatingsAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminRestaurantRatingResponse>>.Failure(new ServiceError("Erreur", 500)));

        IActionResult result = await _sut.GetRestaurantRatings(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        ObjectResult obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region Delete

    [Test]
    public async Task Delete_WhenSuccess_ReturnsNoContent()
    {
        _adminRatingService.DeleteAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        IActionResult result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_WhenNotFound_Returns404()
    {
        _adminRatingService.DeleteAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure(new ServiceError("Avis introuvable", 404)));

        IActionResult result = await _sut.Delete(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        ObjectResult obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion
}
