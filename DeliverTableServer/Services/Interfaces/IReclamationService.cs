
using DeliverTableSharedLibrary.Dtos.Reclamation;
namespace DeliverTableServer.Services.Interfaces;

public interface IReclamationService
{
    Task<List<Reclamation>> GetAllReclamations(ReclamationQuery query);
    Task<Reclamation?> GetReclamationById(int reclamationId);
    Task<Reclamation> UpdateReclamation(int reclamationId, Reclamation reclamation);
    Task<Reclamation> CreateReclamation(CreateReclamationDto reclamation, IFormFileCollection images);
    Task DeleteReclamation(int reclamationId);
    Task<Reclamation?> GetReclamationsByOrderId(int orderId);
    Task<List<Reclamation>> GetReclamationsByUser(int userId);
    Task<List<Reclamation>> GetReclamationsByRestaurant(int restaurantId);
}