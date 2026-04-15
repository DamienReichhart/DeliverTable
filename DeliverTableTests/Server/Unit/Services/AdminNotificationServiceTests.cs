using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class AdminNotificationServiceTests
{
    private INotificationRepository _notificationRepository = null!;
    private IUserRepository _userRepository = null!;
    private AdminNotificationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _notificationRepository = Substitute.For<INotificationRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _sut = new AdminNotificationService(_notificationRepository, _userRepository);
    }

    #region GetAllAsync

    [Test]
    public async Task GetAllAsync_ReturnsAllNotifications()
    {
        var user = CreateValidUser();
        user.Id = 1;
        var notifications = new List<Notification>
        {
            new()
            {
                Id = 1, Type = NotificationType.OrderStatus,
                Payload = "Commande confirmée", IsRead = false,
                UserId = 1, User = user
            },
            new()
            {
                Id = 2, Type = NotificationType.System,
                Payload = "Maintenance prévue", IsRead = true,
                UserId = 1, User = user
            }
        };

        _notificationRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(notifications);

        var result = await _sut.GetAllAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAllAsync_MapsFieldsCorrectly()
    {
        var user = CreateValidUser();
        user.Id = 1;
        var notifications = new List<Notification>
        {
            new()
            {
                Id = 1, Type = NotificationType.OrderStatus,
                Payload = "Commande confirmée", IsRead = false,
                UserId = 1, User = user
            }
        };

        _notificationRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(notifications);

        var result = await _sut.GetAllAsync();

        Assert.That(result.IsSuccess, Is.True);
        var dto = result.Value![0];
        Assert.That(dto.Id, Is.EqualTo(1));
        Assert.That(dto.Type, Is.EqualTo(nameof(NotificationType.OrderStatus)));
        Assert.That(dto.Payload, Is.EqualTo("Commande confirmée"));
        Assert.That(dto.IsRead, Is.False);
        Assert.That(dto.UserName, Is.EqualTo($"{user.FirstName} {user.LastName}"));
        Assert.That(dto.UserId, Is.EqualTo(1));
    }

    #endregion

    #region DeleteAsync

    [Test]
    public async Task DeleteAsync_WhenExists_ReturnsSuccess()
    {
        _notificationRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteAsync(1);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DeleteAsync_WhenNotFound_Returns404()
    {
        _notificationRepository.DeleteAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.DeleteAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.NotificationNotFound));
    }

    #endregion
}
