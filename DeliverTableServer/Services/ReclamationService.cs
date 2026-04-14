using DeliverTableServer.Services.Interfaces;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.Reclamation;
using DeliverTableServer.Configuration;
using DeliverTableSharedLibrary.Constants;
using System.Text.RegularExpressions;
using DeliverTableServer.Models;

namespace DeliverTableServer.Services;

public class ReclamationService(
    IReclamationRepository reclamationRepository,
    IObjectStorageService objectStorage,
    AppEnvironment appEnvironment
    ) : IReclamationService
{

    private readonly long _maxUploadBytes = UploadLimits.ToBytes(appEnvironment.UploadMaxSizeMb);
    private readonly int _maxUploadMb = appEnvironment.UploadMaxSizeMb;

    public async Task<List<Reclamation>> GetAllReclamations(ReclamationQuery query)
    {
        return await reclamationRepository.GetAllReclamations(query);
    }

    public async Task<List<Reclamation>> GetReclamationsByUser(int userId)
    {
        return await reclamationRepository.GetReclamationsByUser(userId);
    }

    public async Task<List<Reclamation>> GetReclamationsByRestaurant(int restaurantId)
    {
        return await reclamationRepository.GetReclamationsByRestaurant(restaurantId);
    }

    public async Task<Reclamation?> GetReclamationById(int reclamationId)
    {
        return await reclamationRepository.GetReclamationById(reclamationId);
    }

    public async Task<Reclamation?> GetReclamationsByOrderId(int orderId)
    {
        return await reclamationRepository.GetReclamationsByOrderId(orderId);
    }

    public async Task<Reclamation> UpdateReclamation(int reclamationId, Reclamation reclamation)
    {
        return await reclamationRepository.UpdateReclamation(reclamationId, reclamation);
    }

    public async Task<Reclamation> CreateReclamation(CreateReclamationDto reclamation, IFormFileCollection images)
    {
        Reclamation newReclamation = await reclamationRepository.CreateReclamation(reclamation);
        foreach (IFormFile image in images)
        {
            string fileName = image.Name;
            var match = Regex.Match(fileName, @"Item_(?<itemId>\d+)_image");
            if (match.Success)
            {
                bool status = int.TryParse(match.Groups["itemId"].Value, out int parsedItemId);
                ReclamationItem? item = newReclamation.Items.Where(i => i.OrderItemId == parsedItemId).FirstOrDefault();
                if (status && item != null)
                {
                    await objectStorage.UploadAsync(image, ApiRoutes.Reclamation.ImageFolder, item.Id);
                }
            }
        }
        return newReclamation;
    }

    public async Task DeleteReclamation(int reclamationId)
    {
        await reclamationRepository.DeleteReclamation(reclamationId);
    }
}