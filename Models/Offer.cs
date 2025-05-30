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

        public int? MessageId { get; set; }

        public bool IsCounterOffer { get; set; } = false;

        public int? OriginalOfferId { get; set; }

        public DateTime? ResponseDate { get; set; }

        [ForeignKey("BuyerId")]
        public virtual User Buyer { get; set; }

        [ForeignKey("ListingId")]
        public virtual Listing Listing { get; set; }

        [ForeignKey("MessageId")]
        public virtual Message Messages { get; set; }

        [ForeignKey("OriginalOfferId")]
        public virtual Offer OriginalOffer { get; set; }

        public virtual ICollection<Offer> CounterOffers { get; set; }

        public virtual Purchase Purchase { get; set; }
    }
}