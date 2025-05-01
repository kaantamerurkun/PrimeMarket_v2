using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrimeMarket.Models.Enum
{
    public enum OfferStatus
    {
        Pending,       // Initial state when offer is made
        Accepted,      // Offer accepted by seller
        Rejected,      // Offer rejected by seller
        Countered,     // Seller made a counter offer
        Purchased,     // Offer was accepted and purchased
        Cancelled,     // Buyer cancelled the offer
        Expired        // Offer expired without response
    }
}