using System.Text.Json;
using DeliverTableInfrastructure.Extensions;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableInfrastructure.TemplateData;
using DeliverTableServer.Common;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Enums;
using Microsoft.Extensions.Logging;

namespace DeliverTableServer.Services;

public class EmailJobService(
    IEmailJobRepository emailJobRepository,
    IMessagePublisher messagePublisher,
    ILogger<EmailJobService> logger) : IEmailJobService
{
    public async Task<ServiceResult> QueueOrderConfirmationAsync(
        Order order, string customerEmail, string customerName)
    {
        OrderConfirmationData templateData = new OrderConfirmationData(
            order.Id,
            order.TotalAmount,
            order.Restaurant.Name,
            order.Items.Select(i => new OrderItemData(i.Dish.Name, i.Quantity, i.UnitPrice)).ToList());

        return await CreateAndPublishAsync(
            EmailJobType.OrderConfirmation,
            customerEmail,
            customerName,
            $"Confirmation de votre commande #{order.Id}",
            templateData);
    }

    public async Task<ServiceResult> QueueOrderStatusUpdateAsync(
        Order order, string customerEmail, string newStatus)
    {
        OrderStatusUpdateData templateData = new OrderStatusUpdateData(order.Id, newStatus, order.Restaurant.Name);

        return await CreateAndPublishAsync(
            EmailJobType.OrderStatusUpdate,
            customerEmail,
            null,
            $"Mise à jour de votre commande #{order.Id}",
            templateData);
    }

    public async Task<ServiceResult> QueueOrderDeliveredAsync(
        Order order, string customerEmail, string customerName)
    {
        OrderDeliveredData templateData = new OrderDeliveredData(order.Id, order.TotalAmount, order.Restaurant.Name);

        return await CreateAndPublishAsync(
            EmailJobType.OrderDelivered,
            customerEmail,
            customerName,
            $"Votre commande #{order.Id} a été livrée",
            templateData);
    }

    public async Task<ServiceResult> QueueOrderCancelledAsync(
        Order order, string customerEmail, string customerName)
    {
        OrderCancelledData templateData = new OrderCancelledData(order.Id, order.Restaurant.Name);

        return await CreateAndPublishAsync(
            EmailJobType.OrderCancelled,
            customerEmail,
            customerName,
            $"Votre commande #{order.Id} a été annulée",
            templateData);
    }

    public async Task<ServiceResult> QueueOrderReadyAsync(
        Order order, string customerEmail, string customerName)
    {
        OrderReadyData templateData = new OrderReadyData(order.Id, order.Restaurant.Name);

        return await CreateAndPublishAsync(
            EmailJobType.OrderReady,
            customerEmail,
            customerName,
            $"Votre commande #{order.Id} est prête",
            templateData);
    }

    public async Task<ServiceResult> QueueNewOrderForRestaurantAsync(
        Order order, string ownerEmail, string restaurantName)
    {
        string customerName = order.Customer?.GetFullName() ?? "Client";

        NewOrderForRestaurantData templateData = new NewOrderForRestaurantData(
            order.Id,
            customerName,
            order.OrderType.ToString(),
            order.TotalAmount,
            order.Items.Select(i => new OrderItemData(i.DishName, i.Quantity, i.UnitPrice)).ToList());

        return await CreateAndPublishAsync(
            EmailJobType.NewOrderForRestaurant,
            ownerEmail,
            restaurantName,
            $"Nouvelle commande #{order.Id} reçue",
            templateData);
    }

    public async Task<ServiceResult> QueuePasswordResetAsync(
        string email, string userName, string resetLink)
    {
        PasswordResetData templateData = new PasswordResetData(resetLink, userName);

        return await CreateAndPublishAsync(
            EmailJobType.PasswordReset,
            email,
            userName,
            "Réinitialisation de votre mot de passe",
            templateData);
    }

    public async Task<ServiceResult> QueuePasswordChangedAsync(string email, string userName)
    {
        PasswordChangedData templateData = new PasswordChangedData(userName);

        return await CreateAndPublishAsync(
            EmailJobType.PasswordChanged,
            email,
            userName,
            "Votre mot de passe a été modifié",
            templateData);
    }

    public async Task<ServiceResult> QueueWelcomeEmailAsync(string email, string userName)
    {
        WelcomeEmailData templateData = new WelcomeEmailData(userName);

        return await CreateAndPublishAsync(
            EmailJobType.WelcomeEmail,
            email,
            userName,
            "Bienvenue sur DeliverTable",
            templateData);
    }

    private async Task<ServiceResult> CreateAndPublishAsync<T>(
        EmailJobType type, string email, string? name, string subject, T templateData)
    {
        EmailJob job = new EmailJob
        {
            Type = type,
            Status = EmailJobStatus.Pending,
            RecipientEmail = email,
            RecipientName = name,
            Subject = subject,
            TemplateData = JsonSerializer.Serialize(templateData),
            MaxRetries = 5
        };

        await emailJobRepository.CreateAsync(job);

        try
        {
            await messagePublisher.PublishAsync(MessagingExchanges.Email, new EmailJobMessage(job.Id));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish email job {JobId} to RabbitMQ. Sweep will recover it.", job.Id);
        }

        return ServiceResult.Success();
    }
}
