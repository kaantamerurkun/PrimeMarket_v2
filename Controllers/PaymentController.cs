using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Filters;
using PrimeMarket.Models;
using PrimeMarket.Models.Enum;
using PrimeMarket.Models.Products;
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
                return RedirectToAction("User_Listing_Details", "User", new { id = listingId });
            }

            if (listing.SellerId == userId)
            {
                TempData["ErrorMessage"] = "You cannot purchase your own listing.";
                return RedirectToAction("User_Listing_Details", "User", new { id = listingId });
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

            // Get all bookmarked items as the cart
            var bookmarks = await _context.Bookmarks
                .Include(b => b.Listing)
                .ThenInclude(l => l.Images)
                .Include(b => b.Listing.Seller)
                .Where(b => b.UserId == userId && b.Listing.Status == ListingStatus.Approved)
                .ToListAsync();

            var cartViewModel = new CartViewModel
            {
                Items = bookmarks.Select(b => new CartItemViewModel
                {
                    BookmarkId = b.Id,
                    ListingId = b.ListingId,
                    Title = b.Listing.Title,
                    Price = b.Listing.Price,
                    SellerName = $"{b.Listing.Seller.FirstName} {b.Listing.Seller.LastName}",
                    ImageUrl = b.Listing.Images.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              b.Listing.Images.FirstOrDefault()?.ImagePath ?? "/images/placeholder.png"
                }).ToList(),
                TotalPrice = bookmarks.Sum(b => b.Listing.Price)
            };

            return View(cartViewModel);
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
                var listing = await _context.Listings.FindAsync(listingId);
                if (listing == null)
                {
                    return Json(new { success = false, message = "Listing not found." });
                }

                if (listing.Status != ListingStatus.Approved)
                {
                    return Json(new { success = false, message = "This listing is not available for purchase." });
                }

                // Check if already in cart (bookmarked)
                var existingBookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.ListingId == listingId);

                if (existingBookmark != null)
                {
                    return Json(new { success = true, message = "Item is already in your cart." });
                }

                // Add to cart (bookmark)
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
                return Json(new { success = false, message = $"Error adding to cart: {ex.Message}" });
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
                var bookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.Id == bookmarkId && b.UserId == userId);

                if (bookmark == null)
                {
                    return Json(new { success = false, message = "Item not found in your cart." });
                }

                _context.Bookmarks.Remove(bookmark);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Item removed from cart successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error removing from cart: {ex.Message}" });
            }
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
                TempData["ErrorMessage"] = "Please fill in all required payment information.";
                return RedirectToAction("Checkout", new { listingId = model.ListingId });
            }

            try
            {
                // Get listing information
                var listing = await _context.Listings.FindAsync(model.ListingId);
                if (listing == null)
                {
                    TempData["ErrorMessage"] = "Listing not found.";
                    return RedirectToAction("MyShoppingCart", "User");
                }

                // Ensure listing is available for purchase
                if (listing.Status != ListingStatus.Approved)
                {
                    TempData["ErrorMessage"] = "This listing is not available for purchase.";
                    return RedirectToAction("User_Listing_Details", "User", new { id = model.ListingId });
                }

                // Process payment (in a real system, you would integrate with a payment gateway here)
                // For this implementation, we'll just simulate a successful payment

                // Create a purchase record
                var purchase = new Purchase
                {
                    BuyerId = userId.Value,
                    ListingId = model.ListingId,
                    Amount = listing.Price,
                    PaymentStatus = PaymentStatus.Completed,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Purchases.Add(purchase);

                // Update listing status to sold
                listing.Status = ListingStatus.Sold;
                listing.UpdatedAt = DateTime.UtcNow;

                // Create notifications for buyer and seller
                var buyerNotification = new Notification
                {
                    UserId = userId.Value,
                    Message = $"You have successfully purchased '{listing.Title}' for {listing.Price:C}",
                    Type = NotificationType.PurchaseCompleted,
                    RelatedEntityId = model.ListingId,
                    CreatedAt = DateTime.UtcNow
                };

                var sellerNotification = new Notification
                {
                    UserId = listing.SellerId,
                    Message = $"Your listing '{listing.Title}' has been sold for {listing.Price:C}",
                    Type = NotificationType.PurchaseCompleted,
                    RelatedEntityId = model.ListingId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(buyerNotification);
                _context.Notifications.Add(sellerNotification);

                // Remove from bookmarks/cart if present
                var bookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.ListingId == model.ListingId);

                if (bookmark != null)
                {
                    _context.Bookmarks.Remove(bookmark);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Payment successful! Thank you for your purchase.";
                return RedirectToAction("PurchaseComplete", new { purchaseId = purchase.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error processing payment: {ex.Message}";
                return RedirectToAction("Checkout", new { listingId = model.ListingId });
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

            var completionViewModel = new PurchaseCompleteViewModel
            {
                PurchaseId = purchase.Id,
                ListingTitle = purchase.Listing.Title,
                ListingImage = purchase.Listing.Images.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              purchase.Listing.Images.FirstOrDefault()?.ImagePath ?? "/images/placeholder.png",
                Amount = purchase.Amount,
                SellerName = $"{purchase.Listing.Seller.FirstName} {purchase.Listing.Seller.LastName}",
                PurchaseDate = purchase.CreatedAt ?? DateTime.UtcNow
            };

            return View(completionViewModel);
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

            // Get all bookmarked items (cart items)
            var bookmarks = await _context.Bookmarks
                .Include(b => b.Listing)
                .ThenInclude(l => l.Images)
                .Include(b => b.Listing.Seller)
                .Where(b => b.UserId == userId && b.Listing.Status == ListingStatus.Approved)
                .ToListAsync();

            if (bookmarks.Count == 0)
            {
                TempData["ErrorMessage"] = "Your shopping cart is empty.";
                return RedirectToAction("MyShoppingCart", "User");
            }

            var checkoutViewModel = new MultipleCheckoutViewModel
            {
                Items = bookmarks.Select(b => new CartItemViewModel
                {
                    BookmarkId = b.Id,
                    ListingId = b.ListingId,
                    Title = b.Listing.Title,
                    Price = b.Listing.Price,
                    SellerName = $"{b.Listing.Seller.FirstName} {b.Listing.Seller.LastName}",
                    ImageUrl = b.Listing.Images.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              b.Listing.Images.FirstOrDefault()?.ImagePath ?? "/images/placeholder.png"
                }).ToList(),
                TotalPrice = bookmarks.Sum(b => b.Listing.Price)
            };

            return View(checkoutViewModel);
        }

        [HttpPost]
        [UserAuthenticationFilter]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessMultiplePayments(MultiplePaymentViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all required payment information.";
                return RedirectToAction("CheckoutMultiple");
            }

            try
            {
                // Get all bookmarked items (cart items)
                var bookmarks = await _context.Bookmarks
                    .Include(b => b.Listing)
                    .Where(b => b.UserId == userId && b.Listing.Status == ListingStatus.Approved)
                    .ToListAsync();

                if (bookmarks.Count == 0)
                {
                    TempData["ErrorMessage"] = "Your shopping cart is empty.";
                    return RedirectToAction("MyShoppingCart", "User");
                }

                // Process payment for each item
                foreach (var bookmark in bookmarks)
                {
                    // Create a purchase record
                    var purchase = new Purchase
                    {
                        BuyerId = userId.Value,
                        ListingId = bookmark.ListingId,
                        Amount = bookmark.Listing.Price,
                        PaymentStatus = PaymentStatus.Completed,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Purchases.Add(purchase);

                    // Update listing status to sold
                    bookmark.Listing.Status = ListingStatus.Sold;
                    bookmark.Listing.UpdatedAt = DateTime.UtcNow;

                    // Create notifications for buyer and seller
                    var buyerNotification = new Notification
                    {
                        UserId = userId.Value,
                        Message = $"You have successfully purchased '{bookmark.Listing.Title}' for {bookmark.Listing.Price:C}",
                        Type = NotificationType.PurchaseCompleted,
                        RelatedEntityId = bookmark.ListingId,
                        CreatedAt = DateTime.UtcNow
                    };

                    var sellerNotification = new Notification
                    {
                        UserId = bookmark.Listing.SellerId,
                        Message = $"Your listing '{bookmark.Listing.Title}' has been sold for {bookmark.Listing.Price:C}",
                        Type = NotificationType.PurchaseCompleted,
                        RelatedEntityId = bookmark.ListingId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(buyerNotification);
                    _context.Notifications.Add(sellerNotification);
                }

                // Remove all bookmarks after purchase
                _context.Bookmarks.RemoveRange(bookmarks);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Payment successful! Thank you for your purchases.";
                return RedirectToAction("MultiplePaymentComplete");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error processing payment: {ex.Message}";
                return RedirectToAction("CheckoutMultiple");
            }
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public IActionResult MultiplePaymentComplete()
        {
            return View();
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

            var purchasesViewModel = purchases.Select(p => new PurchaseViewModel
            {
                PurchaseId = p.Id,
                ListingId = p.ListingId,
                ListingTitle = p.Listing.Title,
                ListingImage = p.Listing.Images.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              p.Listing.Images.FirstOrDefault()?.ImagePath ?? "/images/placeholder.png",
                SellerName = $"{p.Listing.Seller.FirstName} {p.Listing.Seller.LastName}",
                Amount = p.Amount,
                PurchaseDate = p.CreatedAt ?? DateTime.MinValue,
                PaymentStatus = p.PaymentStatus
            }).ToList();

            return View(purchasesViewModel);
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

            var salesViewModel = sales.Select(s => new SaleViewModel
            {
                PurchaseId = s.Id,
                ListingId = s.ListingId,
                ListingTitle = s.Listing.Title,
                ListingImage = s.Listing.Images.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              s.Listing.Images.FirstOrDefault()?.ImagePath ?? "/images/placeholder.png",
                BuyerName = $"{s.Buyer.FirstName} {s.Buyer.LastName}",
                Amount = s.Amount,
                SaleDate = s.CreatedAt ?? DateTime.MinValue,
                PaymentStatus = s.PaymentStatus
            }).ToList();

            return View(salesViewModel);
        }
    }

    // ViewModels for Payment
    public class CheckoutViewModel
    {
        public int ListingId { get; set; }
        public string ListingTitle { get; set; }
        public decimal ListingPrice { get; set; }
        public string SellerName { get; set; }
        public string ListingImage { get; set; }
    }

    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class CartItemViewModel
    {
        public int BookmarkId { get; set; }
        public int ListingId { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string SellerName { get; set; }
        public string ImageUrl { get; set; }
    }

    public class PaymentViewModel
    {
        public int ListingId { get; set; }
        public string CardholderName { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string Cvv { get; set; }
    }

    public class MultiplePaymentViewModel
    {
        public string CardholderName { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string Cvv { get; set; }
    }

    public class MultipleCheckoutViewModel
    {
        public List<CartItemViewModel> Items { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class PurchaseCompleteViewModel
    {
        public int PurchaseId { get; set; }
        public string ListingTitle { get; set; }
        public string ListingImage { get; set; }
        public decimal Amount { get; set; }
        public string SellerName { get; set; }
        public DateTime PurchaseDate { get; set; }
    }

    public class PurchaseViewModel
    {
        public int PurchaseId { get; set; }
        public int ListingId { get; set; }
        public string ListingTitle { get; set; }
        public string ListingImage { get; set; }
        public string SellerName { get; set; }
        public decimal Amount { get; set; }
        public DateTime PurchaseDate { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
    }

    public class SaleViewModel
    {
        public int PurchaseId { get; set; }
        public int ListingId { get; set; }
        public string ListingTitle { get; set; }
        public string ListingImage { get; set; }
        public string BuyerName { get; set; }
        public decimal Amount { get; set; }
        public DateTime SaleDate { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
    }
}