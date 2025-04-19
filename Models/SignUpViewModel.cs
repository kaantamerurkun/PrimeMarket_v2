using System.ComponentModel.DataAnnotations;

namespace PrimeMarket.Models
{
    public class SignUpViewModel
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; }

        [Required, MaxLength(100), EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public bool AgreeToTerms { get; set; }
    }
}
