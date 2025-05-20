namespace PrimeMarket.Models.ViewModel
{
    public class MultiplePaymentViewModel
    {
        public string CardholderName { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string Cvv { get; set; }
        public string ShippingAddress { get; set; }
        public bool SavePaymentDetails { get; set; }
    }
}