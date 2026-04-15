using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Dispute;

public class DisputeAdminFilter
{
    public DisputeState? State { get; set; }
    public int? RestaurantId { get; set; }
    public int? OrderId { get; set; }
    public int? Year { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
