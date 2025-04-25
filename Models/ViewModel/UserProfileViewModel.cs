using System.Collections.Generic;

namespace PrimeMarket.Models.ViewModel
{
    public class UserProfileViewModel
    {
        public User User { get; set; }
        public List<Listing> Listings { get; set; }
    }
}