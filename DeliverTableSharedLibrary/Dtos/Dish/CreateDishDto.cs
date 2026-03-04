using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DeliverTableSharedLibrary.Dtos.Dish
{
    public class CreateDishDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Le prix doit être un nombre avec au plus 2 nombres après la virgule.")]
        [Range(0, 99999.99, ErrorMessage = "Le prix doit être entre 0 et 99999.99")]
        public decimal BasePrice { get; set; } = 0;
        public bool IsVegetarian { get; set; } = false;
        public bool IsVegan { get; set; } = false;
        public bool IsGlutenFree { get; set; } = false;
        public bool IsAllergenHazard { get; set; } = false;
        public bool IsDishOfTheDay { get; set; } = false;
    }
}