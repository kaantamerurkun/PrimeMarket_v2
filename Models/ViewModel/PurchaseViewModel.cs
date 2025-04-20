using PrimeMarket.Models.Enum;

namespace PrimeMarket.Models.ViewModel
{
    public class PurchaseViewModel
    {
        public int PurchaseId { get; set; }
        public int ListingId { get; set; }
        public string ListingTitle { get; set; }
        public string ListingImage { get; set; }
        public string SellerName { get; set; }
        public decimal Amount { get; set; }
        public DateTime PurchaseDate { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
    }
}
