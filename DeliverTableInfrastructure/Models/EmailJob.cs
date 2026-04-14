using System.ComponentModel.DataAnnotations;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Models;

public class EmailJob
{
    [Key]
    public int Id { get; set; }

    public EmailJobType Type { get; set; }

    public EmailJobStatus Status { get; set; }

    [Required]
    [MaxLength(320)]
    public string RecipientEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? RecipientName { get; set; }

    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string TemplateData { get; set; } = string.Empty;

    public int RetryCount { get; set; }

    public int MaxRetries { get; set; } = 5;

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    [MaxLength(400)]
    public string? AttachmentStoragePath { get; set; }

    [MaxLength(200)]
    public string? AttachmentFilename { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
