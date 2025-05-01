using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrimeMarket.Data;
using PrimeMarket.Filters;
using PrimeMarket.Models;
using PrimeMarket.Models.Enum;
using PrimeMarket.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrimeMarket.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(ApplicationDbContext context, ILogger<PaymentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> Cart()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            try
            {
                // Get all bookmarks (items in cart) for the current user
                var bookmarks = await _context.Bookmarks
                    .Include(b => b.Listing)
                    .ThenInclude(l => l.Images)
                    .Include(b => b.Listing.Seller)
                    .Where(b => b.UserId == userId && b.Listing.Status == ListingStatus.Approved)
                    .ToListAsync();

                var model = new CartViewModel
                {
                    Items = bookmarks.Select(b => new CartItemViewModel
                    {
                        BookmarkId = b.Id,
                        ListingId = b.ListingId,
                        Title = b.Listing.Title,
                        Price = b.Listing.Price,
                        SellerName = $"{b.Listing.Seller.FirstName} {b.Listing.Seller.LastName}",
                        ImageUrl = b.Listing.Images?.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                                  b.Listing.Images?.FirstOrDefault()?.ImagePath ??
                                  "/images/placeholder.png"
                    }).ToList(),
                    TotalPrice = bookmarks.Sum(b => b.Listing.Price)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart items");
                TempData["ErrorMessage"] = "An error occurred while loading your cart.";
                return View(new CartViewModel { Items = new List<CartItemViewModel>() });
            }
        }

        [HttpPost]
        [UserAuthenticationFilter]
        public async Task<IActionResult> AddToCart(int listingId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Please log in to add items to your cart." });
            }

            try
            {
                // Check if the listing exists and is available
                var listing = await _context.Listings
                    .FirstOrDefaultAsync(l => l.Id == listingId && l.Status == ListingStatus.Approved);

                if (listing == null)
                {
                    return Json(new { success = false, message = "This listing is not available." });
                }

                // Check if listing is first-hand (only first-hand items can be added to cart)
                if (listing.Condition != "First-Hand")
                {
                    return Json(new { success = false, message = "Only first-hand items can be added to cart. You can make an offer for second-hand items." });
                }

                // Check if the listing has stock
                if (listing.Stock == null || listing.Stock <= 0)
                {
                    return Json(new { success = false, message = "This item is out of stock." });
                }

                // Check if this listing is already in the user's bookmarks/cart
                var existingBookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.ListingId == listingId);

                if (existingBookmark != null)
                {
                    return Json(new { success = false, message = "This item is already in your cart." });
                }

                // Add the listing to bookmarks (cart)
                var bookmark = new Bookmark
                {
                    UserId = userId.Value,
                    ListingId = listingId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Bookmarks.Add(bookmark);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Item added to cart successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart");
                return Json(new { success = false, message = "An error occurred while adding the item to your cart." });
            }
        }

        [HttpPost]
        [UserAuthenticationFilter]
        public async Task<IActionResult> RemoveFromCart(int bookmarkId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Please log in to manage your cart." });
            }

            try
            {
                // Find the bookmark and check it belongs to the user
                var bookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.Id == bookmarkId && b.UserId == userId);

                if (bookmark == null)
                {
                    return Json(new { success = false, message = "Item not found in your cart." });
                }

                _context.Bookmarks.Remove(bookmark);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Item removed from cart." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                return Json(new { success = false, message = "An error occurred while removing the item from your cart." });
            }
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> Checkout(int listingId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Get the listing
            var listing = await _context.Listings
                .Include(l => l.Seller)
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.Id == listingId && l.Status == ListingStatus.Approved);

            if (listing == null)
            {
                TempData["ErrorMessage"] = "Listing not found or not available for purchase.";
                return RedirectToAction("Cart");
            }

            // Check if it's a first-hand listing (only first-hand can be bought directly)
            if (listing.Condition != "First-Hand")
            {
                TempData["ErrorMessage"] = "Second-hand listings can only be purchased through an offer.";
                return RedirectToAction("Details", "Listing", new { id = listingId });
            }

            // Check if there's stock available
            if (!listing.Stock.HasValue || listing.Stock <= 0)
            {
                TempData["ErrorMessage"] = "This item is out of stock.";
                return RedirectToAction("Details", "Listing", new { id = listingId });
            }

            // Prepare checkout model
            var model = new CheckoutViewModel
            {
                ListingId = listing.Id,
                ListingTitle = listing.Title,
                ListingPrice = listing.Price,
                SellerName = $"{listing.Seller.FirstName} {listing.Seller.LastName}",
                ListingImage = listing.Images?.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              listing.Images?.FirstOrDefault()?.ImagePath ??
                              "/images/placeholder.png"
            };

            return View(model);
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> CheckoutMultiple()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Get all items in cart
            var bookmarks = await _context.Bookmarks
                .Include(b => b.Listing)
                .ThenInclude(l => l.Images)
                .Include(b => b.Listing.Seller)
                .Where(b => b.UserId == userId && b.Listing.Status == ListingStatus.Approved)
                .ToListAsync();

            if (bookmarks == null || bookmarks.Count == 0)
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("Cart");
            }

            // Check all items for stock availability and first-hand condition
            foreach (var bookmark in bookmarks)
            {
                if (bookmark.Listing.Condition != "First-Hand")
                {
                    TempData["ErrorMessage"] = "Your cart contains second-hand items that can only be purchased through an offer.";
                    return RedirectToAction("Cart");
                }

                if (!bookmark.Listing.Stock.HasValue || bookmark.Listing.Stock <= 0)
                {
                    TempData["ErrorMessage"] = $"'{bookmark.Listing.Title}' is out of stock.";
                    return RedirectToAction("Cart");
                }
            }

            // Prepare checkout model
            var model = new MultipleCheckoutViewModel
            {
                Items = bookmarks.Select(b => new CartItemViewModel
                {
                    BookmarkId = b.Id,
                    ListingId = b.ListingId,
                    Title = b.Listing.Title,
                    Price = b.Listing.Price,
                    SellerName = $"{b.Listing.Seller.FirstName} {b.Listing.Seller.LastName}",
                    ImageUrl = b.Listing.Images?.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              b.Listing.Images?.FirstOrDefault()?.ImagePath ??
                              "/images/placeholder.png"
                }).ToList(),
                TotalPrice = bookmarks.Sum(b => b.Listing.Price)
            };

            return View(model);
        }

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
                if (!listing.Stock.HasValue || listing.Stock <= 0)
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
                _logger.LogError(ex, "Error processing payment");
                TempData["ErrorMessage"] = "An error occurred while processing your payment: " + ex.Message;
                return RedirectToAction("Checkout", new { listingId = model.ListingId });
            }
        }

        [HttpPost]
        [UserAuthenticationFilter]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessMultiplePayment(MultiplePaymentViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill out all required payment fields.";
                return RedirectToAction("CheckoutMultiple");
            }

            try
            {
                // Get all items in cart
                var bookmarks = await _context.Bookmarks
                    .Include(b => b.Listing)
                    .ThenInclude(l => l.Seller)
                    .Where(b => b.UserId == userId && b.Listing.Status == ListingStatus.Approved)
                    .ToListAsync();

                if (bookmarks == null || bookmarks.Count == 0)
                {
                    TempData["ErrorMessage"] = "Your cart is empty.";
                    return RedirectToAction("Cart");
                }

                // Process each item as a separate purchase
                foreach (var bookmark in bookmarks)
                {
                    var listing = bookmark.Listing;

                    // Double-check that it's a first-hand listing with available stock
                    if (listing.Condition != "First-Hand" || !listing.Stock.HasValue || listing.Stock <= 0)
                    {
                        continue; // Skip this item
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

                    // Create purchase confirmation record
                    var confirmation = new PurchaseConfirmation
                    {
                        PurchaseId = purchase.Id,
                        SellerShippedProduct = false,
                        BuyerReceivedProduct = false,
                        PaymentReleased = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PurchaseConfirmations.Add(confirmation);

                    // Reduce stock
                    listing.Stock -= 1;

                    // If stock reaches 0, mark as sold out
                    if (listing.Stock <= 0)
                    {
                        listing.Status = ListingStatus.Sold;
                    }

                    listing.UpdatedAt = DateTime.UtcNow;

                    // Create notifications
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

                    // Remove from bookmarks/cart
                    _context.Bookmarks.Remove(bookmark);
                }

                await _context.SaveChangesAsync();

                return RedirectToAction("MyPurchases");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing multiple payments");
                TempData["ErrorMessage"] = "An error occurred while processing your payment: " + ex.Message;
                return RedirectToAction("CheckoutMultiple");
            }
        }


        // Process offer purchase for second-hand listings
        [HttpPost]
        [HttpGet]
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
                var model = new OfferPaymentViewModel
                {
                    OfferId = offer.Id,
                    ListingId = offer.ListingId,
                    ListingTitle = offer.Listing.Title,
                    SellerName = $"{offer.Listing.Seller.FirstName} {offer.Listing.Seller.LastName}",
                    OfferAmount = offer.OfferAmount
                };
                return View(model);

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

                // Create a message in the conversation about the purchase
                var purchaseMessage = new Message
                {
                    SenderId = userId.Value,
                    ReceiverId = offer.Listing.SellerId,
                    ListingId = offer.ListingId,
                    Content = $"I have completed the purchase of this item for {offer.OfferAmount:C}.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Messages.Add(purchaseMessage);

                await _context.SaveChangesAsync();

                return RedirectToAction("PurchaseComplete", new { purchaseId = purchase.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing offer purchase");
                TempData["ErrorMessage"] = "An error occurred while processing your payment: " + ex.Message;
                return RedirectToAction("MyOffers", "User");
            }
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> PurchaseComplete(int purchaseId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Get the purchase
            var purchase = await _context.Purchases
                .Include(p => p.Listing)
                .ThenInclude(l => l.Images)
                .Include(p => p.Listing.Seller)
                .FirstOrDefaultAsync(p => p.Id == purchaseId && p.BuyerId == userId);

            if (purchase == null)
            {
                TempData["ErrorMessage"] = "Purchase not found.";
                return RedirectToAction("MyPurchases");
            }

            var model = new PurchaseCompleteViewModel
            {
                PurchaseId = purchase.Id,
                ListingTitle = purchase.Listing.Title,
                ListingImage = purchase.Listing.Images?.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              purchase.Listing.Images?.FirstOrDefault()?.ImagePath ??
                              "/images/placeholder.png",
                Amount = purchase.Amount,
                SellerName = $"{purchase.Listing.Seller.FirstName} {purchase.Listing.Seller.LastName}",
                PurchaseDate = purchase.CreatedAt ?? DateTime.UtcNow
            };

            return View(model);
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> MyPurchases()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var purchases = await _context.Purchases
                .Include(p => p.Listing)
                .ThenInclude(l => l.Images)
                .Include(p => p.Listing.Seller)
                .Include(p => p.Confirmation)
                .Where(p => p.BuyerId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var model = purchases.Select(p => new PurchaseViewModel
            {
                PurchaseId = p.Id,
                ListingId = p.ListingId,
                ListingTitle = p.Listing.Title,
                ListingImage = p.Listing.Images?.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              p.Listing.Images?.FirstOrDefault()?.ImagePath ??
                              "/images/placeholder.png",
                SellerName = $"{p.Listing.Seller.FirstName} {p.Listing.Seller.LastName}",
                Amount = p.Amount,
                PurchaseDate = p.CreatedAt ?? DateTime.MinValue,
                PaymentStatus = p.PaymentStatus
            }).ToList();

            return View(model);
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> MySales()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var sales = await _context.Purchases
                .Include(p => p.Listing)
                .ThenInclude(l => l.Images)
                .Include(p => p.Buyer)
                .Include(p => p.Confirmation)
                .Where(p => p.Listing.SellerId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var model = sales.Select(p => new SaleViewModel
            {
                PurchaseId = p.Id,
                ListingId = p.ListingId,
                ListingTitle = p.Listing.Title,
                ListingImage = p.Listing.Images?.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              p.Listing.Images?.FirstOrDefault()?.ImagePath ??
                              "/images/placeholder.png",
                BuyerName = $"{p.Buyer.FirstName} {p.Buyer.LastName}",
                Amount = p.Amount,
                SaleDate = p.CreatedAt ?? DateTime.MinValue,
                PaymentStatus = p.PaymentStatus
            }).ToList();

            return View(model);
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> PurchaseStatus(int purchaseId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var purchase = await _context.Purchases
                .Include(p => p.Listing)
                .ThenInclude(l => l.Images)
                .Include(p => p.Listing.Seller)
                .Include(p => p.Buyer)
                .Include(p => p.Confirmation)
                .Include(p => p.Offer) // Include the offer information
                .FirstOrDefaultAsync(p => p.Id == purchaseId);

            if (purchase == null)
            {
                TempData["ErrorMessage"] = "Purchase not found.";
                return RedirectToAction("MyPurchases");
            }

            // Check if the user is either the buyer or the seller
            if (purchase.BuyerId != userId && purchase.Listing.SellerId != userId)
            {
                TempData["ErrorMessage"] = "You don't have permission to view this purchase.";
                return RedirectToAction("MyPurchases");
            }

            var isSecondHand = purchase.Listing.Condition == "Second-Hand";
            var offerAmount = purchase.Offer?.OfferAmount ?? purchase.Amount;

            var model = new PurchaseConfirmationViewModel
            {
                PurchaseId = purchase.Id,
                ListingId = purchase.ListingId,
                ListingTitle = purchase.Listing.Title,
                ListingImage = purchase.Listing.Images?.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              purchase.Listing.Images?.FirstOrDefault()?.ImagePath ??
                              "/images/placeholder.png",
                Amount = purchase.Amount,
                SellerName = $"{purchase.Listing.Seller.FirstName} {purchase.Listing.Seller.LastName}",
                BuyerName = $"{purchase.Buyer.FirstName} {purchase.Buyer.LastName}",
                PurchaseDate = purchase.CreatedAt ?? DateTime.MinValue,
                PaymentStatus = purchase.PaymentStatus,
                IsFirstHand = !isSecondHand,
                SellerShippedProduct = purchase.Confirmation.SellerShippedProduct,
                ShippingConfirmedDate = purchase.Confirmation.ShippingConfirmedDate,
                BuyerReceivedProduct = purchase.Confirmation.BuyerReceivedProduct,
                ReceiptConfirmedDate = purchase.Confirmation.ReceiptConfirmedDate,
                PaymentReleased = purchase.Confirmation.PaymentReleased,
                PaymentReleasedDate = purchase.Confirmation.PaymentReleasedDate,
                TrackingNumber = purchase.Confirmation.TrackingNumber,
                ShippingProvider = purchase.Confirmation.ShippingProvider,
                IsViewerSeller = purchase.Listing.SellerId == userId,
                IsViewerBuyer = purchase.BuyerId == userId,
                IsSecondHandPurchase = isSecondHand,
                OfferAmount = offerAmount
            };

            return View(model);
        }

        [HttpPost]
        [UserAuthenticationFilter]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmShipping(int purchaseId, string trackingNumber, string shippingProvider)
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
                .FirstOrDefaultAsync(p => p.Id == purchaseId);

            if (purchase == null)
            {
                TempData["ErrorMessage"] = "Purchase not found.";
                return RedirectToAction("MySales");
            }

            // Check if the user is the seller
            if (purchase.Listing.SellerId != userId)
            {
                TempData["ErrorMessage"] = "You don't have permission to confirm shipping for this purchase.";
                return RedirectToAction("MySales");
            }

            try
            {
                // Update confirmation
                purchase.Confirmation.SellerShippedProduct = true;
                purchase.Confirmation.ShippingConfirmedDate = DateTime.UtcNow;
                purchase.Confirmation.TrackingNumber = trackingNumber;
                purchase.Confirmation.ShippingProvider = shippingProvider;
                purchase.Confirmation.UpdatedAt = DateTime.UtcNow;

                // Create notification for buyer
                var notification = new Notification
                {
                    UserId = purchase.BuyerId,
                    Message = $"The seller has shipped your purchase '{purchase.Listing.Title}'. Tracking: {trackingNumber}",
                    Type = NotificationType.PurchaseCompleted,
                    RelatedEntityId = purchaseId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);

                // Add a message to the conversation between buyer and seller
                var message = new Message
                {
                    SenderId = userId.Value,
                    ReceiverId = purchase.BuyerId,
                    ListingId = purchase.ListingId,
                    Content = $"I've shipped your item. Tracking Number: {trackingNumber}, Shipping Provider: {shippingProvider}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Messages.Add(message);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Shipping confirmation successful.";
                return RedirectToAction("PurchaseStatus", new { purchaseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming shipping");
                TempData["ErrorMessage"] = "An error occurred while confirming the shipping: " + ex.Message;
                return RedirectToAction("PurchaseStatus", new { purchaseId });
            }
        }

        [HttpPost]
        [UserAuthenticationFilter]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReceived(int purchaseId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var purchase = await _context.Purchases
                .Include(p => p.Listing)
                .Include(p => p.Confirmation)
                .Include(p => p.Listing.Seller)
                .FirstOrDefaultAsync(p => p.Id == purchaseId);

            if (purchase == null)
            {
                TempData["ErrorMessage"] = "Purchase not found.";
                return RedirectToAction("MyPurchases");
            }

            // Check if the user is the buyer
            if (purchase.BuyerId != userId)
            {
                TempData["ErrorMessage"] = "You don't have permission to confirm receipt for this purchase.";
                return RedirectToAction("MyPurchases");
            }

            try
            {
                // Check if seller has confirmed shipping
                if (!purchase.Confirmation.SellerShippedProduct)
                {
                    TempData["ErrorMessage"] = "The seller has not confirmed shipping yet.";
                    return RedirectToAction("PurchaseStatus", new { purchaseId });
                }

                // Update confirmation
                purchase.Confirmation.BuyerReceivedProduct = true;
                purchase.Confirmation.ReceiptConfirmedDate = DateTime.UtcNow;
                purchase.Confirmation.UpdatedAt = DateTime.UtcNow;

                // If both shipping and receipt are confirmed, release payment
                if (purchase.Confirmation.SellerShippedProduct && purchase.Confirmation.BuyerReceivedProduct)
                {
                    purchase.Confirmation.PaymentReleased = true;
                    purchase.Confirmation.PaymentReleasedDate = DateTime.UtcNow;
                    purchase.PaymentStatus = PaymentStatus.Completed;
                    purchase.UpdatedAt = DateTime.UtcNow;

                    // Create notification for seller
                    var sellerNotification = new Notification
                    {
                        UserId = purchase.Listing.SellerId,
                        Message = $"The buyer has confirmed receipt of your product '{purchase.Listing.Title}'. Payment has been released to you.",
                        Type = NotificationType.PurchaseCompleted,
                        RelatedEntityId = purchaseId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(sellerNotification);

                    // Add a message to the conversation about payment release
                    var paymentMessage = new Message
                    {
                        SenderId = userId.Value,
                        ReceiverId = purchase.Listing.SellerId,
                        ListingId = purchase.ListingId,
                        Content = $"I've received the item and confirmed delivery. Payment has been released to you.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Messages.Add(paymentMessage);
                }

                // Create notification for seller that item was received
                var notification = new Notification
                {
                    UserId = purchase.Listing.SellerId,
                    Message = $"The buyer has confirmed receipt of your product '{purchase.Listing.Title}'.",
                    Type = NotificationType.PurchaseCompleted,
                    RelatedEntityId = purchaseId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);

                // Add a message to the conversation between buyer and seller
                var receiptMessage = new Message
                {
                    SenderId = userId.Value,
                    ReceiverId = purchase.Listing.SellerId,
                    ListingId = purchase.ListingId,
                    Content = $"I've received the item. Thank you!",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Messages.Add(receiptMessage);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Receipt confirmation successful.";
                if (purchase.PaymentStatus == PaymentStatus.Completed)
                {
                    TempData["SuccessMessage"] += " Payment has been released to the seller.";
                }

                return RedirectToAction("PurchaseStatus", new { purchaseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming receipt");
                TempData["ErrorMessage"] = "An error occurred while confirming receipt: " + ex.Message;
                return RedirectToAction("PurchaseStatus", new { purchaseId });
            }
        }
    }
}