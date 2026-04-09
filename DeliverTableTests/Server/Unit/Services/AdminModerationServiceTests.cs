using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class AdminModerationServiceTests
{
    private IModerationRepository _moderationRepository = null!;
    private AdminModerationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _moderationRepository = Substitute.For<IModerationRepository>();
        _sut = new AdminModerationService(_moderationRepository);
    }

    #region GetAllAsync

    [Test]
    public async Task GetAllAsync_ReturnsAllModerationActions()
    {
        var admin = CreateValidUser();
        admin.Id = 1;
        var actions = new List<ModerationAction>
        {
            new()
            {
                Id = 1,
                TargetType = ModerationTargetType.Restaurant,
                TargetId = 10,
                ActionType = ModerationActionType.Approve,
                Reason = "Conforme",
                AdminUserId = 1,
                AdminUser = admin,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                TargetType = ModerationTargetType.User,
                TargetId = 20,
                ActionType = ModerationActionType.Ban,
                Reason = "Abus",
                AdminUserId = 1,
                AdminUser = admin,
                CreatedAt = DateTime.UtcNow
            }
        };

        _moderationRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(actions);

        var result = await _sut.GetAllAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        _moderationRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ModerationAction>());

        var result = await _sut.GetAllAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty);
    }

    #endregion

    #region GetByIdAsync

    [Test]
    public async Task GetByIdAsync_WhenExists_ReturnsModerationAction()
    {
        var admin = CreateValidUser();
        admin.Id = 1;
        var action = new ModerationAction
        {
            Id = 1,
            TargetType = ModerationTargetType.Restaurant,
            TargetId = 10,
            ActionType = ModerationActionType.Approve,
            Reason = "Conforme",
            AdminUserId = 1,
            AdminUser = admin,
            CreatedAt = DateTime.UtcNow
        };

        _moderationRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(action);

        var result = await _sut.GetByIdAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(1));
        Assert.That(result.Value.TargetType, Is.EqualTo(nameof(ModerationTargetType.Restaurant)));
        Assert.That(result.Value.ActionType, Is.EqualTo(nameof(ModerationActionType.Approve)));
        Assert.That(result.Value.AdminUserName, Is.EqualTo($"{admin.FirstName} {admin.LastName}"));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotFound_ReturnsError()
    {
        _moderationRepository.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns((ModerationAction?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.ModerationActionNotFound));
    }

    #endregion

    #region CreateAsync

    [Test]
    public async Task CreateAsync_ReturnsCreatedAction()
    {
        var request = new AdminCreateModerationActionRequest
        {
            TargetType = ModerationTargetType.Restaurant,
            TargetId = 10,
            ActionType = ModerationActionType.Reject,
            Reason = "Non conforme"
        };

        _moderationRepository.CreateAsync(Arg.Any<ModerationAction>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var a = callInfo.ArgAt<ModerationAction>(0);
                a.Id = 5;
                return a;
            });

        var result = await _sut.CreateAsync(request, adminUserId: 1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(5));
        Assert.That(result.Value.TargetId, Is.EqualTo(10));
        Assert.That(result.Value.AdminUserId, Is.EqualTo(1));
    }

    [Test]
    public async Task CreateAsync_WithNullReason_DefaultsToEmptyString()
    {
        var request = new AdminCreateModerationActionRequest
        {
            TargetType = ModerationTargetType.User,
            TargetId = 20,
            ActionType = ModerationActionType.Warn,
            Reason = null
        };

        _moderationRepository.CreateAsync(Arg.Any<ModerationAction>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var a = callInfo.ArgAt<ModerationAction>(0);
                a.Id = 6;
                return a;
            });

        var result = await _sut.CreateAsync(request, adminUserId: 2);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Reason, Is.EqualTo(""));
    }

    #endregion
}
