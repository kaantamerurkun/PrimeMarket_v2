namespace PrimeMarket.Models.ViewModel
{
    public class MultipleCheckoutViewModel
    {
        public List<CartItemViewModel> Items { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
