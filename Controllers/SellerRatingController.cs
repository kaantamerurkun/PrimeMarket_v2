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
    public class SellerRatingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SellerRatingController> _logger;

        public SellerRatingController(ApplicationDbContext context, ILogger<SellerRatingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        [UserAuthenticationFilter]
        public async Task<IActionResult> RateSeller(int sellerId, int rating, string comment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Please log in to rate sellers." });
            }

            try
            {
                // Check if the seller exists
                var seller = await _context.Users.FindAsync(sellerId);
                if (seller == null)
                {
                    return Json(new { success = false, message = "Seller not found." });
                }

                // Check if the user has completed a purchase from this seller
                var completedPurchase = await _context.Purchases
                    .Include(p => p.Listing)
                    .Include(p => p.Confirmation)
                    .Where(p => p.BuyerId == userId &&
                               p.Listing.SellerId == sellerId &&
                               p.PaymentStatus == PaymentStatus.Completed &&
                               p.Confirmation != null &&
                               p.Confirmation.BuyerReceivedProduct)
                    .FirstOrDefaultAsync();

                if (completedPurchase == null)
                {
                    return Json(new { success = false, message = "You can only rate sellers from whom you have completed a purchase." });
                }

                // Check if the user has already rated this seller for this purchase
                var existingRating = await _context.SellerRatings
                    .FirstOrDefaultAsync(r => r.BuyerId == userId &&
                                            r.SellerId == sellerId &&
                                            r.PurchaseId == completedPurchase.Id);

                if (existingRating != null)
                {
                    // Update existing rating
                    existingRating.Rating = rating;
                    //existingRating.Comment = comment;
                    existingRating.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new rating
                    var sellerRating = new SellerRating
                    {
                        BuyerId = userId.Value,
                        SellerId = sellerId,
                        Rating = rating,
                        //Comment = comment,
                        PurchaseId = completedPurchase.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.SellerRatings.Add(sellerRating);
                }

                await _context.SaveChangesAsync();

                // Calculate new average rating
                var allRatings = await _context.SellerRatings
                    .Where(r => r.SellerId == sellerId)
                    .ToListAsync();

                var averageRating = allRatings.Average(r => r.Rating);
                var totalRatings = allRatings.Count;

                return Json(new
                {
                    success = true,
                    message = "Rating submitted successfully.",
                    averageRating = Math.Round(averageRating, 1),
                    totalRatings = totalRatings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rating seller");
                return Json(new { success = false, message = "An error occurred while submitting your rating." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSellerRating(int sellerId)
        {
            try
            {
                var ratings = await _context.SellerRatings
                    .Where(r => r.SellerId == sellerId)
                    .ToListAsync();

                if (!ratings.Any())
                {
                    return Json(new
                    {
                        success = true,
                        averageRating = 0,
                        totalRatings = 0,
                        userRating = 0
                    });
                }

                var averageRating = ratings.Average(r => r.Rating);
                var totalRatings = ratings.Count;

                // Get current user's rating if logged in
                int userRating = 0;
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId != null)
                {
                    var userRatingEntity = ratings.FirstOrDefault(r => r.BuyerId == userId);
                    userRating = userRatingEntity?.Rating ?? 0;
                }

                return Json(new
                {
                    success = true,
                    averageRating = Math.Round(averageRating, 1),
                    totalRatings = totalRatings,
                    userRating = userRating
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller rating");
                return Json(new { success = false, message = "An error occurred while getting the rating." });
            }
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> CanRateSeller(int sellerId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { canRate = false, message = "Please log in to rate sellers." });
            }

            try
            {
                // Check if the user has completed a purchase from this seller
                var hasCompletedPurchase = await _context.Purchases
                    .Include(p => p.Listing)
                    .Include(p => p.Confirmation)
                    .AnyAsync(p => p.BuyerId == userId &&
                                  p.Listing.SellerId == sellerId &&
                                  p.PaymentStatus == PaymentStatus.Completed &&
                                  p.Confirmation != null &&
                                  p.Confirmation.BuyerReceivedProduct);

                if (!hasCompletedPurchase)
                {
                    return Json(new { canRate = false, message = "You can only rate sellers from whom you have completed a purchase." });
                }

                return Json(new { canRate = true, message = "You can rate this seller." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can rate seller");
                return Json(new { canRate = false, message = "An error occurred while checking rating eligibility." });
            }
        }
    }
}