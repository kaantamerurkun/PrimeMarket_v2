using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace PrimeMarket.Models.Products
{
    public class Monitor : BaseProduct
    {
        [Display(Name = "Compatible Models")]
        public string CompatibleModels { get; set; }

        [Display(Name = "Warranty Period")]
        public string Warranty { get; set; }
    }
}