namespace PrimeMarket.Models.ViewModel
{
    public class CartItemViewModel
    {
        public int BookmarkId { get; set; }
        public int ListingId { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string SellerName { get; set; }
        public string ImageUrl { get; set; }
        public int? MaxStock { get; set; } // Added property to store the maximum available stock
    }
}