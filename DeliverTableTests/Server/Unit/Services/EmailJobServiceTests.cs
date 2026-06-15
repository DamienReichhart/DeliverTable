using System.Text.Json;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class EmailJobServiceTests
{
    private IEmailJobRepository _emailJobRepository = null!;
    private IMessagePublisher _messagePublisher = null!;
    private ILogger<EmailJobService> _logger = null!;
    private EmailJobService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _emailJobRepository = Substitute.For<IEmailJobRepository>();
        _messagePublisher = Substitute.For<IMessagePublisher>();
        _logger = Substitute.For<ILogger<EmailJobService>>();
        _sut = new EmailJobService(_emailJobRepository, _messagePublisher, _logger);
    }

    #region QueueOrderConfirmationAsync

    [Test]
    public async Task QueueOrderConfirmationAsync_CreatesJobAndPublishes_ReturnsSuccess()
    {
        Order order = CreateTestOrder();

        ServiceResult result = await _sut.QueueOrderConfirmationAsync(order, "client@test.com", "Jean Dupont");

        Assert.That(result.IsSuccess, Is.True);
        await _emailJobRepository.Received(1).CreateAsync(
            Arg.Is<EmailJob>(j =>
                j.Type == EmailJobType.OrderConfirmation &&
                j.Status == EmailJobStatus.Pending &&
                j.RecipientEmail == "client@test.com"),
            Arg.Any<CancellationToken>());
        await _messagePublisher.Received(1).PublishAsync("email", Arg.Any<EmailJobMessage>());
    }

    [Test]
    public async Task QueueOrderConfirmationAsync_SetsCorrectJobProperties()
    {
        Order order = CreateTestOrder();
        EmailJob? capturedJob = null;
        await _emailJobRepository.CreateAsync(
            Arg.Do<EmailJob>(j => capturedJob = j),
            Arg.Any<CancellationToken>());

        await _sut.QueueOrderConfirmationAsync(order, "client@test.com", "Jean Dupont");

        Assert.That(capturedJob, Is.Not.Null);
        Assert.That(capturedJob!.RecipientEmail, Is.EqualTo("client@test.com"));
        Assert.That(capturedJob.RecipientName, Is.EqualTo("Jean Dupont"));
        Assert.That(capturedJob.Subject, Is.EqualTo($"Confirmation de votre commande #{order.Id}"));
        Assert.That(capturedJob.Type, Is.EqualTo(EmailJobType.OrderConfirmation));
        Assert.That(capturedJob.Status, Is.EqualTo(EmailJobStatus.Pending));
        Assert.That(capturedJob.MaxRetries, Is.EqualTo(5));

        JsonDocument templateData = JsonDocument.Parse(capturedJob.TemplateData);
        Assert.That(templateData.RootElement.GetProperty("OrderId").GetInt32(), Is.EqualTo(order.Id));
        Assert.That(templateData.RootElement.GetProperty("RestaurantName").GetString(), Is.EqualTo("Test Restaurant"));
    }

    [Test]
    public async Task QueueOrderConfirmationAsync_WhenPublishFails_StillReturnsSuccess()
    {
        Order order = CreateTestOrder();
        _messagePublisher.PublishAsync("email", Arg.Any<EmailJobMessage>())
            .ThrowsAsync(new Exception("RabbitMQ down"));

        ServiceResult result = await _sut.QueueOrderConfirmationAsync(order, "client@test.com", "Jean Dupont");

        Assert.That(result.IsSuccess, Is.True);
        await _emailJobRepository.Received(1).CreateAsync(Arg.Any<EmailJob>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region QueuePasswordResetAsync

    [Test]
    public async Task QueuePasswordResetAsync_CreatesJobAndPublishes_ReturnsSuccess()
    {
        ServiceResult result = await _sut.QueuePasswordResetAsync("user@test.com", "Jean", "https://reset.link/token");

        Assert.That(result.IsSuccess, Is.True);
        await _emailJobRepository.Received(1).CreateAsync(
            Arg.Is<EmailJob>(j =>
                j.Type == EmailJobType.PasswordReset &&
                j.Status == EmailJobStatus.Pending &&
                j.RecipientEmail == "user@test.com" &&
                j.Subject == "Réinitialisation de votre mot de passe"),
            Arg.Any<CancellationToken>());
        await _messagePublisher.Received(1).PublishAsync("email", Arg.Any<EmailJobMessage>());
    }

    #endregion

    #region QueueWelcomeEmailAsync

    [Test]
    public async Task QueueWelcomeEmailAsync_CreatesJobAndPublishes_ReturnsSuccess()
    {
        ServiceResult result = await _sut.QueueWelcomeEmailAsync("new@test.com", "Marie");

        Assert.That(result.IsSuccess, Is.True);
        await _emailJobRepository.Received(1).CreateAsync(
            Arg.Is<EmailJob>(j =>
                j.Type == EmailJobType.WelcomeEmail &&
                j.Status == EmailJobStatus.Pending &&
                j.RecipientEmail == "new@test.com" &&
                j.Subject == "Bienvenue sur DeliverTable"),
            Arg.Any<CancellationToken>());
        await _messagePublisher.Received(1).PublishAsync("email", Arg.Any<EmailJobMessage>());
    }

    #endregion

    #region Helpers

    private static Order CreateTestOrder()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        return new Order
        {
            Id = 42,
            CustomerId = 1,
            RestaurantId = 1,
            Restaurant = restaurant,
            TotalAmount = 35.50m,
            Items =
            [
                new OrderItem
                {
                    DishId = 100,
                    Dish = new Dish { Id = 100, Name = "Pizza Margherita" },
                    Quantity = 2,
                    UnitPrice = 12.00m
                },
                new OrderItem
                {
                    DishId = 200,
                    Dish = new Dish { Id = 200, Name = "Tiramisu" },
                    Quantity = 1,
                    UnitPrice = 11.50m
                }
            ]
        };
    }

    #endregion
}
