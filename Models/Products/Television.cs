using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace PrimeMarket.Models.Products
{
    public class Television : BaseProduct
    {
        [Display(Name = "Screen Size")]
        public string ScreenSize { get; set; }

        [Display(Name = "Smart TV")]
        public bool SmartTV { get; set; }

        [Display(Name = "HDMI")]
        public string Hdmi { get; set; }

        [Display(Name = "Resolution")]
        public string Resolution { get; set; }

        [Display(Name = "Display Technology")]
        public string DisplayTechnology { get; set; }

        [Display(Name = "Warranty Period")]
        public string Warranty { get; set; }
    }
}