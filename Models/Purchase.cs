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

        public int? OfferId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public int Quantity { get; set; } = 1;

        [Required]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public string CardholderName { get; set; }

        public string LastFourDigits { get; set; }

        public string ShippingAddress { get; set; }

        [ForeignKey("BuyerId")]
        public virtual User Buyer { get; set; }

        [ForeignKey("ListingId")]
        public virtual Listing Listing { get; set; }

        [ForeignKey("OfferId")]
        public virtual Offer Offer { get; set; }

        public virtual PurchaseConfirmation Confirmation { get; set; }
    }
}