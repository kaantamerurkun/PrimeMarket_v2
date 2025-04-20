using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Filters;
using PrimeMarket.Models;
using PrimeMarket.Models.Enum;
using PrimeMarket.Models.Factory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PrimeMarket.Controllers
{
    public class ListingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ListingController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [UserAuthenticationFilter]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateListing(ListingViewModel model, List<IFormFile> images)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/User/CreateListing.cshtml", model);
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            try
            {
                // Create the listing
                var listing = new Listing
                {
                    SellerId = userId.Value,
                    Title = model.Title,
                    Price = model.Price,
                    Description = model.Description,
                    Condition = model.Condition,
                    Category = model.Category,
                    SubCategory = model.SubCategory,
                    DetailCategory = model.DetailCategory,
                    Location = model.Location,
                    Status = ListingStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Listings.Add(listing);
                await _context.SaveChangesAsync();

                // Create the appropriate product model based on the category/subcategory
                if (!string.IsNullOrEmpty(model.Category) && !string.IsNullOrEmpty(model.SubCategory))
                {
                    try
                    {
                        var product = ProductFactory.CreateProduct(model.Category, model.SubCategory);

                        // Set product properties based on dynamic fields from the form
                        // This depends on what properties you're collecting for each type of product
                        if (product != null)
                        {
                            product.ListingId = listing.Id;

                            // Use reflection to set properties from the model's DynamicProperties
                            if (model.DynamicProperties != null)
                            {
                                foreach (var prop in model.DynamicProperties)
                                {
                                    var productProp = product.GetType().GetProperty(prop.Key);
                                    if (productProp != null)
                                    {
                                        // Handle different property types
                                        if (productProp.PropertyType == typeof(bool))
                                        {
                                            bool.TryParse(prop.Value, out bool boolValue);
                                            productProp.SetValue(product, boolValue);
                                        }
                                        else
                                        {
                                            productProp.SetValue(product, prop.Value);
                                        }
                                    }
                                }
                            }

                            // Add the product to the appropriate DbSet
                            var dbSetProperty = _context.GetType().GetProperty(product.GetType().Name + "s");
                            if (dbSetProperty != null)
                            {
                                var dbSet = dbSetProperty.GetValue(_context);
                                dbSet.GetType().GetMethod("Add").Invoke(dbSet, new[] { product });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log or handle the error
                        Console.WriteLine($"Error creating product: {ex.Message}");
                    }
                }

                // Process and save images
                if (images != null && images.Count > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "listings");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    bool isFirstImage = true;
                    foreach (var image in images)
                    {
                        if (image.Length > 0)
                        {
                            var uniqueFileName = $"{listing.Id}_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(fileStream);
                            }

                            var listingImage = new ListingImage
                            {
                                ListingId = listing.Id,
                                ImagePath = $"/images/listings/{uniqueFileName}",
                                IsMainImage = isFirstImage // First image is the main image
                            };

                            _context.ListingImages.Add(listingImage);
                            isFirstImage = false;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Create notification for admin
                var adminNotification = new Notification
                {
                    UserId = 1, // Assuming user ID 1 is an admin, adjust as needed
                    Message = $"New listing '{model.Title}' needs review",
                    Type = NotificationType.ListingApproved,
                    RelatedEntityId = listing.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(adminNotification);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your listing has been created and is pending approval.";
                return RedirectToAction("MyProfilePage", "User");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating listing: {ex.Message}");
                return View("~/Views/User/CreateListing.cshtml", model);
            }
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> MyListings()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var listings = await _context.Listings
                .Where(l => l.SellerId == userId.Value)
                .Include(l => l.Images)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(listings);
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> EditListing(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var listing = await _context.Listings
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.Id == id && l.SellerId == userId.Value);

            if (listing == null)
            {
                return NotFound();
            }

            // Get product details based on category and subcategory
            // This is a simplification - you'll need to fetch the actual product data
            var model = new ListingViewModel
            {
                Id = listing.Id,
                Title = listing.Title,
                Price = listing.Price,
                Description = listing.Description,
                Condition = listing.Condition,
                Category = listing.Category,
                SubCategory = listing.SubCategory,
                DetailCategory = listing.DetailCategory,
                Location = listing.Location,
                Images = listing.Images.ToList()
            };

            return View(model);
        }

        [HttpPost]
        [UserAuthenticationFilter]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditListing(ListingViewModel model, List<IFormFile> newImages)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var listing = await _context.Listings
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.Id == model.Id && l.SellerId == userId.Value);

            if (listing == null)
            {
                return NotFound();
            }

            try
            {
                // Update listing properties
                listing.Title = model.Title;
                listing.Price = model.Price;
                listing.Description = model.Description;
                listing.Location = model.Location;
                listing.UpdatedAt = DateTime.UtcNow;
                // Reset status to pending if it was previously rejected
                if (listing.Status == ListingStatus.Rejected)
                {
                    listing.Status = ListingStatus.Pending;
                    listing.RejectionReason = null;
                }

                // Process and save new images
                if (newImages != null && newImages.Count > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "listings");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    bool isFirstImage = !listing.Images.Any();
                    foreach (var image in newImages)
                    {
                        if (image.Length > 0)
                        {
                            var uniqueFileName = $"{listing.Id}_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(fileStream);
                            }

                            var listingImage = new ListingImage
                            {
                                ListingId = listing.Id,
                                ImagePath = $"/images/listings/{uniqueFileName}",
                                IsMainImage = isFirstImage
                            };

                            _context.ListingImages.Add(listingImage);
                            isFirstImage = false;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Your listing has been updated and is pending review.";
                return RedirectToAction("MyListings");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating listing: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        [UserAuthenticationFilter]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteListing(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == id && l.SellerId == userId.Value);

            if (listing == null)
            {
                return NotFound();
            }

            try
            {
                // Check if the listing can be deleted (not already sold)
                if (listing.Status == ListingStatus.Sold)
                {
                    TempData["ErrorMessage"] = "Cannot delete a sold listing.";
                    return RedirectToAction("MyListings");
                }

                // Set as Archived instead of actually deleting
                listing.Status = ListingStatus.Archived;
                listing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your listing has been deleted.";
                return RedirectToAction("MyListings");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting listing: {ex.Message}";
                return RedirectToAction("MyListings");
            }
        }

        [HttpPost]
        [UserAuthenticationFilter]
        public async Task<IActionResult> ToggleBookmark(int listingId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Please log in to bookmark items." });
            }

            try
            {
                var existingBookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.UserId == userId.Value && b.ListingId == listingId);

                if (existingBookmark != null)
                {
                    // Remove bookmark
                    _context.Bookmarks.Remove(existingBookmark);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, isBookmarked = false });
                }
                else
                {
                    // Add bookmark
                    var bookmark = new Bookmark
                    {
                        UserId = userId.Value,
                        ListingId = listingId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Bookmarks.Add(bookmark);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, isBookmarked = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [UserAuthenticationFilter]
        public async Task<IActionResult> MakeOffer(OfferViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Please log in to make an offer." });
            }

            if (model.OfferAmount <= 0)
            {
                return Json(new { success = false, message = "Offer amount must be greater than zero." });
            }

            try
            {
                var listing = await _context.Listings.FindAsync(model.ListingId);
                if (listing == null)
                {
                    return Json(new { success = false, message = "Listing not found." });
                }

                // Check if user is trying to make an offer on their own listing
                if (listing.SellerId == userId.Value)
                {
                    return Json(new { success = false, message = "You cannot make an offer on your own listing." });
                }

                // Create the offer
                var offer = new Offer
                {
                    BuyerId = userId.Value,
                    ListingId = model.ListingId,
                    OfferAmount = model.OfferAmount,
                    Message = model.Message,
                    Status = OfferStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Offers.Add(offer);

                // Create notification for the seller
                var notification = new Notification
                {
                    UserId = listing.SellerId,
                    Message = $"You have received a new offer of {model.OfferAmount:C} for your listing '{listing.Title}'",
                    Type = NotificationType.NewOffer,
                    RelatedEntityId = model.ListingId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Your offer has been sent to the seller." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error making offer: {ex.Message}" });
            }
        }

        [HttpPost]
        [UserAuthenticationFilter]
        public async Task<IActionResult> RespondToOffer(int offerId, bool accept)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Please log in to respond to offers." });
            }

            try
            {
                var offer = await _context.Offers
                    .Include(o => o.Listing)
                    .FirstOrDefaultAsync(o => o.Id == offerId && o.Listing.SellerId == userId.Value);

                if (offer == null)
                {
                    return Json(new { success = false, message = "Offer not found or you don't have permission to respond to it." });
                }

                if (accept)
                {
                    // Accept the offer
                    offer.Status = OfferStatus.Accepted;

                    // Create notification for the buyer
                    var acceptNotification = new Notification
                    {
                        UserId = offer.BuyerId,
                        Message = $"Your offer of {offer.OfferAmount:C} for '{offer.Listing.Title}' has been accepted",
                        Type = NotificationType.OfferAccepted,
                        RelatedEntityId = offer.ListingId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(acceptNotification);
                }
                else
                {
                    // Reject the offer
                    offer.Status = OfferStatus.Rejected;

                    // Create notification for the buyer
                    var rejectNotification = new Notification
                    {
                        UserId = offer.BuyerId,
                        Message = $"Your offer of {offer.OfferAmount:C} for '{offer.Listing.Title}' has been rejected",
                        Type = NotificationType.OfferRejected,
                        RelatedEntityId = offer.ListingId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(rejectNotification);
                }

                offer.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = accept ? "Offer accepted successfully." : "Offer rejected successfully."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error responding to offer: {ex.Message}" });
            }
        }
    }

    public class ListingViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string Condition { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string DetailCategory { get; set; }
        public string Location { get; set; }
        public Dictionary<string, string> DynamicProperties { get; set; }
        public List<ListingImage> Images { get; set; }
    }

    public class OfferViewModel
    {
        public int ListingId { get; set; }
        public decimal OfferAmount { get; set; }
        public string Message { get; set; }
    }
}