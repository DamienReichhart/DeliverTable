using DeliverTableInfrastructure.Models;
using DeliverTableServer.Common;

namespace DeliverTableServer.Services.Interfaces;

public interface IEmailJobService
{
    Task<ServiceResult> QueueOrderConfirmationAsync(Order order, string customerEmail, string customerName);
    Task<ServiceResult> QueueOrderStatusUpdateAsync(Order order, string customerEmail, string newStatus);
    Task<ServiceResult> QueueOrderDeliveredAsync(Order order, string customerEmail, string customerName);
    Task<ServiceResult> QueueOrderCancelledAsync(Order order, string customerEmail, string customerName);
    Task<ServiceResult> QueueOrderReadyAsync(Order order, string customerEmail, string customerName);
    Task<ServiceResult> QueueNewOrderForRestaurantAsync(Order order, string ownerEmail, string restaurantName);
    Task<ServiceResult> QueuePasswordResetAsync(string email, string userName, string resetLink);
    Task<ServiceResult> QueuePasswordChangedAsync(string email, string userName);
    Task<ServiceResult> QueueWelcomeEmailAsync(string email, string userName);
}
