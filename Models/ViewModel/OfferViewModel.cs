using PrimeMarket.Models.Enum;
using System;

namespace PrimeMarket.Models.ViewModel
{
    public class OfferViewModel
    {
        public int OfferId { get; set; }
        public int ListingId { get; set; }
        public string ListingTitle { get; set; }
        public string ListingImage { get; set; }
        public string SellerName { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal OfferAmount { get; set; }
        public OfferStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Message { get; internal set; }
        public string? OfferAmountRaw { get; internal set; }
    }
}