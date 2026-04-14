using System.ComponentModel.DataAnnotations;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Models;

public class InvoiceCounter
{
    [Key]
    public int Id { get; set; }

    public InvoiceIssuerType EntityType { get; set; }

    public int? EntityId { get; set; }

    public int Year { get; set; }

    public int NextNumber { get; set; } = 1;
}
