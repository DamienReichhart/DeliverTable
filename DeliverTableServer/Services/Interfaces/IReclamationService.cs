
using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Reclamation;
namespace DeliverTableServer.Services.Interfaces;

public interface IReclamationService
{
    Task<ServiceResult<List<ReclamationDto>>> GetAllReclamations(ReclamationQuery query);
    Task<ServiceResult<ReclamationDto>> GetReclamationById(int reclamationId);
    Task<ServiceResult<ReclamationDto>> UpdateReclamation(int reclamationId, UpdateReclamationDto reclamation);
    Task<ServiceResult<ReclamationDto>> CreateReclamation(CreateReclamationDto reclamation, IFormFileCollection images);
    Task<ServiceResult> DeleteReclamation(int reclamationId);
    Task<ServiceResult<ReclamationDto>> GetReclamationsByOrderId(int orderId);
    Task<ServiceResult<List<ReclamationDto>>> GetReclamationsByUser(int userId);
    Task<ServiceResult<List<ReclamationDto>>> GetReclamationsByRestaurant(int restaurantId);
    Task<ServiceResult<List<ReclamationDto>>> GetReclamationsByRestaurantOwner(int ownerId);
    Task<ServiceResult<ReclamationDto>> ResolveReclamation(int reclamationId, int ownerId);
    Task<ServiceResult<ReclamationDto>> RefundReclamation(int reclamationId, int ownerId, RefundReclamationDto dto);
    Task<ServiceResult<ReclamationDto>> ContestReclamation(int reclamationId, int customerId);
    Task<ServiceResult<ReclamationDto>> CompleteReclamation(int reclamationId);
}
