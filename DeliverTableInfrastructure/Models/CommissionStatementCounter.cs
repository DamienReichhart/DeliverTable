using System.ComponentModel.DataAnnotations;

namespace DeliverTableInfrastructure.Models;

public class CommissionStatementCounter
{
    [Key]
    public int Id { get; set; }

    public int NextNumber { get; set; }

    public byte[]? RowVersion { get; set; }
}
