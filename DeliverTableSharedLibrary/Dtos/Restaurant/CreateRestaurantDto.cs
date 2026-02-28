using System.ComponentModel.DataAnnotations;
using DeliverTableSharedLibrary.Constants.Enums;

namespace DeliverTableSharedLibrary.Dtos.Restaurant
{
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

        public string Type {get; set;} = RestaurantType.Autre.ToString();

        [Required(ErrorMessage = "Ce champ est requis")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ce champ est requis")]
        [MinLength(5, ErrorMessage = "Votre code postal doit comporter 5 caractères")]
        [MaxLength(5, ErrorMessage = "Votre code postal doit comporter 5 caractères")]
        public string ZipCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ce champ est requis")]
        public string Country { get; set; } = AvailableCountries.France.ToString();
    }
}