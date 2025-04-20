using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Filters;
using PrimeMarket.Models;
using PrimeMarket.Models.Enum;
using PrimeMarket.Models.Products;
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

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
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

            var listing = await _context.Listings
                .Include(l => l.Images)
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null)
            {
                return NotFound();
            }

            if (listing.Status != ListingStatus.Approved)
            {
                TempData["ErrorMessage"] = "This listing is not available for purchase.";
                return RedirectToAction("Details", "Listing", new { id = listingId });
            }

            if (listing.SellerId == userId)
            {
                TempData["ErrorMessage"] = "You cannot purchase your own listing.";
                return RedirectToAction("Details", "Listing", new { id = listingId });
            }

            var checkoutViewModel = new CheckoutViewModel
            {
                ListingId = listing.Id,
                ListingTitle = listing.Title,
                ListingPrice = listing.Price,
                SellerName = $"{listing.Seller.FirstName} {listing.Seller.LastName}",
                ListingImage = listing.Images.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              listing.Images.FirstOrDefault()?.ImagePath ?? "/images/placeholder.png"
            };

            return View(checkoutViewModel);
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

            // Get user's bookmarks (which serve as cart items)
            var bookmarks = await _context.Bookmarks
                .Include(b => b.Listing)
                .ThenInclude(l => l.Images)
                .Include(b => b.Listing.Seller)
                .Where(b => b.UserId == userId && b.Listing.Status == ListingStatus.Approved)
                .ToListAsync();

            var cartItems = bookmarks.Select(b => new CartItemViewModel
            {
                BookmarkId = b.Id,
                ListingId = b.ListingId,
                Title = b.Listing.Title,
                Price = b.Listing.Price,
                SellerName = $"{b.Listing.Seller.FirstName} {b.Listing.Seller.LastName}",
                ImageUrl = b.Listing.Images.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                          b.Listing.Images.FirstOrDefault()?.ImagePath ?? "/images/placeholder.png"
            }).ToList();

            var cartViewModel = new CartViewModel
            {
                Items = cartItems,
                TotalPrice = cartItems.Sum(i => i.Price)
            };

            return View(cartViewModel);
        }

        [HttpPost]
        [UserAuthenticationFilter]
        public async Task<IActionResult> RemoveFromCart(int bookmarkId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "You must be logged in to perform this action." });
            }

            try
            {
                var bookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.Id == bookmarkId && b.UserId == userId);

                if (bookmark == null)
                {
                    return Json(new { success = false, message = "Item not found in your cart." });
                }

                _context.Bookmarks.Remove(bookmark);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
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

            // Get user's bookmarks (cart items)
            var bookmarks = await _context.Bookmarks
                .Include(b => b.Listing)
                .ThenInclude(l => l.Images)
                .Include(b => b.Listing.Seller)
                .Where(b => b.UserId == userId && b.Listing.Status == ListingStatus.Approved)
                .ToListAsync();

            if (bookmarks.Count == 0)
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("Cart");
            }

            var cartItems = bookmarks.Select(b => new CartItemViewModel
            {
                BookmarkId = b.Id,
                ListingId = b.ListingId,
                Title = b.Listing.Title,
                Price = b.Listing.Price,
                SellerName = $"{b.Listing.Seller.FirstName} {b.Listing.Seller.LastName}",
                ImageUrl = b.Listing.Images.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                          b.Listing.Images.FirstOrDefault()?.ImagePath ?? "/images/placeholder.png"
            }).ToList();

            var checkoutViewModel = new MultipleCheckoutViewModel
            {
                Items = cartItems,
                TotalPrice = cartItems.Sum(i => i.Price)
            };

            return View(checkoutViewModel);
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

                // Create purchase record
                var purchase = new Purchase
                {
                    BuyerId = userId.Value,
                    ListingId = listing.Id,
                    Amount = listing.Price,
                    PaymentStatus = PaymentStatus.Completed,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Purchases.Add(purchase);

                // Update listing status
                listing.Status = ListingStatus.Sold;
                listing.UpdatedAt = DateTime.UtcNow;

                // Create notifications for buyer and seller
                var buyerNotification = new Notification
                {
                    UserId = userId.Value,
                    Message = $"You have successfully purchased '{listing.Title}'",
                    Type = NotificationType.PurchaseCompleted,
                    RelatedEntityId = purchase.Id,
                    CreatedAt = DateTime.UtcNow
                };

                var sellerNotification = new Notification
                {
                    UserId = listing.SellerId,
                    Message = $"Your listing '{listing.Title}' has been sold",
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
                // Get the user's cart items
                var bookmarks = await _context.Bookmarks
                    .Include(b => b.Listing)
                    .Where(b => b.UserId == userId && b.Listing.Status == ListingStatus.Approved)
                    .ToListAsync();

                if (bookmarks.Count == 0)
                {
                    TempData["ErrorMessage"] = "Your cart is empty.";
                    return RedirectToAction("Cart");
                }

                List<Purchase> purchases = new List<Purchase>();
                List<Notification> notifications = new List<Notification>();

                // Process each item in the cart
                foreach (var bookmark in bookmarks)
                {
                    var listing = bookmark.Listing;

                    // Create purchase record
                    var purchase = new Purchase
                    {
                        BuyerId = userId.Value,
                        ListingId = listing.Id,
                        Amount = listing.Price,
                        PaymentStatus = PaymentStatus.Completed,
                        CreatedAt = DateTime.UtcNow
                    };

                    purchases.Add(purchase);

                    // Update listing status
                    listing.Status = ListingStatus.Sold;
                    listing.UpdatedAt = DateTime.UtcNow;

                    // Create notifications for buyer and seller
                    var buyerNotification = new Notification
                    {
                        UserId = userId.Value,
                        Message = $"You have successfully purchased '{listing.Title}'",
                        Type = NotificationType.PurchaseCompleted,
                        RelatedEntityId = listing.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    var sellerNotification = new Notification
                    {
                        UserId = listing.SellerId,
                        Message = $"Your listing '{listing.Title}' has been sold",
                        Type = NotificationType.PurchaseCompleted,
                        RelatedEntityId = listing.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    notifications.Add(buyerNotification);
                    notifications.Add(sellerNotification);
                }

                // Add all purchases and notifications to database
                _context.Purchases.AddRange(purchases);
                _context.Notifications.AddRange(notifications);

                // Clear the cart (bookmarks)
                _context.Bookmarks.RemoveRange(bookmarks);

                await _context.SaveChangesAsync();

                return RedirectToAction("PurchaseComplete", new { purchaseId = purchases.First().Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while processing your payment: " + ex.Message;
                return RedirectToAction("CheckoutMultiple");
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

            var purchase = await _context.Purchases
                .Include(p => p.Listing)
                .ThenInclude(l => l.Images)
                .Include(p => p.Listing.Seller)
                .FirstOrDefaultAsync(p => p.Id == purchaseId && p.BuyerId == userId);

            if (purchase == null)
            {
                return NotFound();
            }

            var viewModel = new PurchaseCompleteViewModel
            {
                PurchaseId = purchase.Id,
                ListingTitle = purchase.Listing.Title,
                ListingImage = purchase.Listing.Images.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              purchase.Listing.Images.FirstOrDefault()?.ImagePath ?? "/images/placeholder.png",
                Amount = purchase.Amount,
                SellerName = $"{purchase.Listing.Seller.FirstName} {purchase.Listing.Seller.LastName}",
                PurchaseDate = purchase.CreatedAt ?? DateTime.UtcNow
            };

            return View(viewModel);
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
                .Where(p => p.BuyerId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var viewModels = purchases.Select(p => new PurchaseViewModel
            {
                PurchaseId = p.Id,
                ListingId = p.ListingId,
                ListingTitle = p.Listing.Title,
                ListingImage = p.Listing.Images.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              p.Listing.Images.FirstOrDefault()?.ImagePath ?? "/images/placeholder.png",
                SellerName = $"{p.Listing.Seller.FirstName} {p.Listing.Seller.LastName}",
                Amount = p.Amount,
                PurchaseDate = p.CreatedAt ?? DateTime.UtcNow,
                PaymentStatus = p.PaymentStatus
            }).ToList();

            return View(viewModels);
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
                .Where(p => p.Listing.SellerId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var viewModels = sales.Select(p => new SaleViewModel
            {
                PurchaseId = p.Id,
                ListingId = p.ListingId,
                ListingTitle = p.Listing.Title,
                ListingImage = p.Listing.Images.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              p.Listing.Images.FirstOrDefault()?.ImagePath ?? "/images/placeholder.png",
                BuyerName = $"{p.Buyer.FirstName} {p.Buyer.LastName}",
                Amount = p.Amount,
                SaleDate = p.CreatedAt ?? DateTime.UtcNow,
                PaymentStatus = p.PaymentStatus
            }).ToList();

            return View(viewModels);
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> AddToCart(int listingId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            try
            {
                // Check if listing exists and is available
                var listing = await _context.Listings
                    .FirstOrDefaultAsync(l => l.Id == listingId && l.Status == ListingStatus.Approved);

                if (listing == null)
                {
                    TempData["ErrorMessage"] = "This listing is not available.";
                    return RedirectToAction("Details", "Listing", new { id = listingId });
                }

                // Check if already in cart (bookmarked)
                var existingBookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.ListingId == listingId);

                if (existingBookmark != null)
                {
                    TempData["SuccessMessage"] = "This item is already in your cart.";
                    return RedirectToAction("Cart");
                }

                // Add to cart (create bookmark)
                var bookmark = new Bookmark
                {
                    UserId = userId.Value,
                    ListingId = listingId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Bookmarks.Add(bookmark);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Item added to your cart.";
                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Details", "Listing", new { id = listingId });
            }
        }
    }
}