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

        // Related message ID (to link offer with conversation)
        public int? MessageId { get; set; }

        // Flag for counter offers (if seller responds with different price)
        public bool IsCounterOffer { get; set; } = false;

        // Reference to original offer if this is a counter offer
        public int? OriginalOfferId { get; set; }

        // When offer was accepted/rejected/completed
        public DateTime? ResponseDate { get; set; }

        // Navigation properties
        [ForeignKey("BuyerId")]
        public virtual User Buyer { get; set; }

        [ForeignKey("ListingId")]
        public virtual Listing Listing { get; set; }

        [ForeignKey("MessageId")]
        public virtual Message Messages { get; set; }

        [ForeignKey("OriginalOfferId")]
        public virtual Offer OriginalOffer { get; set; }

        // Collection of counter offers
        public virtual ICollection<Offer> CounterOffers { get; set; }

        // Link to purchase if this offer was accepted and purchased
        public virtual Purchase Purchase { get; set; }
    }
}