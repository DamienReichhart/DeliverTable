using DeliverTableInfrastructure.Models;
using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dispute;

namespace DeliverTableServer.Services.Interfaces;

public interface IDisputeService
{
    Task<ServiceResult<Dispute>> HandleCreatedAsync(
        Stripe.Dispute stripeDispute, List<Func<Task>> deferredPublishes, CancellationToken ct);

    Task<ServiceResult> HandleUpdatedAsync(Stripe.Dispute stripeDispute, CancellationToken ct);

    Task<ServiceResult> HandleClosedAsync(
        Stripe.Dispute stripeDispute, List<Func<Task>> deferredPublishes, CancellationToken ct);

    Task<bool> HasOpenDisputeForOrderAsync(int orderId, CancellationToken ct);

    Task<ServiceResult<PaginatedResult<AdminDisputeRowDto>>> ListForAdminAsync(
        DisputeAdminFilter filter, CancellationToken ct);

    Task<ServiceResult<PaginatedResult<DisputeRowDto>>> ListForRestaurantAsync(
        int restaurantId, int page, int pageSize, int userId, bool isAdmin, CancellationToken ct);

    Task<ServiceResult<AdminDisputeDetailDto>> GetAdminDetailAsync(int disputeId, CancellationToken ct);
}
