// Models/ViewModel/PaymentViewModel.cs
namespace PrimeMarket.Models.ViewModel
{
    public class PaymentViewModel
    {
        public int ListingId { get; set; }
        public string CardholderName { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string Cvv { get; set; }
        public string ShippingAddress { get; set; }
        public bool SavePaymentDetails { get; set; }
        public int Quantity { get; set; } = 1; // Default to 1
    }
}