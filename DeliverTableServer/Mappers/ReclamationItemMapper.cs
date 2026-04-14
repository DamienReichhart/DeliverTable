using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos.Reclamation;
namespace DeliverTableServer.Mappers;

public static class ReclamationItemMapper
{
    public static ReclamationItemDto ToDto(this ReclamationItem reclamationItem)
    {
        if (reclamationItem?.OrderItem == null) throw new ArgumentNullException(nameof(reclamationItem));
        return new ReclamationItemDto
        {
            Id = reclamationItem.Id,
            Item = reclamationItem.OrderItem.ToDto(),
            HasAttachedImage = reclamationItem.HasAttachedImage,
            ImagePath = reclamationItem.HasAttachedImage ?
                ApiRoutes.Reclamation.ImagePath + $"{reclamationItem.Id}.png" : null
        };
    }
}
