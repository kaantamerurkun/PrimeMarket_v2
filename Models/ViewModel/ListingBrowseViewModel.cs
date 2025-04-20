namespace PrimeMarket.Models.ViewModel
{
    public class ListingBrowseViewModel
    {
        public List<Listing> Listings { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string SearchTerm { get; set; }
    }
}
