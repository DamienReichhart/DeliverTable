using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace DeliverTableServer.Models;

public class User : IdentityUser<int>
{
    [Required]
    public override string? Email { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public RestaurantOwner? RestaurantOwner { get; set; }
    public CustomerProfile? CustomerProfile { get; set; }
}