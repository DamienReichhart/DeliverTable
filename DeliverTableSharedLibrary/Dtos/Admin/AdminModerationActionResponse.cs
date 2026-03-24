namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminModerationActionResponse
{
    public int Id { get; set; }
    public string TargetType { get; set; } = "";
    public int TargetId { get; set; }
    public string ActionType { get; set; } = "";
    public string Reason { get; set; } = "";
    public string AdminUserName { get; set; } = "";
    public int AdminUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
