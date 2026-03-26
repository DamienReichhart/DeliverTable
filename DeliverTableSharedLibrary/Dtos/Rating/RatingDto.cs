namespace DeliverTableSharedLibrary.Dtos.Rating;

public class RatingDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
