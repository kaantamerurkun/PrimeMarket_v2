using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace PrimeMarket.Models.Products
{
    [NotMapped]
    public abstract class BaseProduct
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Listing")]
        public int ListingId { get; set; }

        // Navigation property
        public virtual Listing Listing { get; set; }
    }
}
