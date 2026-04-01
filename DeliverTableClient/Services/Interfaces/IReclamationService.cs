using DeliverTableSharedLibrary.Dtos.Reclamation;
using DeliverTableSharedLibrary.Interfaces;
using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableClient.Services.Interfaces;

public interface IReclamationService
{
    Task<(ReclamationDto?, ErrorResponse?)> CreateReclamationAsync(CreateReclamationDto reclamation, List<Image> images, CancellationToken ct = default);
    Task<(ReclamationDto?, ErrorResponse?)> GetByOrderIdAsync(int orderId, CancellationToken ct = default);
    Task<(List<ReclamationDto>?, ErrorResponse?)> GetAllAsync(ReclamationQuery query, CancellationToken ct = default);
    Task<(List<ReclamationDto>?, ErrorResponse?)> GetByRestaurantOwnerAsync(CancellationToken ct = default);
    Task<(ReclamationDto?, ErrorResponse?)> UpdateAsync(int reclamationId, UpdateReclamationDto reclamation, CancellationToken ct = default);
    Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int reclamationId, CancellationToken ct = default);
    Task<(ReclamationDto?, ErrorResponse?)> RefundAsync(int reclamationId, RefundReclamationDto dto, CancellationToken ct = default);
    Task<(ReclamationDto?, ErrorResponse?)> ResolveAsync(int reclamationId, CancellationToken ct = default);
    Task<(ReclamationDto?, ErrorResponse?)> ContestAsync(int reclamationId, CancellationToken ct = default);
    Task<(ReclamationDto?, ErrorResponse?)> CompleteAsync(int reclamationId, CancellationToken ct = default);
}
