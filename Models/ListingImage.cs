using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrimeMarket.Models
{
    public class ListingImage : BaseEntity
    {
        [Required]
        public int ListingId { get; set; }

        [Required]
        public string ImagePath { get; set; }

        public bool IsMainImage { get; set; } = false;

        [ForeignKey("ListingId")]
        public virtual Listing Listing { get; set; }
    }
}
