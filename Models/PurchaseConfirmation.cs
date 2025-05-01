using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PrimeMarket.Models.Enum;
using PrimeMarket.Models.Products;

namespace PrimeMarket.Models
{
    public class PurchaseConfirmation : BaseEntity
    {
        [Required]
        public int PurchaseId { get; set; }

        // Is product shipped by seller
        public bool SellerShippedProduct { get; set; } = false;

        // When seller confirmed shipping
        public DateTime? ShippingConfirmedDate { get; set; }

        // Is product received by buyer
        public bool BuyerReceivedProduct { get; set; } = false;

        // When buyer confirmed receiving
        public DateTime? ReceiptConfirmedDate { get; set; }

        // Is payment released to seller
        public bool PaymentReleased { get; set; } = false;

        // When payment was released
        public DateTime? PaymentReleasedDate { get; set; }

        // Optional tracking information
        public string? TrackingNumber { get; set; }

        // Optional shipping carrier
        public string? ShippingProvider { get; set; }

        // Navigation property
        [ForeignKey("PurchaseId")]
        public virtual Purchase Purchase { get; set; }
    }
}