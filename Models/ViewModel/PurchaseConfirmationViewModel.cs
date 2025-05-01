using PrimeMarket.Models.Enum;

namespace PrimeMarket.Models.ViewModel
{
    public class PurchaseConfirmationViewModel
    {
        public int PurchaseId { get; set; }
        public int ListingId { get; set; }
        public string ListingTitle { get; set; }
        public string ListingImage { get; set; }
        public decimal Amount { get; set; }
        public string SellerName { get; set; }
        public string BuyerName { get; set; }
        public DateTime PurchaseDate { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public bool IsFirstHand { get; set; }

        // Purchase flow tracking
        public bool SellerShippedProduct { get; set; }
        public DateTime? ShippingConfirmedDate { get; set; }
        public bool BuyerReceivedProduct { get; set; }
        public DateTime? ReceiptConfirmedDate { get; set; }
        public bool PaymentReleased { get; set; }
        public DateTime? PaymentReleasedDate { get; set; }

        // For the seller to provide
        public string TrackingNumber { get; set; }
        public string ShippingProvider { get; set; }

        // Who is viewing this confirmation page
        public bool IsViewerSeller { get; set; }
        public bool IsViewerBuyer { get; set; }
    }
}