using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace PrimeMarket.Models.Products
{
    public class BaseWhiteGood : BaseProduct
    {
        [Display(Name = "Energy Class")]
        public string EnergyClass { get; set; }

        [Display(Name = "Product Dimensions")]
        public string Dimensions { get; set; }

        [Display(Name = "Color")]
        public string Color { get; set; }

        [Display(Name = "Warranty Period")]
        public string Warranty { get; set; }
    }
}