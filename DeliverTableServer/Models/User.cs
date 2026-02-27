using System.ComponentModel.DataAnnotations;
using DeliverTableSharedLibrary.Constants.Enums;
using Microsoft.AspNetCore.Identity;

namespace DeliverTableServer.Models;

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

    public RestaurantOwner? RestaurantOwner { get; set; }
    public CustomerProfile? CustomerProfile { get; set; }

    public List<Restaurant> Restaurants { get; set; } = [];
}