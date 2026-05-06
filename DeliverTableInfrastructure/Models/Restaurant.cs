using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Models
{
    public class Restaurant
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        public RestaurantType Type { get; set; } = RestaurantType.Autre;
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        [Required]
        public string AdressLine1 { get; set; } = string.Empty;
        public string AdressLine2 { get; set; } = string.Empty;
        [Required]
        public string City { get; set; } = string.Empty;
        [Required]
        [MaxLength(20)]
        public string ZipCode { get; set; } = string.Empty;
        [Required]
        public string Country { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public User Owner { get; set; } = null!;
        public List<Dish> Dishes { get; set; } = [];
        [Column(TypeName = "decimal(9, 2)")]
        public decimal Balance { get; set; }
        public List<RestaurantTransaction> Transactions { get; set; } = [];
        [MaxLength(14)]
        public string Siret { get; set; } = string.Empty;
        [MaxLength(200)]
        public string LegalName { get; set; } = string.Empty;
        [MaxLength(500)]
        public string LegalAddress { get; set; } = string.Empty;
        [MaxLength(50)]
        public string LegalForm { get; set; } = string.Empty;
        public bool IsVatRegistered { get; set; } = true;
        [MaxLength(20)]
        public string? VatNumber { get; set; }
    }
}