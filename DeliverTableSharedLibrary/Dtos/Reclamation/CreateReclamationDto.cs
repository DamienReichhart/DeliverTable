using System.ComponentModel.DataAnnotations;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Reclamation;

public class CreateReclamationDto
{
    public int OrderId { get; set; }
    [Required(ErrorMessage = "Veuillez décrire votre problème")]
    [MaxLength(10000, ErrorMessage = "Votre message ne peut pas excéder 10 000 caractères.")]
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = nameof(ReclamationType.Other);
    public List<CreateReclamationItemDto> Items { get; set; } = [];
}