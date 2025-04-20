using System.ComponentModel.DataAnnotations;

namespace PrimeMarket.Models.ViewModel
{
    public class AdminLoginModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
