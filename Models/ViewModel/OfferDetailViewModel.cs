using PrimeMarket.Models.Enum;

namespace PrimeMarket.Models.ViewModel
{
    public class OfferDetailViewModel
    {
        public int OfferId { get; set; }
        public int ListingId { get; set; }
        public string ListingTitle { get; set; }
        public string ListingImage { get; set; }
        public decimal ListingPrice { get; set; }
        public decimal OfferAmount { get; set; }
        public string OfferMessage { get; set; }
        public OfferStatus Status { get; set; }
        public DateTime OfferDate { get; set; }
        public DateTime? ResponseDate { get; set; }

        // User details
        public int BuyerId { get; set; }
        public string BuyerName { get; set; }
        public int SellerId { get; set; }
        public string SellerName { get; set; }

        // Counter offer details
        public bool IsCounterOffer { get; set; }
        public int? OriginalOfferId { get; set; }
        public decimal? OriginalOfferAmount { get; set; }

        // Counter offer collections
        public List<OfferDetailViewModel> CounterOffers { get; set; }

        // For checking user permissions
        public bool IsViewerSeller { get; set; }
        public bool IsViewerBuyer { get; set; }

        // Actions available
        public bool CanAccept => Status == OfferStatus.Pending && IsViewerSeller;
        public bool CanReject => Status == OfferStatus.Pending && IsViewerSeller;
        public bool CanCounter => Status == OfferStatus.Pending && IsViewerSeller;
        public bool CanCancel => Status == OfferStatus.Pending && IsViewerBuyer;
        public bool CanPurchase => Status == OfferStatus.Accepted && IsViewerBuyer;
    }
}