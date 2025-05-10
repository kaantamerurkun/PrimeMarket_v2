using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrimeMarket.Models
{
    public class ProductReview : BaseEntity
    {
        [Required]
        public int ListingId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Comment { get; set; }

        // To ensure only one review per user per product
        public bool IsVerifiedPurchase { get; set; } = true;

        // Navigation properties
        [ForeignKey("ListingId")]
        public virtual Listing Listing { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}