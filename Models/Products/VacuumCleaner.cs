using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace PrimeMarket.Models.Products
{
    public class VacuumCleaner : BaseProduct
    {
        [Display(Name = "Cable Length")]
        public string CableLength { get; set; }

        [Display(Name = "Water Container")]
        public bool WaterContainer { get; set; }

        [Display(Name = "Weight")]
        public string Weight { get; set; }

        [Display(Name = "Power")]
        public string Power { get; set; }

        [Display(Name = "Dust Container")]
        public string DustContainer { get; set; }

        [Display(Name = "Warranty Period")]
        public string Warranty { get; set; }
    }
}