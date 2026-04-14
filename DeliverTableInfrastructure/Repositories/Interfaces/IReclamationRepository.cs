using DeliverTableServer.Models;

namespace DeliverTableServer.Repositories.Interfaces;

public interface IReclamationRepository
{
    Task<List<Reclamation>> GetAllReclamations();
}