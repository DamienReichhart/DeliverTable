using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Reclamation;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface IReclamationRepository
{
    Task<List<ReclamationDto>> GetAllReclamations(ReclamationQuery query);
    Task<Reclamation?> GetReclamationById(int reclamationId);
    Task<Reclamation?> GetReclamationsByOrderId(int orderId);
    Task<List<Reclamation>> GetReclamationsByUser(int userId);
    Task<List<Reclamation>> GetReclamationsByRestaurant(int restaurantId);
    Task<List<Reclamation>> GetReclamationsByRestaurantOwner(int ownerId);
    Task<Reclamation?> UpdateReclamation(int reclamationId, UpdateReclamationDto reclamation);
    Task<Reclamation?> UpdateReclamationStatus(int reclamationId, ReclamationStatus status);
    Task<Reclamation> CreateReclamation(CreateReclamationDto reclamation);
    Task<bool> DeleteReclamation(int reclamationId);
}
