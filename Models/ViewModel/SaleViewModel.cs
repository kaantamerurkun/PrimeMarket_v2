using PrimeMarket.Models.Enum;

namespace PrimeMarket.Models.ViewModel
{
    public class SaleViewModel
    {
        public int PurchaseId { get; set; }
        public int ListingId { get; set; }
        public string ListingTitle { get; set; }
        public string ListingImage { get; set; }
        public string BuyerName { get; set; }
        public decimal Amount { get; set; }
        public DateTime SaleDate { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public int Quantity { get; set; } = 1; // Default to 1

        // Helper property to calculate unit price
        public decimal UnitPrice => Quantity > 0 ? Amount / Quantity : Amount;
    }
}
