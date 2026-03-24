namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminDashboardStatsResponse
{
    public int TotalUsers { get; set; }
    public int TotalRestaurants { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ActivePromotions { get; set; }
    public int PendingOrders { get; set; }
}
