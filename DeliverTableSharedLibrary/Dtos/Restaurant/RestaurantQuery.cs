using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliverTableSharedLibrary.Dtos.Restaurant
{
    public class RestaurantQuery
    {
        public string? Name { get; set; } = null;
        public string? City { get; set; } = null;
        public string? Type { get; set; } = null;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}