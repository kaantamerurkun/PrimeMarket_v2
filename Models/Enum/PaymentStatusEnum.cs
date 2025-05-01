using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrimeMarket.Models.Enum
{
    public enum PaymentStatus
    {
        Pending,     // Initial state
        Authorized,  // Payment authorized but held in escrow
        Completed,   // Payment fully processed and released to seller
        Failed,      // Payment processing failed
        Refunded     // Payment was refunded to buyer
    }
}