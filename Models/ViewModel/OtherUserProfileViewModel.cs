using System.Collections.Generic;

namespace PrimeMarket.Models.ViewModel
{
    public class OtherUserProfileViewModel
    {
        public User User { get; set; }
        public List<Listing> Listings { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public bool CanRateUser { get; set; }
        public int UserRating { get; set; }
    }
}