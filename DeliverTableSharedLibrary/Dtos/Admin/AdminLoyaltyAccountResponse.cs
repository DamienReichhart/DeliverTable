namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminLoyaltyAccountResponse
{
    public int Id { get; set; }
    public int PointsBalance { get; set; }
    public string CustomerName { get; set; } = "";
    public int ProgramId { get; set; }
    public DateTime CreatedAt { get; set; }
}
