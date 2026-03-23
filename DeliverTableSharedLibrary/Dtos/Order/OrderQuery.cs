namespace DeliverTableSharedLibrary.Dtos.Order;

public class OrderQuery
{
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
