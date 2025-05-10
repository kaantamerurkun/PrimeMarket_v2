using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrimeMarket.Models
{
    public class SellerRating : BaseEntity
    {
        [Required]
        public int BuyerId { get; set; }

        [Required]
        public int SellerId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        //public string Comment { get; set; }

        // Reference to the purchase that allows this rating
        [Required]
        public int PurchaseId { get; set; }

        // Navigation properties
        [ForeignKey("BuyerId")]
        public virtual User Buyer { get; set; }

        [ForeignKey("SellerId")]
        public virtual User Seller { get; set; }

        [ForeignKey("PurchaseId")]
        public virtual Purchase Purchase { get; set; }
    }
}