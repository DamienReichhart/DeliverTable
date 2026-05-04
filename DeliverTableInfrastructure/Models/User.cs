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

    [MaxLength(200)]
    public string BillingAddressLine1 { get; set; } = string.Empty;

    [MaxLength(200)]
    public string BillingAddressLine2 { get; set; } = string.Empty;

    [MaxLength(10)]
    public string BillingPostalCode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string BillingCity { get; set; } = string.Empty;

    [MaxLength(100)]
    public string BillingCountry { get; set; } = string.Empty;

    public RestaurantOwner? RestaurantOwner { get; set; }
    public Customer? Customer { get; set; }

    public List<Restaurant> Restaurants { get; set; } = [];
}