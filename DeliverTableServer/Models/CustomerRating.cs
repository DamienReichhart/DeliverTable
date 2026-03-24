using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableServer.Models;

public class CustomerRating
{
    [Key]
    public int Id { get; set; }

    public int OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;

    public int RestaurantId { get; set; }

    [ForeignKey("RestaurantId")]
    public Restaurant Restaurant { get; set; } = null!;

    public int RatedCustomerUserId { get; set; }

    [ForeignKey("RatedCustomerUserId")]
    public User RatedCustomerUser { get; set; } = null!;

    public int RestaurantUserId { get; set; }

    [ForeignKey("RestaurantUserId")]
    public User RestaurantUser { get; set; } = null!;

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(2000)]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
