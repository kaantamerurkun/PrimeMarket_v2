using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrimeMarket.Models.Enum
{
    public enum NotificationType
    {
        ListingApproved,
        ListingRejected,
        NewMessage,
        NewOffer,
        OfferAccepted,
        OfferRejected,
        VerificationApproved,
        VerificationRejected,
        PurchaseCompleted,
        OfferCancelled,
        ListingUpdated
    }
}