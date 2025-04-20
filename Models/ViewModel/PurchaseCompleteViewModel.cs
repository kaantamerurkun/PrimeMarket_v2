namespace PrimeMarket.Models.ViewModel
{
    public class PurchaseCompleteViewModel
    {
        public int PurchaseId { get; set; }
        public string ListingTitle { get; set; }
        public string ListingImage { get; set; }
        public decimal Amount { get; set; }
        public string SellerName { get; set; }
        public DateTime PurchaseDate { get; set; }
    }
}
