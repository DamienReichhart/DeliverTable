using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Reclamation;

public class CreateReclamationItemDto
{
    [Required]
    public int OrderItemId { get; set; }

    public bool HasImage { get; set; } = false;
}