using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace PrimeMarket.Models.Products
{
    public class Laptop : BaseComputer
    {
        [Display(Name = "Keyboard")]
        public string Keyboard { get; set; }

        [Display(Name = "Screen Size")]
        public string ScreenSize { get; set; }
    }
}