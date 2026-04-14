using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.Reclamation;
namespace DeliverTableServer.Mappers;

public static class ReclamationItemMapper
{
    public static ReclamationItemDto ToDto(this ReclamationItem reclamationItem)
    {
        if (reclamationItem?.OrderItem == null) throw new ArgumentNullException(nameof(reclamationItem));
        return new ReclamationItemDto
        {
            Item = reclamationItem.OrderItem.ToDto(),
            HasAttachedImage = reclamationItem.HasAttachedImage
        };
    }
}
