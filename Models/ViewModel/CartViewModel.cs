using PrimeMarket.Controllers;

namespace PrimeMarket.Models.ViewModel
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
