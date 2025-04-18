using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrimeMarket.Models.Enum
{
    public enum VerificationStatus
    {
        Pending,
        Approved,
        Rejected
    }
}