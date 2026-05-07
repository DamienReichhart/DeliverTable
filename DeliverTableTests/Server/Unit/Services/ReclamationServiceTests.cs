using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableInfrastructure.Services.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Reclamation;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;
using DeliverTableTests.Global.Helpers;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class ReclamationServiceTests
{
    private IReclamationRepository _reclamationRepository = null!;
    private IObjectStorageService _objectStorageService = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private IRestaurantTransactionRepository _restaurantTransactionRepository = null!;
    private AppEnvironment _appEnvironment = null!;
    private ReclamationService _sut = null!;

    private const int ReclamationId = 1;
    private const int OrderId = 10;
    private const int CustomerId = 42;
    private const int OwnerId = 7;
    private const int RestaurantId = 3;

    [SetUp]
    public void SetUp()
    {
        _reclamationRepository = Substitute.For<IReclamationRepository>();
        _objectStorageService = Substitute.For<IObjectStorageService>();
        _appEnvironment = AppEnvironmentTestHelper.SetupEnvironment();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _restaurantTransactionRepository = Substitute.For<IRestaurantTransactionRepository>();
        _sut = new ReclamationService(_reclamationRepository, _restaurantRepository, _restaurantTransactionRepository, _objectStorageService, _appEnvironment);
    }

    [TearDown]
    public void TearDown() => AppEnvironmentTestHelper.CleanupEnvironment();

    // ── helpers ────────────────────────────────────────────────────────────────

    private static Reclamation BuildReclamation(ReclamationStatus status, int orderId = OrderId) => new()
    {
        ReclamationId = ReclamationId,
        OrderId = orderId,
        Status = status,
        Description = "Test",
        Type = ReclamationType.Other,
        Order = new Order
        {
            Id = orderId,
            CustomerId = CustomerId,
            RestaurantId = RestaurantId,
            Restaurant = new Restaurant { Id = RestaurantId, OwnerId = OwnerId }
        },
        Items = []
    };

    // ── ResolveReclamation ──────────────────────────────────────────────────────

    [Test]
    public async Task ResolveReclamation_WhenPendingAndOwnerMatches_ReturnsUpdatedDto()
    {
        var reclamation = BuildReclamation(ReclamationStatus.Pending);
        _reclamationRepository.GetReclamationById(ReclamationId).Returns(reclamation);
        _reclamationRepository.UpdateReclamationStatus(ReclamationId, ReclamationStatus.Resolved)
            .Returns(BuildReclamation(ReclamationStatus.Resolved));

        ServiceResult<ReclamationDto> result = await _sut.ResolveReclamation(ReclamationId, OwnerId);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Status, Is.EqualTo(ReclamationStatus.Resolved));
    }

    [Test]
    public async Task ResolveReclamation_WhenReclamationNotFound_Returns404()
    {
        _reclamationRepository.GetReclamationById(ReclamationId).Returns((Reclamation?)null);

        ServiceResult<ReclamationDto> result = await _sut.ResolveReclamation(ReclamationId, OwnerId);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task ResolveReclamation_WhenStatusIsNotPending_Returns409()
    {
        var reclamation = BuildReclamation(ReclamationStatus.Resolved);
        _reclamationRepository.GetReclamationById(ReclamationId).Returns(reclamation);

        ServiceResult<ReclamationDto> result = await _sut.ResolveReclamation(ReclamationId, OwnerId);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(409));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.ReclamationInvalidTransition));
    }

    [Test]
    public async Task ResolveReclamation_WhenOwnerDoesNotOwnRestaurant_Returns403()
    {
        var reclamation = BuildReclamation(ReclamationStatus.Pending);
        _reclamationRepository.GetReclamationById(ReclamationId).Returns(reclamation);

        ServiceResult<ReclamationDto> result = await _sut.ResolveReclamation(ReclamationId, ownerId: 999);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(403));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.ReclamationAccessDenied));
    }

    // ── ContestReclamation ──────────────────────────────────────────────────────

    [Test]
    public async Task ContestReclamation_WhenResolvedAndCustomerMatches_ReturnsUpdatedDto()
    {
        var reclamation = BuildReclamation(ReclamationStatus.Resolved);
        _reclamationRepository.GetReclamationById(ReclamationId).Returns(reclamation);
        _reclamationRepository.UpdateReclamationStatus(ReclamationId, ReclamationStatus.Contested)
            .Returns(BuildReclamation(ReclamationStatus.Contested));

        ServiceResult<ReclamationDto> result = await _sut.ContestReclamation(ReclamationId, CustomerId);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Status, Is.EqualTo(ReclamationStatus.Contested));
    }

    [Test]
    public async Task ContestReclamation_WhenReclamationNotFound_Returns404()
    {
        _reclamationRepository.GetReclamationById(ReclamationId).Returns((Reclamation?)null);

        ServiceResult<ReclamationDto> result = await _sut.ContestReclamation(ReclamationId, CustomerId);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task ContestReclamation_WhenStatusIsNotResolved_Returns409()
    {
        var reclamation = BuildReclamation(ReclamationStatus.Pending);
        _reclamationRepository.GetReclamationById(ReclamationId).Returns(reclamation);

        ServiceResult<ReclamationDto> result = await _sut.ContestReclamation(ReclamationId, CustomerId);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(409));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.ReclamationInvalidTransition));
    }

    [Test]
    public async Task ContestReclamation_WhenCustomerIsNotOrderOwner_Returns403()
    {
        var reclamation = BuildReclamation(ReclamationStatus.Resolved);
        _reclamationRepository.GetReclamationById(ReclamationId).Returns(reclamation);

        ServiceResult<ReclamationDto> result = await _sut.ContestReclamation(ReclamationId, customerId: 999);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(403));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.ReclamationAccessDenied));
    }

    // ── CompleteReclamation ─────────────────────────────────────────────────────

    [Test]
    public async Task CompleteReclamation_WhenContested_ReturnsUpdatedDto()
    {
        var reclamation = BuildReclamation(ReclamationStatus.Contested);
        _reclamationRepository.GetReclamationById(ReclamationId).Returns(reclamation);
        _reclamationRepository.UpdateReclamationStatus(ReclamationId, ReclamationStatus.Completed)
            .Returns(BuildReclamation(ReclamationStatus.Completed));

        ServiceResult<ReclamationDto> result = await _sut.CompleteReclamation(ReclamationId);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Status, Is.EqualTo(ReclamationStatus.Completed));
    }

    [Test]
    public async Task CompleteReclamation_WhenReclamationNotFound_Returns404()
    {
        _reclamationRepository.GetReclamationById(ReclamationId).Returns((Reclamation?)null);

        ServiceResult<ReclamationDto> result = await _sut.CompleteReclamation(ReclamationId);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task CompleteReclamation_WhenStatusIsNotContested_Returns409()
    {
        var reclamation = BuildReclamation(ReclamationStatus.Resolved);
        _reclamationRepository.GetReclamationById(ReclamationId).Returns(reclamation);

        ServiceResult<ReclamationDto> result = await _sut.CompleteReclamation(ReclamationId);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(409));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.ReclamationInvalidTransition));
    }

    // ── GetReclamationsByRestaurantOwner ────────────────────────────────────────

    [Test]
    public async Task GetReclamationsByRestaurantOwner_ReturnsMappedDtos()
    {
        var reclamations = new List<Reclamation>
        {
            BuildReclamation(ReclamationStatus.Pending),
            BuildReclamation(ReclamationStatus.Resolved)
        };
        _reclamationRepository.GetReclamationsByRestaurantOwner(OwnerId).Returns(reclamations);

        ServiceResult<List<ReclamationDto>> result = await _sut.GetReclamationsByRestaurantOwner(OwnerId);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetReclamationsByRestaurantOwner_WhenNoReclamations_ReturnsEmptyList()
    {
        _reclamationRepository.GetReclamationsByRestaurantOwner(OwnerId).Returns([]);

        ServiceResult<List<ReclamationDto>> result = await _sut.GetReclamationsByRestaurantOwner(OwnerId);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty);
    }
}
