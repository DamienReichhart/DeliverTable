using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Restaurant;

public class UpdateTablesCapacityRequest
{
    [Range(0, int.MaxValue, ErrorMessage = "Le nombre de tables doit etre positif")]
    public int CapacityPerSlot { get; set; }
}
