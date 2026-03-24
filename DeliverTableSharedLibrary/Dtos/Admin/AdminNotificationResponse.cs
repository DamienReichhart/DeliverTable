namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminNotificationResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public string Payload { get; set; } = "";
    public bool IsRead { get; set; }
    public string UserName { get; set; } = "";
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
