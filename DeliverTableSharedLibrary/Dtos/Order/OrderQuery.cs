namespace DeliverTableSharedLibrary.Dtos.Order;

public class OrderQuery
{
    public string? Status { get; set; }
    public bool? ToPrepare { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public string? SortBy { get; set; }
    public bool? SortDesc { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
