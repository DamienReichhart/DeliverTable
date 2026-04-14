using DeliverTableServer.Data;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories;

public class ReclamationRepository(
    DeliverTableContext dbContext
    ): IReclamationRepository
{
    public async Task<List<Reclamation>> GetAllReclamations()
    {
        return await dbContext.Reclamations.ToListAsync();
    }
}