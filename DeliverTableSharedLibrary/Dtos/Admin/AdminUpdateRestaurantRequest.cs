using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminUpdateRestaurantRequest
{
    [Required(ErrorMessage = "Le nom est obligatoire")]
    [MaxLength(100)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [Required(ErrorMessage = "L'adresse est obligatoire")]
    public string AdressLine1 { get; set; } = "";

    public string? AdressLine2 { get; set; }

    [Required(ErrorMessage = "La ville est obligatoire")]
    public string City { get; set; } = "";

    [Required(ErrorMessage = "Le code postal est obligatoire")]
    [MaxLength(20)]
    public string ZipCode { get; set; } = "";

    [Required(ErrorMessage = "Le pays est obligatoire")]
    public string Country { get; set; } = "";

    public bool IsActive { get; set; }
}
