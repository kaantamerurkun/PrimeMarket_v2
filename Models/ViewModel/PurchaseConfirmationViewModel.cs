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
        public bool SellerShippedProduct { get; set; }
        public DateTime? ShippingConfirmedDate { get; set; }
        public bool BuyerReceivedProduct { get; set; }
        public DateTime? ReceiptConfirmedDate { get; set; }
        public bool PaymentReleased { get; set; }
        public DateTime? PaymentReleasedDate { get; set; }
        public string TrackingNumber { get; set; }
        public string ShippingProvider { get; set; }
        public bool IsViewerSeller { get; set; }
        public bool IsViewerBuyer { get; set; }
        public bool IsSecondHandPurchase { get; set; }
        public decimal OfferAmount { get; set; }
        public int Quantity { get; set; } = 1; // Default to 1

        // Helper property to calculate unit price

        

        // Helper property to calculate unit price
        public decimal UnitPrice => Quantity > 0 ? Amount / Quantity : Amount;
    }
}