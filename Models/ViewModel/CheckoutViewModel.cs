using PrimeMarket.Models.Enum;
namespace PrimeMarket.Models.ViewModel
{
    public class CheckoutViewModel
    {
        public int ListingId { get; set; }
        public string ListingTitle { get; set; }
        public decimal ListingPrice { get; set; }
        public string SellerName { get; set; }
        public string ListingImage { get; set; }
        public int Quantity { get; set; } = 1; // Default to 1
        public decimal TotalPrice { get; set; }
    }
}
