using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace PrimeMarket.Models
{
    public class Admin : BaseEntity
    {
        [Required, MaxLength(50)]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }


        // For tracking actions taken by this admin
        public virtual ICollection<AdminAction> AdminActions { get; set; }

        // Methods for handling password hashing

    }

    // Entity to track admin actions for audit purposes
    public class AdminAction : BaseEntity
    {
        [Required]
        public int AdminId { get; set; }

        [Required, MaxLength(50)]
        public string ActionType { get; set; }

        // The entity type that was affected (e.g., "User", "Listing")
        [Required, MaxLength(50)]
        public string EntityType { get; set; }

        // The ID of the entity that was affected
        public int? EntityId { get; set; }

        // Additional details about the action
        public string ActionDetails { get; set; }

        // Navigation property
        [ForeignKey("AdminId")]
        public virtual Admin Admin { get; set; }
    }
}