using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableServer.Models;

public class CustomerFavouriteRestaurant
{
    public int CustomerUserId { get; set; }

    [ForeignKey("CustomerUserId")]
    public User CustomerUser { get; set; } = null!;

    public int RestaurantId { get; set; }

    [ForeignKey("RestaurantId")]
    public Restaurant Restaurant { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
