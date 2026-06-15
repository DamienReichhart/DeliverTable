using System.Globalization;
using System.Text.Json;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableInfrastructure.TemplateData;
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dispute;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;
using DeliverTableSharedLibrary.Enums;
using Microsoft.Extensions.Logging;

namespace DeliverTableServer.Services;

public class DisputeService(
    IDisputeRepository disputeRepository,
    IPaymentRepository paymentRepository,
    IOrderRepository orderRepository,
    IRestaurantRepository restaurantRepository,
    IRestaurantTransactionRepository transactionRepository,
    IEmailJobRepository emailJobRepository,
    IAdminNotificationService notifications,
    IMessagePublisher publisher,
    AppEnvironment env,
    ILogger<DisputeService> logger) : IDisputeService
{
    public async Task<ServiceResult<Dispute>> HandleCreatedAsync(
        Stripe.Dispute stripeDispute, List<Func<Task>> deferredPublishes, CancellationToken ct)
    {
        Dispute? existing = await disputeRepository.GetByStripeDisputeIdAsync(stripeDispute.Id, ct);
        if (existing is not null)
        {
            logger.LogInformation("Dispute {StripeDisputeId} already exists, skipping", stripeDispute.Id);
            return ServiceResult<Dispute>.Success(existing);
        }

        Payment? payment = await paymentRepository.GetByStripeChargeIdAsync(stripeDispute.ChargeId, ct);
        if (payment is null)
        {
            logger.LogWarning(
                "Dispute {DisputeId} references charge {ChargeId} with no matching Payment row",
                stripeDispute.Id, stripeDispute.ChargeId);
            return new ServiceError(ErrorMessages.DisputePaymentNotFound);
        }

        Order? order = await orderRepository.GetByIdAsync(payment.OrderId, ct);
        if (order is null)
            return new ServiceError(ErrorMessages.OrderNotFound);

        Restaurant? restaurant = await restaurantRepository.GetByIdWithOwnerAsync(order.RestaurantId, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound);

        decimal amount = stripeDispute.Amount / 100m;

        Dispute dispute = new Dispute
        {
            StripeDisputeId = stripeDispute.Id,
            PaymentId = payment.Id,
            OrderId = order.Id,
            RestaurantId = restaurant.Id,
            Amount = amount,
            Currency = (stripeDispute.Currency ?? "eur").ToUpperInvariant(),
            ReasonCode = stripeDispute.Reason ?? string.Empty,
            State = DisputeState.Open,
            DueBy = stripeDispute.EvidenceDetails?.DueBy,
            OpenedAt = stripeDispute.Created,
            StripePayload = SerializeDispute(stripeDispute),
        };
        await disputeRepository.CreateAsync(dispute, ct);

        decimal balanceAfter = restaurant.Balance - amount;
        await transactionRepository.CreateAsync(new RestaurantTransaction
        {
            RestaurantId = restaurant.Id,
            OrderId = order.Id,
            Type = TransactionType.DisputeReversal,
            GrossAmount = amount,
            CommissionAmount = 0m,
            NetAmount = -amount,
            BalanceAfter = balanceAfter,
            CreatedAt = DateTime.UtcNow,
        }, ct);
        restaurant.Balance = balanceAfter;
        await restaurantRepository.UpdateAsync(restaurant, ct);

        await RaiseNotificationsAndQueueEmailsAsync(dispute, restaurant, "open", deferredPublishes, ct);

        return ServiceResult<Dispute>.Success(dispute);
    }

    public async Task<ServiceResult> HandleUpdatedAsync(Stripe.Dispute stripeDispute, CancellationToken ct)
    {
        Dispute? dispute = await disputeRepository.GetByStripeDisputeIdAsync(stripeDispute.Id, ct);
        if (dispute is null)
        {
            logger.LogWarning("Dispute update for unknown StripeDisputeId {Id}", stripeDispute.Id);
            return new ServiceError(ErrorMessages.DisputeNotFound);
        }

        dispute.DueBy = stripeDispute.EvidenceDetails?.DueBy ?? dispute.DueBy;
        dispute.StripePayload = SerializeDispute(stripeDispute);
        await disputeRepository.UpdateAsync(dispute, ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> HandleClosedAsync(
        Stripe.Dispute stripeDispute, List<Func<Task>> deferredPublishes, CancellationToken ct)
    {
        Dispute? dispute = await disputeRepository.GetByStripeDisputeIdAsync(stripeDispute.Id, ct);
        if (dispute is null)
            return new ServiceError(ErrorMessages.DisputeNotFound);

        if (dispute.State != DisputeState.Open)
        {
            logger.LogInformation(
                "Dispute {DisputeId} already in state {State}, skipping close",
                dispute.Id, dispute.State);
            return ServiceResult.Success();
        }

        string status = (stripeDispute.Status ?? string.Empty).ToLowerInvariant();
        string eventKey;

        Restaurant? restaurant = null;
        if (status == "won" || status == "lost")
        {
            restaurant = await restaurantRepository.GetByIdWithOwnerAsync(dispute.RestaurantId, ct);
            if (restaurant is null)
                return new ServiceError(ErrorMessages.RestaurantNotFound);
        }

        if (status == "won")
        {
            dispute.State = DisputeState.Won;
            dispute.ClosedAt = DateTime.UtcNow;
            dispute.StripePayload = SerializeDispute(stripeDispute);

            decimal balanceAfter = restaurant!.Balance + dispute.Amount;
            await transactionRepository.CreateAsync(new RestaurantTransaction
            {
                RestaurantId = restaurant.Id,
                OrderId = dispute.OrderId,
                Type = TransactionType.DisputeRestored,
                GrossAmount = dispute.Amount,
                CommissionAmount = 0m,
                NetAmount = dispute.Amount,
                BalanceAfter = balanceAfter,
                CreatedAt = DateTime.UtcNow,
            }, ct);
            restaurant.Balance = balanceAfter;
            await restaurantRepository.UpdateAsync(restaurant, ct);
            eventKey = "won";
        }
        else if (status == "lost")
        {
            dispute.State = DisputeState.Lost;
            dispute.ClosedAt = DateTime.UtcNow;
            dispute.StripePayload = SerializeDispute(stripeDispute);
            eventKey = "lost";
        }
        else
        {
            logger.LogInformation(
                "Dispute {DisputeId} closed with unhandled status {Status}",
                stripeDispute.Id, status);
            return ServiceResult.Success();
        }

        await disputeRepository.UpdateAsync(dispute, ct);
        await RaiseNotificationsAndQueueEmailsAsync(dispute, restaurant!, eventKey, deferredPublishes, ct);
        return ServiceResult.Success();
    }

    public Task<bool> HasOpenDisputeForOrderAsync(int orderId, CancellationToken ct) =>
        disputeRepository.HasOpenForOrderAsync(orderId, ct);

    public async Task<ServiceResult<PaginatedResult<AdminDisputeRowDto>>> ListForAdminAsync(
        DisputeAdminFilter filter, CancellationToken ct)
    {
        (List<Dispute>? items, int total) = await disputeRepository.AdminListAsync(
            filter.State, filter.RestaurantId, filter.OrderId, filter.Year,
            filter.Page, filter.PageSize, ct);

        List<AdminDisputeRowDto> rows = items.Select(d => new AdminDisputeRowDto(
            d.Id,
            d.StripeDisputeId,
            d.OrderId,
            d.RestaurantId,
            d.Restaurant?.Name ?? string.Empty,
            d.Order?.Customer?.Email ?? string.Empty,
            d.Amount,
            d.Currency,
            d.ReasonCode,
            d.State,
            d.OpenedAt,
            d.ClosedAt,
            d.DueBy)).ToList();

        return new PaginatedResult<AdminDisputeRowDto>
        {
            Items = rows,
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize,
        };
    }

    public async Task<ServiceResult<PaginatedResult<DisputeRowDto>>> ListForRestaurantAsync(
        int restaurantId, int page, int pageSize, int userId, bool isAdmin, CancellationToken ct)
    {
        if (!isAdmin)
        {
            Restaurant? resto = await restaurantRepository.GetByIdAsync(restaurantId, ct);
            if (resto is null)
                return new ServiceError(ErrorMessages.RestaurantNotFound);
            if (resto.OwnerId != userId)
                return ServiceError.Forbidden(ErrorMessages.DisputeAccessDenied);
        }

        (List<Dispute>? items, int total) = await disputeRepository.ListForRestaurantAsync(restaurantId, page, pageSize, ct);
        List<DisputeRowDto> rows = items.Select(d => new DisputeRowDto(
            d.Id,
            d.StripeDisputeId,
            d.OrderId,
            d.Amount,
            d.Currency,
            d.ReasonCode,
            d.State,
            d.OpenedAt,
            d.ClosedAt,
            d.DueBy)).ToList();

        return new PaginatedResult<DisputeRowDto>
        {
            Items = rows,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<ServiceResult<AdminDisputeDetailDto>> GetAdminDetailAsync(
        int disputeId, CancellationToken ct)
    {
        Dispute? dispute = await disputeRepository.GetByIdAsync(disputeId, ct);
        if (dispute is null)
            return ServiceError.NotFound(ErrorMessages.DisputeNotFound);

        Payment? payment = await paymentRepository.GetByIdAsync(dispute.PaymentId, ct);
        string stripeChargeId = payment?.StripeChargeId ?? string.Empty;
        decimal paymentAmount = payment?.Amount ?? 0m;

        AdminDisputeRowDto header = new AdminDisputeRowDto(
            dispute.Id,
            dispute.StripeDisputeId,
            dispute.OrderId,
            dispute.RestaurantId,
            dispute.Restaurant?.Name ?? string.Empty,
            dispute.Order?.Customer?.Email ?? string.Empty,
            dispute.Amount,
            dispute.Currency,
            dispute.ReasonCode,
            dispute.State,
            dispute.OpenedAt,
            dispute.ClosedAt,
            dispute.DueBy);

        string dashboardUrl = BuildStripeDashboardUrl(dispute.StripeDisputeId);

        return new AdminDisputeDetailDto(
            header,
            dashboardUrl,
            dispute.PaymentId,
            stripeChargeId,
            paymentAmount,
            new List<RestaurantTransactionDto>());
    }

    private string BuildStripeDashboardUrl(string stripeDisputeId)
    {
        string testPrefix = env.StripeSecretKey.StartsWith("sk_test_", StringComparison.Ordinal)
            ? "test/"
            : string.Empty;
        return $"https://dashboard.stripe.com/{testPrefix}disputes/{stripeDisputeId}";
    }

    private async Task RaiseNotificationsAndQueueEmailsAsync(
        Dispute dispute,
        Restaurant restaurant,
        string eventKey,
        List<Func<Task>> deferredPublishes,
        CancellationToken ct)
    {
        string notificationPayload = JsonSerializer.Serialize(new
        {
            disputeId = dispute.Id,
            stripeDisputeId = dispute.StripeDisputeId,
            orderId = dispute.OrderId,
            restaurantId = dispute.RestaurantId,
            amount = dispute.Amount,
            reason = dispute.ReasonCode,
            state = dispute.State.ToString(),
            eventKey,
        });

        await notifications.RaiseForAllAdminsAsync(NotificationType.Dispute, notificationPayload, ct);
        if (restaurant.OwnerId != 0)
        {
            await notifications.RaiseForUserAsync(
                restaurant.OwnerId, NotificationType.Dispute, notificationPayload, ct);
        }

        (EmailJobType adminType, string? adminSubject) = ResolveAdminTemplate(eventKey, dispute.OrderId);
        (EmailJobType restoType, string? restoSubject) = ResolveRestaurantTemplate(eventKey, dispute.OrderId);
        DisputeEmailData templateData = BuildTemplateData(dispute, restaurant);

        await QueueEmailAsync(adminType, env.AdminDisputeEmail, null, adminSubject, templateData, deferredPublishes, ct);

        string? ownerEmail = restaurant.Owner?.Email;
        if (!string.IsNullOrWhiteSpace(ownerEmail))
        {
            await QueueEmailAsync(
                restoType, ownerEmail, restaurant.Name, restoSubject, templateData, deferredPublishes, ct);
        }
    }

    private async Task QueueEmailAsync(
        EmailJobType type,
        string recipientEmail,
        string? recipientName,
        string subject,
        DisputeEmailData data,
        List<Func<Task>> deferredPublishes,
        CancellationToken ct)
    {
        EmailJob job = new EmailJob
        {
            Type = type,
            Status = EmailJobStatus.Pending,
            RecipientEmail = recipientEmail,
            RecipientName = recipientName,
            Subject = subject,
            TemplateData = JsonSerializer.Serialize(data),
            MaxRetries = 5,
        };
        await emailJobRepository.CreateAsync(job, ct);

        int jobId = job.Id;
        deferredPublishes.Add(async () =>
        {
            try
            {
                await publisher.PublishAsync(MessagingExchanges.Email, new EmailJobMessage(jobId), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to publish dispute email job {JobId} to RabbitMQ. Sweep will recover it.", jobId);
            }
        });
    }

    private static (EmailJobType type, string subject) ResolveAdminTemplate(string eventKey, int orderId) =>
        eventKey switch
        {
            "won" => (EmailJobType.DisputeWonAdmin, $"Litige gagné sur la commande #{orderId}"),
            "lost" => (EmailJobType.DisputeLostAdmin, $"Litige perdu sur la commande #{orderId}"),
            _ => (EmailJobType.DisputeOpenedAdmin, $"Nouveau litige sur la commande #{orderId}"),
        };

    private static (EmailJobType type, string subject) ResolveRestaurantTemplate(string eventKey, int orderId) =>
        eventKey switch
        {
            "won" => (EmailJobType.DisputeWonRestaurant, $"Litige gagné sur la commande #{orderId}"),
            "lost" => (EmailJobType.DisputeLostRestaurant, $"Litige perdu sur la commande #{orderId}"),
            _ => (EmailJobType.DisputeOpenedRestaurant, $"Un litige a été ouvert sur la commande #{orderId}"),
        };

    private DisputeEmailData BuildTemplateData(Dispute dispute, Restaurant restaurant)
    {
        return new DisputeEmailData(
            dispute.Id,
            dispute.StripeDisputeId,
            dispute.OrderId,
            dispute.RestaurantId,
            restaurant.Name ?? string.Empty,
            dispute.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            dispute.Currency,
            dispute.ReasonCode,
            dispute.DueBy?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            BuildStripeDashboardUrl(dispute.StripeDisputeId),
            $"/admin/litiges/{dispute.Id}");
    }

    private static string SerializeDispute(Stripe.Dispute d)
    {
        return JsonSerializer.Serialize(new
        {
            id = d.Id,
            chargeId = d.ChargeId,
            status = d.Status,
            reason = d.Reason,
            amount = d.Amount,
            currency = d.Currency,
            created = d.Created,
            dueBy = d.EvidenceDetails?.DueBy,
        });
    }
}
