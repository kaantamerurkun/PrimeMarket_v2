using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Filters;
using PrimeMarket.Models;
using PrimeMarket.Models.Enum;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PrimeMarket.Controllers
{
    public class ProductReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductReviewController> _logger;

        public ProductReviewController(ApplicationDbContext context, ILogger<ProductReviewController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        [UserAuthenticationFilter]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int listingId, int rating, string comment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Please log in to add a review." });
            }

            try
            {
                var listing = await _context.Listings
                    .FirstOrDefaultAsync(l => l.Id == listingId);

                if (listing == null)
                {
                    return Json(new { success = false, message = "Listing not found." });
                }

                if (listing.Condition != "First-Hand")
                {
                    return Json(new { success = false, message = "Reviews are only allowed for first-hand products." });
                }

                var hasPurchased = await _context.Purchases
                    .Include(p => p.Confirmation)
                    .AnyAsync(p => p.BuyerId == userId.Value &&
                                  p.ListingId == listingId &&
                                  p.PaymentStatus == PaymentStatus.Completed &&
                                  p.Confirmation != null &&
                                  p.Confirmation.BuyerReceivedProduct);

                if (!hasPurchased)
                {
                    return Json(new { success = false, message = "You can only review products you have purchased and received." });
                }

                var existingReview = await _context.ProductReviews
                    .FirstOrDefaultAsync(r => r.UserId == userId.Value && r.ListingId == listingId);

                if (existingReview != null)
                {
                    return Json(new { success = false, message = "You have already reviewed this product." });
                }

                var review = new ProductReview
                {
                    ListingId = listingId,
                    UserId = userId.Value,
                    Rating = rating,
                    Comment = comment,
                    IsVerifiedPurchase = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProductReviews.Add(review);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Review added successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding review");
                return Json(new { success = false, message = "An error occurred while adding your review." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetListingReviews(int listingId)
        {
            try
            {
                var reviews = await _context.ProductReviews
                    .Include(r => r.User)
                    .Where(r => r.ListingId == listingId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new
                    {
                        Id = r.Id,
                        UserName = $"{r.User.FirstName} {r.User.LastName}",
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        IsVerifiedPurchase = r.IsVerifiedPurchase
                    })
                    .ToListAsync();

                var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
                var totalReviews = reviews.Count;

                return Json(new
                {
                    success = true,
                    reviews = reviews,
                    averageRating = Math.Round(averageRating, 1),
                    totalReviews = totalReviews
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews");
                return Json(new { success = false, message = "An error occurred while loading reviews." });
            }
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> CanReview(int listingId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { canReview = false, message = "Please log in to review products." });
            }

            try
            {
                var listing = await _context.Listings
                    .FirstOrDefaultAsync(l => l.Id == listingId);

                if (listing == null)
                {
                    return Json(new { canReview = false, message = "Listing not found." });
                }

                if (listing.Condition != "First-Hand")
                {
                    return Json(new { canReview = false, message = "Reviews are only allowed for first-hand products." });
                }

                var hasPurchased = await _context.Purchases
                    .Include(p => p.Confirmation)
                    .AnyAsync(p => p.BuyerId == userId.Value &&
                                  p.ListingId == listingId &&
                                  p.PaymentStatus == PaymentStatus.Completed &&
                                  p.Confirmation != null &&
                                  p.Confirmation.BuyerReceivedProduct);

                if (!hasPurchased)
                {
                    return Json(new { canReview = false, message = "You can only review products you have purchased and received." });
                }

                var hasReviewed = await _context.ProductReviews
                    .AnyAsync(r => r.UserId == userId.Value && r.ListingId == listingId);

                if (hasReviewed)
                {
                    return Json(new { canReview = false, message = "You have already reviewed this product." });
                }

                return Json(new { canReview = true, message = "You can review this product." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking review eligibility");
                return Json(new { canReview = false, message = "An error occurred while checking review eligibility." });
            }
        }
    }
}