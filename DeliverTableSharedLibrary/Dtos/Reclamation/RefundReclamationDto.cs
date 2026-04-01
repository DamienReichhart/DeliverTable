namespace DeliverTableSharedLibrary.Dtos.Reclamation;

public class RefundReclamationDto
{
    /// <summary>
    /// IDs des ReclamationItem à rembourser. Vide = remboursement total de la commande.
    /// </summary>
    public List<int> ItemIds { get; set; } = [];
}
