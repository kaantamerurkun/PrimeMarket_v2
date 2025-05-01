namespace PrimeMarket.Models.ViewModel
{
    public class ListingDetailsViewModel
    {
        public Listing Listing { get; set; }
        public dynamic Product { get; set; }
        public bool IsBookmarked { get; set; }
        public List<Listing> RelatedListings { get; set; }
        public bool IsOwner { get; set; }

        // Flag to identify if offers are allowed (second-hand)
        public bool AllowOffers => Listing?.Condition == "Second-Hand";

        // Flag to identify if direct purchase is allowed (first-hand)
        public bool AllowBuyNow => Listing?.Condition == "First-Hand";

        // Available stock for first-hand listings
        public int? AvailableStock => Listing?.Stock;

        // Current active offers (for second-hand)
        public List<Offer> ActiveOffers { get; set; }
    }
}