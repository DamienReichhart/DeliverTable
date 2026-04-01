using DeliverTableServer.Services.Interfaces;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.Reclamation;
using DeliverTableServer.Configuration;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Enums;
using System.Text.RegularExpressions;
using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableServer.Models;

namespace DeliverTableServer.Services;

public class ReclamationService(
    IReclamationRepository reclamationRepository,
    IRestaurantRepository restaurantRepository,
    IRestaurantTransactionRepository transactionRepository,
    IObjectStorageService objectStorage,
    AppEnvironment appEnvironment
    ) : IReclamationService
{

    private readonly long _maxUploadBytes = UploadLimits.ToBytes(appEnvironment.UploadMaxSizeMb);
    private readonly int _maxUploadMb = appEnvironment.UploadMaxSizeMb;

    public async Task<ServiceResult<List<ReclamationDto>>> GetAllReclamations(ReclamationQuery query)
    {
        return await reclamationRepository.GetAllReclamations(query);
    }

    public async Task<ServiceResult<List<ReclamationDto>>> GetReclamationsByUser(int userId)
    {
        var reclamations = await reclamationRepository.GetReclamationsByUser(userId);
        return reclamations.Select(r => r.ToDto()).ToList();
    }

    public async Task<ServiceResult<List<ReclamationDto>>> GetReclamationsByRestaurant(int restaurantId)
    {
        var reclamations = await reclamationRepository.GetReclamationsByRestaurant(restaurantId);
        return reclamations.Select(r => r.ToDto()).ToList();
    }

    public async Task<ServiceResult<ReclamationDto>> GetReclamationById(int reclamationId)
    {
        Reclamation? reclamation = await reclamationRepository.GetReclamationById(reclamationId);
        if (reclamation == null)
            return new ServiceError(ErrorMessages.ReclamationNotFound, 404);

        return reclamation.ToDto();
    }

    public async Task<ServiceResult<ReclamationDto>> GetReclamationsByOrderId(int orderId)
    {
        Reclamation? reclamation = await reclamationRepository.GetReclamationsByOrderId(orderId);
        if (reclamation == null)
            return new ServiceError(ErrorMessages.ReclamationNotFound, 404);
        return reclamation.ToDto();
    }

    public async Task<ServiceResult<ReclamationDto>> UpdateReclamation(int reclamationId, UpdateReclamationDto reclamation)
    {
        Reclamation? updated = await reclamationRepository.UpdateReclamation(reclamationId, reclamation);
        if (updated == null)
            return new ServiceError(ErrorMessages.ReclamationNotFound, 404);

        return updated.ToDto();
    }

    public async Task<ServiceResult<ReclamationDto>> CreateReclamation(CreateReclamationDto reclamation, IFormFileCollection images)
    {
        if (!Enum.TryParse<ReclamationType>(
                reclamation.Type,
                ignoreCase: true,
                out _))
        {
            return new ServiceError(ErrorMessages.InvalidFields, 400);
        }

        foreach (IFormFile image in images)
        {
            if (image.Length > _maxUploadBytes)
                return new ServiceError(ErrorMessages.FileTooLarge(_maxUploadMb), 413);
        }

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
        return newReclamation.ToDto();
    }

    public async Task<ServiceResult> DeleteReclamation(int reclamationId)
    {
        bool deleted = await reclamationRepository.DeleteReclamation(reclamationId);
        if (!deleted)
            return new ServiceError(ErrorMessages.ReclamationNotFound, 404);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<List<ReclamationDto>>> GetReclamationsByRestaurantOwner(int ownerId)
    {
        var reclamations = await reclamationRepository.GetReclamationsByRestaurantOwner(ownerId);
        return reclamations.Select(r => r.ToDto()).ToList();
    }

    public async Task<ServiceResult<ReclamationDto>> RefundReclamation(int reclamationId, int ownerId, RefundReclamationDto dto)
    {
        Reclamation? reclamation = await reclamationRepository.GetReclamationById(reclamationId);
        if (reclamation == null)
            return new ServiceError(ErrorMessages.ReclamationNotFound, 404);

        if (reclamation.Order.Restaurant.OwnerId != ownerId)
            return new ServiceError(ErrorMessages.ReclamationAccessDenied, 403);

        if (reclamation.Status != ReclamationStatus.Pending)
            return new ServiceError(ErrorMessages.ReclamationInvalidTransition, 409);

        decimal refundAmount;
        if (dto.ItemIds.Count == 0)
        {
            refundAmount = reclamation.Order.TotalAmount;
        }
        else
        {
            var selectedItems = reclamation.Items
                .Where(i => dto.ItemIds.Contains(i.Id))
                .ToList();

            if (selectedItems.Count == 0)
                return new ServiceError(ErrorMessages.ReclamationRefundNoItems, 400);

            refundAmount = selectedItems.Sum(i => i.OrderItem.UnitPrice * i.OrderItem.Quantity);
        }

        Restaurant? restaurant = await restaurantRepository.GetByIdAsync(reclamation.Order.RestaurantId);
        if (restaurant == null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (restaurant.Balance < refundAmount)
            return new ServiceError(ErrorMessages.ReclamationRefundInsufficientBalance, 400);

        restaurant.Balance -= refundAmount;
        await restaurantRepository.UpdateAsync(restaurant);

        var transaction = new RestaurantTransaction
        {
            RestaurantId = restaurant.Id,
            OrderId = reclamation.OrderId,
            Type = TransactionType.Refund,
            GrossAmount = refundAmount,
            CommissionAmount = 0,
            NetAmount = refundAmount,
            BalanceAfter = restaurant.Balance
        };
        await transactionRepository.CreateAsync(transaction);

        Reclamation? updated = await reclamationRepository.UpdateReclamationStatus(reclamationId, ReclamationStatus.Resolved);
        return updated!.ToDto();
    }

    public async Task<ServiceResult<ReclamationDto>> ResolveReclamation(int reclamationId, int ownerId)
    {
        Reclamation? reclamation = await reclamationRepository.GetReclamationById(reclamationId);
        if (reclamation == null)
            return new ServiceError(ErrorMessages.ReclamationNotFound, 404);

        if (reclamation.Order.Restaurant.OwnerId != ownerId)
            return new ServiceError(ErrorMessages.ReclamationAccessDenied, 403);

        if (reclamation.Status != ReclamationStatus.Pending)
            return new ServiceError(ErrorMessages.ReclamationInvalidTransition, 409);

        Reclamation? updated = await reclamationRepository.UpdateReclamationStatus(reclamationId, ReclamationStatus.Resolved);
        return updated!.ToDto();
    }

    public async Task<ServiceResult<ReclamationDto>> ContestReclamation(int reclamationId, int customerId)
    {
        Reclamation? reclamation = await reclamationRepository.GetReclamationById(reclamationId);
        if (reclamation == null)
            return new ServiceError(ErrorMessages.ReclamationNotFound, 404);

        if (reclamation.Order.CustomerId != customerId)
            return new ServiceError(ErrorMessages.ReclamationAccessDenied, 403);

        if (reclamation.Status != ReclamationStatus.Resolved)
            return new ServiceError(ErrorMessages.ReclamationInvalidTransition, 409);

        Reclamation? updated = await reclamationRepository.UpdateReclamationStatus(reclamationId, ReclamationStatus.Contested);
        return updated!.ToDto();
    }

    public async Task<ServiceResult<ReclamationDto>> CompleteReclamation(int reclamationId)
    {
        Reclamation? reclamation = await reclamationRepository.GetReclamationById(reclamationId);
        if (reclamation == null)
            return new ServiceError(ErrorMessages.ReclamationNotFound, 404);

        if (reclamation.Status != ReclamationStatus.Contested)
            return new ServiceError(ErrorMessages.ReclamationInvalidTransition, 409);

        Reclamation? updated = await reclamationRepository.UpdateReclamationStatus(reclamationId, ReclamationStatus.Completed);
        return updated!.ToDto();
    }
}
