using DeliverTableServer.Models;

namespace DeliverTableServer.Services.Interfaces;

public interface IReclamationService
{
    Task<List<Reclamation>> GetAllReclamations();
}