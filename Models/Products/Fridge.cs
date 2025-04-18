using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace PrimeMarket.Models.Products
{
    public class Fridge : BaseWhiteGood
    {
        [Display(Name = "Volume")]
        public string Volume { get; set; }

        [Display(Name = "Freezer")]
        public bool Freezer { get; set; }
    }
}