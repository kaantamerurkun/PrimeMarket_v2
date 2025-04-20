namespace PrimeMarket.Models.ViewModel
{
    public class ListingDetailsViewModel
    {
        public Listing Listing { get; set; }
        public dynamic Product { get; set; }
        public bool IsBookmarked { get; set; }
        public List<Listing> RelatedListings { get; set; }
        public bool IsOwner { get; set; }
    }
}
