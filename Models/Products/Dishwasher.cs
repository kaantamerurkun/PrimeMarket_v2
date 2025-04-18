using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace PrimeMarket.Models.Products
{
    public class Dishwasher : BaseWhiteGood
    {
        [Display(Name = "Washing Capacity")]
        public string Capacity { get; set; }
    }
}