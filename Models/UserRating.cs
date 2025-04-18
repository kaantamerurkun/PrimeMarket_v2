using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace PrimeMarket.Models
{
    public class UserRating : BaseEntity
    {
        [Required]
        public int RaterId { get; set; }

        [Required]
        public int RatedUserId { get; set; }

        [Required, Range(1, 5)]
        public int Rating { get; set; }

        public string Comment { get; set; }

        // Navigation properties
        [ForeignKey("RaterId")]
        public virtual User Rater { get; set; }

        [ForeignKey("RatedUserId")]
        public virtual User RatedUser { get; set; }
    }
}