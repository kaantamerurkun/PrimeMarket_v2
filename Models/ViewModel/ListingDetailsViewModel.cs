using PrimeMarket.Models.Enum;

namespace PrimeMarket.Models.ViewModel
{
    public class ListingDetailsViewModel
    {
        public Listing Listing { get; set; }
        public dynamic Product { get; set; }
        public bool IsBookmarked { get; set; }
        public List<Listing> RelatedListings { get; set; }
        public bool IsOwner { get; set; }
        public List<ProductReview> Reviews { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public bool CanReview { get; set; }
        public bool HasReviewed { get; set; }

        // Flag to identify if offers are allowed (second-hand and not archived)
        public bool AllowOffers => Listing?.Condition == "Second-Hand" && Listing?.Status != ListingStatus.Archived;

        // Flag to identify if direct purchase is allowed (first-hand and not archived)
        public bool AllowBuyNow => Listing?.Condition == "First-Hand" && Listing?.Status != ListingStatus.Archived;

        // Available stock for first-hand listings
        public int? AvailableStock => Listing?.Stock;

        // Current active offers (for second-hand)
        public List<Offer> ActiveOffers { get; set; }

        // Flag to check if listing is archived
        public bool IsArchived => Listing?.Status == ListingStatus.Archived;
    }
}