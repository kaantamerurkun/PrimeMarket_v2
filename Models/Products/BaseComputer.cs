using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace PrimeMarket.Models.Products
{
    public class BaseComputer : BaseProduct
    {
        [Display(Name = "RAM")]
        public string Ram { get; set; }

        [Display(Name = "RAM Type")]
        public string RamType { get; set; }

        [Display(Name = "HDMI")]
        public bool Hdmi { get; set; }

        [Display(Name = "GPU Memory")]
        public string GpuMemory { get; set; }

        [Display(Name = "Graphics Card")]
        public string Gpu { get; set; }

        [Display(Name = "Memory Speed")]
        public string MemorySpeed { get; set; }

        [Display(Name = "Processor")]
        public string Processor { get; set; }

        [Display(Name = "Operating System")]
        public string OperatingSystem { get; set; }

        [Display(Name = "HDD/SSD")]
        public string Storage { get; set; }

        [Display(Name = "Warranty Period")]
        public string Warranty { get; set; }
    }
}