using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;

namespace DeliverTableServer.Services;

public class ReclamationService(
    IReclamationRepository reclamationRepository
    ): IReclamationService
{
    public Task<List<Reclamation>> GetAllReclamations()
    {
        return reclamationRepository.GetAllReclamations();
    }
}