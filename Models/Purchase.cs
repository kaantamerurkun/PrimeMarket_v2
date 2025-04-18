using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using PrimeMarket.Models.Enum;

namespace PrimeMarket.Models.Products
{
    public class Purchase : BaseEntity
    {
        [Required]
        public int BuyerId { get; set; }

        [Required]
        public int ListingId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        // Navigation properties
        [ForeignKey("BuyerId")]
        public virtual User Buyer { get; set; }

        [ForeignKey("ListingId")]
        public virtual Listing Listing { get; set; }
    }
}