using DeliverTableSharedLibrary.Dtos.Reclamation;
using DeliverTableSharedLibrary.Interfaces;

namespace DeliverTableClient.Services.Interfaces;

public interface IReclamationService
{
    public Task<bool> CreateReclamation(CreateReclamationDto reclamation, List<Image> images);
}