using System;
namespace PrimeMarket.Models.ViewModel
{
    public class OfferPaymentViewModel
    {
        public int OfferId { get; set; }
        public int ListingId { get; set; }
        public string ListingTitle { get; set; }
        public string ListingImage { get; set; }
        public string SellerName { get; set; }
        public decimal OfferAmount { get; set; }

        public string CardholderName { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string Cvv { get; set; }
        public string ShippingAddress { get; set; }
        public bool SavePaymentDetails { get; set; }
    }
}