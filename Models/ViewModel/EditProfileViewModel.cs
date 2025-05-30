using System.ComponentModel.DataAnnotations;

namespace PrimeMarket.Models.ViewModel
{
    public class EditProfileViewModel
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; }

        [Required, MaxLength(100), EmailAddress]
        public string Email { get; set; }

        [MaxLength(15)]
        public string PhoneNumber { get; set; }

        public string ProfileImagePath { get; set; }

        public string CurrentPassword { get; set; }

        [MinLength(6)]
        public string NewPassword { get; set; }

        public string ConfirmNewPassword { get; set; }
    }
}
