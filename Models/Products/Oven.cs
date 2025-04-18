using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace PrimeMarket.Models.Products
{
    public class Oven : BaseWhiteGood
    {
        [Display(Name = "Timer")]
        public bool Timer { get; set; }

        [Display(Name = "Volume")]
        public string Volume { get; set; }
    }
}