using System.ComponentModel.DataAnnotations;
using DeliverTableSharedLibrary.Enums;
using DeliverTableSharedLibrary.Validation;

namespace DeliverTableSharedLibrary.Dtos.Restaurant;

public class CreateRestaurantDto
{
    [Required(ErrorMessage = "Ce champ est requis")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ce champ est requis")]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ce champ est requis")]
    public string AdressLine1 { get; set; } = string.Empty;

    public string AdressLine2 { get; set; } = string.Empty;

    public string Type { get; set; } = RestaurantType.Autre.ToString();

    [Required(ErrorMessage = "Ce champ est requis")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ce champ est requis")]
    [MinLength(5, ErrorMessage = "Votre code postal doit comporter 5 caractères")]
    [MaxLength(5, ErrorMessage = "Votre code postal doit comporter 5 caractères")]
    public string ZipCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ce champ est requis")]
    public string Country { get; set; } = AvailableCountries.France.ToString();

    [Required(ErrorMessage = "Pour des raisons juridiques, le Siret est requis")]
    [MaxLength(14, ErrorMessage = "Le Siret doit comporter 14 chiffres")]
    [MinLength(14, ErrorMessage = "Le Siret doit comporter 14 chiffres")]
    [RegularExpression("^[0-9]{14}$", ErrorMessage = "Le Siret doit comporter 14 chiffres")]
    [Siret]
    public string Siret { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ce champ est requis")]
    [MaxLength(200)]
    public string LegalName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string LegalAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LegalForm { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? VatNumber { get; set; }

    public bool IsVatRegistered { get; set; } = true;
}
