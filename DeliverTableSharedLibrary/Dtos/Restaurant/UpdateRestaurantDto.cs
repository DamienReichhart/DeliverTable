using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using DeliverTableSharedLibrary.Constants.Enums;

namespace DeliverTableSharedLibrary.Dtos.Restaurant
{
    public class UpdateRestaurantDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string AdressLine1 { get; set; } = string.Empty;

        public string AdressLine2 { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        public string Type { get; set; } = RestaurantType.Autre.ToString();

        [Required]
        [MaxLength(20)]
        public string ZipCode { get; set; } = string.Empty;

        [Required]
        public string Country { get; set; } = string.Empty;
    }
}