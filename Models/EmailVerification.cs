using System.ComponentModel.DataAnnotations;

namespace PrimeMarket.Models
{
    public class EmailVerification
    {
        public int Id { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MaxLength(6)]
        public string Code { get; set; }

        public DateTime Expiration { get; set; }
    }

}
