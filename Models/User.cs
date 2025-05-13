using PrimeMarket.Models.Products;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrimeMarket.Models
{
    public class User : BaseEntity
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; }

        [Required, MaxLength(100), EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [MaxLength(15)]
        public string? PhoneNumber { get; set; }

        public string? ProfileImagePath { get; set; }

        [Required]
        public bool IsAdmin { get; set; } = false;

        [Required]
        public bool IsEmailVerified { get; set; } = false;

        public bool IsIdVerified { get; set; } = false;

        // Navigation properties
        public virtual ICollection<Listing> Listings { get; set; }
        public virtual ICollection<Message> SentMessages { get; set; }
        public virtual ICollection<Message> ReceivedMessages { get; set; }
        public virtual ICollection<Bookmark> Bookmarks { get; set; }
        public virtual ICollection<Purchase> Purchases { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<UserRating> RatingsGiven { get; set; }
        public virtual ICollection<UserRating> RatingsReceived { get; set; }
        public virtual VerificationDocument VerificationDocument { get; set; }
        public virtual ICollection<ProductReview> ProductReviews { get; set; }
        // Add these navigation properties to your User model class:

        // Ratings given by this user as a buyer
        public virtual ICollection<SellerRating> RatingsGivenAsBuyer { get; set; }

        // Ratings received by this user as a seller
        public virtual ICollection<SellerRating> RatingsReceivedAsSeller { get; set; }
        // In your User class
        public virtual ICollection<Offer> Offers { get; set; }
    }
}