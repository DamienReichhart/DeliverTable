namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminCustomerRatingResponse
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
    public string RestaurantName { get; set; } = "";
    public string RatedCustomerName { get; set; } = "";
    public string RaterName { get; set; } = "";
    public int OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}
