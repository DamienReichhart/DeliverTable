using System.ComponentModel.DataAnnotations;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Identity;

namespace DeliverTableInfrastructure.Models;

public class User : IdentityUser<int>
{
    [Required]
    [EmailAddress]
    public override string? Email { get; set; }

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string? StripeCustomerId { get; set; }

    public RestaurantOwner? RestaurantOwner { get; set; }
    public Customer? Customer { get; set; }

    public List<Restaurant> Restaurants { get; set; } = [];
}