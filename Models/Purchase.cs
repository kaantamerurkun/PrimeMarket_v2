using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PrimeMarket.Models.Enum;

namespace PrimeMarket.Models
{
    public class Purchase : BaseEntity
    {
        [Required]
        public int BuyerId { get; set; }

        [Required]
        public int ListingId { get; set; }

        // Optional for purchases made via offer (Second-hand)
        public int? OfferId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        // Navigation properties
        [ForeignKey("BuyerId")]
        public virtual User Buyer { get; set; }

        [ForeignKey("ListingId")]
        public virtual Listing Listing { get; set; }

        [ForeignKey("OfferId")]
        public virtual Offer Offer { get; set; }

        // Link to purchase confirmation details
        public virtual PurchaseConfirmation Confirmation { get; set; }
    }
}