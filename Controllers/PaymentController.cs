using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Filters;
using PrimeMarket.Models.Enum;
using PrimeMarket.Models.ViewModel;
using PrimeMarket.Models;

[HttpPost]
[UserAuthenticationFilter]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
{
    var userId = HttpContext.Session.GetInt32("UserId");
    if (userId == null)
    {
        return RedirectToAction("Login", "User");
    }

    if (!ModelState.IsValid)
    {
        TempData["ErrorMessage"] = "Please fill out all required payment fields.";
        return RedirectToAction("Checkout", new { listingId = model.ListingId });
    }

    try
    {
        // Get the listing
        var listing = await _context.Listings
            .Include(l => l.Seller)
            .FirstOrDefaultAsync(l => l.Id == model.ListingId && l.Status == ListingStatus.Approved);

        if (listing == null)
        {
            TempData["ErrorMessage"] = "This listing is no longer available.";
            return RedirectToAction("Checkout", new { listingId = model.ListingId });
        }

        // Check if this is a first-hand listing (only first-hand can be bought directly)
        if (listing.Condition != "First-Hand")
        {
            TempData["ErrorMessage"] = "Second-hand listings can only be purchased through an offer.";
            return RedirectToAction("Details", "Listing", new { id = model.ListingId });
        }

        // Check if there's enough stock
        if (!listing.Stock.HasValue || listing.Stock.Value <= 0)
        {
            TempData["ErrorMessage"] = "This item is out of stock.";
            return RedirectToAction("Details", "Listing", new { id = model.ListingId });
        }

        // Create purchase record
        var purchase = new Purchase
        {
            BuyerId = userId.Value,
            ListingId = listing.Id,
            Amount = listing.Price,
            PaymentStatus = PaymentStatus.Authorized, // Payment is authorized but held in escrow
            CreatedAt = DateTime.UtcNow
        };

        _context.Purchases.Add(purchase);

        // Create purchase confirmation record for tracking shipping/delivery
        var confirmation = new PurchaseConfirmation
        {
            PurchaseId = purchase.Id,
            SellerShippedProduct = false,
            BuyerReceivedProduct = false,
            PaymentReleased = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.PurchaseConfirmations.Add(confirmation);

        // Reduce stock of first-hand listing
        listing.Stock -= 1;

        // If stock reaches 0, mark as sold out
        if (listing.Stock <= 0)
        {
            listing.Status = ListingStatus.Sold;
        }

        listing.UpdatedAt = DateTime.UtcNow;

        // Create notifications for buyer and seller
        var buyerNotification = new Notification
        {
            UserId = userId.Value,
            Message = $"You have successfully purchased '{listing.Title}'. Please wait for the seller to ship the item.",
            Type = NotificationType.PurchaseCompleted,
            RelatedEntityId = purchase.Id,
            CreatedAt = DateTime.UtcNow
        };

        var sellerNotification = new Notification
        {
            UserId = listing.SellerId,
            Message = $"Your listing '{listing.Title}' has been purchased. Please ship the item and confirm shipping.",
            Type = NotificationType.PurchaseCompleted,
            RelatedEntityId = purchase.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(buyerNotification);
        _context.Notifications.Add(sellerNotification);

        await _context.SaveChangesAsync();

        return RedirectToAction("PurchaseComplete", new { purchaseId = purchase.Id });
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = "An error occurred while processing your payment: " + ex.Message;
        return RedirectToAction("Checkout", new { listingId = model.ListingId });
    }
}

// Process offer purchase for second-hand listings
[HttpPost]
[UserAuthenticationFilter]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ProcessOfferPurchase(int offerId)
{
    var userId = HttpContext.Session.GetInt32("UserId");
    if (userId == null)
    {
        return RedirectToAction("Login", "User");
    }

    try
    {
        // Get the offer with listing
        var offer = await _context.Offers
            .Include(o => o.Listing)
            .ThenInclude(l => l.Seller)
            .FirstOrDefaultAsync(o => o.Id == offerId && o.BuyerId == userId && o.Status == OfferStatus.Accepted);

        if (offer == null)
        {
            TempData["ErrorMessage"] = "Offer not found or not accepted.";
            return RedirectToAction("MyOffers", "User");
        }

        // Check if listing is still available
        if (offer.Listing.Status != ListingStatus.Approved)
        {
            TempData["ErrorMessage"] = "This listing is no longer available.";
            return RedirectToAction("MyOffers", "User");
        }

        // Check that this is a second-hand listing
        if (offer.Listing.Condition != "Second-Hand")
        {
            TempData["ErrorMessage"] = "Invalid operation. This offer is not for a second-hand listing.";
            return RedirectToAction("Details", "Listing", new { id = offer.ListingId });
        }

        // Create purchase record
        var purchase = new Purchase
        {
            BuyerId = userId.Value,
            ListingId = offer.ListingId,
            OfferId = offer.Id,
            Amount = offer.OfferAmount, // Use the offer amount, not listing price
            PaymentStatus = PaymentStatus.Authorized, // Payment is authorized but held in escrow
            CreatedAt = DateTime.UtcNow
        };

        _context.Purchases.Add(purchase);

        // Create purchase confirmation record for tracking shipping/delivery
        var confirmation = new PurchaseConfirmation
        {
            PurchaseId = purchase.Id,
            SellerShippedProduct = false,
            BuyerReceivedProduct = false,
            PaymentReleased = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.PurchaseConfirmations.Add(confirmation);

        // Update offer status
        offer.Status = OfferStatus.Purchased;
        offer.UpdatedAt = DateTime.UtcNow;

        // Update listing status
        offer.Listing.Status = ListingStatus.Sold;
        offer.Listing.UpdatedAt = DateTime.UtcNow;

        // Create notifications for buyer and seller
        var buyerNotification = new Notification
        {
            UserId = userId.Value,
            Message = $"You have successfully purchased '{offer.Listing.Title}' via your offer. Please wait for the seller to ship the item.",
            Type = NotificationType.PurchaseCompleted,
            RelatedEntityId = purchase.Id,
            CreatedAt = DateTime.UtcNow
        };

        var sellerNotification = new Notification
        {
            UserId = offer.Listing.SellerId,
            Message = $"Your listing '{offer.Listing.Title}' has been purchased through an offer. Please ship the item and confirm shipping.",
            Type = NotificationType.PurchaseCompleted,
            RelatedEntityId = purchase.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(buyerNotification);
        _context.Notifications.Add(sellerNotification);

        await _context.SaveChangesAsync();

        return RedirectToAction("PurchaseComplete", new { purchaseId = purchase.Id });
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = "An error occurred while processing your payment: " + ex.Message;
        return RedirectToAction("MyOffers", "User");
    }
}

// New action methods for handling purchase confirmation

[HttpGet]
[UserAuthenticationFilter]
public async Task<IActionResult> ConfirmShipment(int purchaseId)
{
    var userId = HttpContext.Session.GetInt32("UserId");
    if (userId == null)
    {
        return RedirectToAction("Login", "User");
    }

    var purchase = await _context.Purchases
        .Include(p => p.Listing)
        .Include(p => p.Confirmation)
        .FirstOrDefaultAsync(p => p.Id == purchaseId);

    if (purchase == null)
    {
        return NotFound();
    }

    // Check if user is the seller of this listing
    if (purchase.Listing.SellerId != userId)
    {
        return Forbid();
    }

    // Validate purchase status
    if (purchase.PaymentStatus != PaymentStatus.Authorized)
    {
        TempData["ErrorMessage"] = "Invalid purchase status for shipment confirmation.";
        return RedirectToAction("MySales");
    }

    // Check if already shipped
    if (purchase.Confirmation.SellerShippedProduct)
    {
        TempData["ErrorMessage"] = "This order has already been marked as shipped.";
        return RedirectToAction("ShipmentDetails", new { purchaseId });
    }

    return View(new ShipmentConfirmationViewModel
    {
        PurchaseId = purchase.Id,
        ListingId = purchase.ListingId,
        ListingTitle = purchase.Listing.Title,
        BuyerId = purchase.BuyerId,
        TrackingNumber = purchase.Confirmation.TrackingNumber,
        ShippingProvider = purchase.Confirmation.ShippingProvider
    });
}

[HttpPost]
[UserAuthenticationFilter]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ConfirmShipment(ShipmentConfirmationViewModel model)
{
    var userId = HttpContext.Session.GetInt32("UserId");
    if (userId == null)
    {
        return RedirectToAction("Login", "User");
    }

    var purchase = await _context.Purchases
        .Include(p => p.Listing)
        .Include(p => p.Confirmation)
        .Include(p => p.Buyer)