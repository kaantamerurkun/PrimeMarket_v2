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

        public bool SellerShippedProduct { get; set; } = false;

        public DateTime? ShippingConfirmedDate { get; set; }

        public bool BuyerReceivedProduct { get; set; } = false;

        public DateTime? ReceiptConfirmedDate { get; set; }

        public bool PaymentReleased { get; set; } = false;

        public DateTime? PaymentReleasedDate { get; set; }

        public string? TrackingNumber { get; set; }

        public string? ShippingProvider { get; set; }

        [ForeignKey("PurchaseId")]
        public virtual Purchase Purchase { get; set; }
    }
}