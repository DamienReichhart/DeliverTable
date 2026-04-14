using DeliverTableSharedLibrary.Dtos.Reclamation;
namespace DeliverTableServer.Repositories.Interfaces;

public interface IReclamationRepository
{
    Task<List<Reclamation>> GetAllReclamations(ReclamationQuery query);
    Task<Reclamation?> GetReclamationById(int reclamationId);
    Task<Reclamation?> GetReclamationsByOrderId(int orderId);
    Task<List<Reclamation>> GetReclamationsByUser(int userId);
    Task<List<Reclamation?>> GetReclamationsByRestaurant(int restaurantId);
    Task<Reclamation> UpdateReclamation(int reclamationId, Reclamation reclamation);
    Task<Reclamation> CreateReclamation(CreateReclamationDto reclamation);
    Task DeleteReclamation(int reclamationId);
}