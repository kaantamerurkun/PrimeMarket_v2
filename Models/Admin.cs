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


        public virtual ICollection<AdminAction> AdminActions { get; set; }


    }

    public class AdminAction : BaseEntity
    {
        [Required]
        public int AdminId { get; set; }

        [Required, MaxLength(50)]
        public string ActionType { get; set; }

        [Required, MaxLength(50)]
        public string EntityType { get; set; }

        public int? EntityId { get; set; }

        public string ActionDetails { get; set; }

        [ForeignKey("AdminId")]
        public virtual Admin Admin { get; set; }
    }
}