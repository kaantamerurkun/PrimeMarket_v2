using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using PrimeMarket.Models.Enum;

namespace PrimeMarket.Models
{
    public class Offer : BaseEntity
    {
        [Required]
        public int BuyerId { get; set; }

        [Required]
        public int ListingId { get; set; }

        [Required]
        public decimal OfferAmount { get; set; }

        public string Message { get; set; }

        [Required]
        public OfferStatus Status { get; set; } = OfferStatus.Pending;

        // Navigation properties
        [ForeignKey("BuyerId")]
        public virtual User Buyer { get; set; }

        [ForeignKey("ListingId")]
        public virtual Listing Listing { get; set; }
    }
}
