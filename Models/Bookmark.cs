using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrimeMarket.Models
{
    public class Bookmark : BaseEntity
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int ListingId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("ListingId")]
        public virtual Listing Listing { get; set; }
    }
}
