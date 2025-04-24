namespace PrimeMarket.Models.ViewModel
{
    public class OfferViewModel
    {
        public int ListingId { get; set; }
        public decimal OfferAmount { get; set; }          // what the binder managed to parse
        public string? OfferAmountRaw { get; set; }          // as-sent, if you keep it in JS
        public string? Message { get; set; }
    }

}
