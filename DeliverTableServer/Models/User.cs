using Microsoft.AspNetCore.Identity;

namespace DeliverTableServer.Models;

public class User : IdentityUser<int>
{

    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";

    public UserRole Role { get; set; } = UserRole.Customer;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public RestaurantOwner? RestaurantOwner { get; set; }
    public CustomerProfile? CustomerProfile { get; set; }
}