using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Reclamation;
namespace DeliverTableServer.Mappers;

public static class ReclamationMapper
{
    public static ReclamationDto ToDto(this Reclamation reclamation)
    {
        return new ReclamationDto
        {
            ReclamationId = reclamation.ReclamationId,
            Type = reclamation.Type,
            Status = reclamation.Status,
            Description = reclamation.Description,
            Created = reclamation.Created,
            Updated = reclamation.Updated,
            Items = reclamation.Items?.Select(item => item.ToDto()).ToList() ?? [],
            OrderId = reclamation.OrderId,
            OrderTotalAmount = reclamation.Order?.TotalAmount ?? 0,
            RefundAmount = reclamation.RefundAmount
        };
    }
}
