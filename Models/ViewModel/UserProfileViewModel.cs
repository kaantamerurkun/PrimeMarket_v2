﻿using System.Collections.Generic;

namespace PrimeMarket.Models.ViewModel
{
    public class UserProfileViewModel
    {
        public User User { get; set; }
        public List<Listing> Listings { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
    }
}