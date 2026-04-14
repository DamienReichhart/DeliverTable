using System.ComponentModel.DataAnnotations;

namespace DeliverTableInfrastructure.Models;

public class ProcessedStripeEvent
{
    [Key]
    [MaxLength(200)]
    public string StripeEventId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
