using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PrimeMarket.Models.ViewModel
{
    public class ListingViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Condition is required")]
        public string Condition { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Subcategory is required")]
        public string SubCategory { get; set; }

        [Required(ErrorMessage = "Detail category is required")]
        public string DetailCategory { get; set; }


        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; }

        public Dictionary<string, string> DynamicProperties { get; set; }

        public List<ListingImage> Images { get; set; }
    }
}