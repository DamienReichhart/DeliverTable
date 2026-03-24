using DeliverTableServer.Models;

namespace DeliverTableServer.Repositories.Interfaces;

public interface ITransactionRepository
{
    Task<List<RestaurantTransaction>> GetAllAsync(CancellationToken ct = default);
    Task<RestaurantTransaction?> GetByIdAsync(int id, CancellationToken ct = default);
}
