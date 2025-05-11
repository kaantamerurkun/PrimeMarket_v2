using PrimeMarket.Models.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrimeMarket.Models
{
    public class VerificationDocument : BaseEntity
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string FrontImagePath { get; set; }

        [Required]
        public string BackImagePath { get; set; }

        [Required]
        public string FaceImagePath { get; set; }  // New field for face photo

        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        public string RejectionReason { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}