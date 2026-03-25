namespace DeliverTableSharedLibrary.Dtos.Admin;

/// <summary>
///     Comprehensive dashboard analytics payload with time-series data,
///     breakdowns, and ranked lists for the admin command center.
/// </summary>
public class AdminDashboardAnalyticsResponse
{
    public List<DailyRevenuePoint> RevenueByDay { get; set; } = [];
    public List<DailyCountPoint> OrdersByDay { get; set; } = [];
    public List<DailyCountPoint> UserRegistrationsByDay { get; set; } = [];
    public List<StatusBreakdownItem> OrdersByStatus { get; set; } = [];
    public List<StatusBreakdownItem> OrdersByType { get; set; } = [];
    public List<StatusBreakdownItem> PaymentsByStatus { get; set; } = [];
    public List<TopRestaurantItem> TopRestaurantsByRevenue { get; set; } = [];
    public List<RecentOrderItem> RecentOrders { get; set; } = [];
    public decimal AverageOrderValue { get; set; }
    public decimal TodayRevenue { get; set; }
    public int TodayOrders { get; set; }
    public int NewUsersThisWeek { get; set; }
}

public class DailyRevenuePoint
{
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
}

public class DailyCountPoint
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
}

public class StatusBreakdownItem
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class TopRestaurantItem
{
    public int RestaurantId { get; set; }
    public string Name { get; set; } = "";
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class RecentOrderItem
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = "";
    public string RestaurantName { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public string OrderType { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
