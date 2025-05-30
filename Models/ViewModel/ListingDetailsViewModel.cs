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

        public bool AllowOffers => Listing?.Condition == "Second-Hand" && Listing?.Status != ListingStatus.Archived;

        public bool AllowBuyNow => Listing?.Condition == "First-Hand" && Listing?.Status != ListingStatus.Archived;

        public int? AvailableStock => Listing?.Stock;

        public List<Offer> ActiveOffers { get; set; }

        public bool IsArchived => Listing?.Status == ListingStatus.Archived;
    }
}