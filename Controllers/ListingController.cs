using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Filters;
using PrimeMarket.Models;
using PrimeMarket.Models.Enum;
using PrimeMarket.Models.Factory;
using PrimeMarket.Models.ViewModel;
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
        private readonly ILogger<ListingController> _logger;

        public ListingController(ApplicationDbContext context, ILogger<ListingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAuthenticationFilter] // Added authentication filter
        public async Task<IActionResult> CreateListing(ListingViewModel model, List<IFormFile> images)
        {
            _logger.LogInformation("CreateListing called with model: {@Model}", model);

            // Check if the user is authenticated
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                _logger.LogWarning("User not authenticated");
                return RedirectToAction("Login", "User");
            }

            // Add validation error messages for missing required fields
            if (string.IsNullOrEmpty(model.Title))
                ModelState.AddModelError("Title", "Title is required");

            if (model.Price <= 0)
                ModelState.AddModelError("Price", "Price must be greater than 0");

            if (string.IsNullOrEmpty(model.Description))
                ModelState.AddModelError("Description", "Description is required");

            if (string.IsNullOrEmpty(model.Condition))
                ModelState.AddModelError("Condition", "Condition is required");

            if (string.IsNullOrEmpty(model.Category))
                ModelState.AddModelError("Category", "Category is required");

            if (string.IsNullOrEmpty(model.Location))
                ModelState.AddModelError("Location", "Location is required");

            if (images == null || images.Count == 0)
                ModelState.AddModelError("images", "At least one image is required");

            // Check model validity
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid: {@ModelState}", ModelState);

                // Log the model values for debugging
                foreach (var prop in model.GetType().GetProperties())
                {
                    _logger.LogInformation($"Property {prop.Name} = {prop.GetValue(model)}");
                }

                // Pass the selected values back to the view
                ViewBag.SelectedCondition = model.Condition;
                ViewBag.SelectedCategory = model.Category;
                ViewBag.SelectedSubCategory = model.SubCategory;
                ViewBag.SelectedDetailCategory = model.DetailCategory;

                // Return to the create view with the model to retain form data
                return View("~/Views/User/CreateListing.cshtml", model);
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

                _logger.LogInformation("Listing created with ID: {ListingId}", listing.Id);

                // Create the appropriate product model based on the category/subcategory
                if (!string.IsNullOrEmpty(model.Category) && !string.IsNullOrEmpty(model.SubCategory))
                {
                    try
                    {
                        var product = ProductFactory.CreateProduct(model.Category, model.SubCategory);
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
                                        try
                                        {
                                            // Handle different property types
                                            if (productProp.PropertyType == typeof(bool))
                                            {
                                                bool boolValue = prop.Value.ToLower() == "yes" || prop.Value.ToLower() == "true";
                                                productProp.SetValue(product, boolValue);
                                            }
                                            else if (productProp.PropertyType == typeof(int))
                                            {
                                                int.TryParse(prop.Value, out int intValue);
                                                productProp.SetValue(product, intValue);
                                            }
                                            else if (productProp.PropertyType == typeof(decimal))
                                            {
                                                decimal.TryParse(prop.Value, out decimal decimalValue);
                                                productProp.SetValue(product, decimalValue);
                                            }
                                            else
                                            {
                                                productProp.SetValue(product, prop.Value);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            // Log the error but continue
                                            _logger.LogError(ex, "Error setting property {PropertyName}: {ErrorMessage}", prop.Key, ex.Message);
                                        }
                                    }
                                }
                            }

                            // Add the product to the appropriate DbSet
                            var dbSetProperty = _context.GetType().GetProperty(product.GetType().Name + "s");
                            if (dbSetProperty != null)
                            {
                                var dbSet = dbSetProperty.GetValue(_context);
                                if (dbSet != null)
                                {
                                    var addMethod = dbSet.GetType().GetMethod("Add");
                                    if (addMethod != null)
                                    {
                                        addMethod.Invoke(dbSet, new[] { product });
                                        _logger.LogInformation("Product of type {ProductType} added for listing {ListingId}", product.GetType().Name, listing.Id);
                                    }
                                    else
                                    {
                                        _logger.LogError("Could not find Add method for DbSet of type {ProductType}", product.GetType().Name + "s");
                                    }
                                }
                                else
                                {
                                    _logger.LogError("DbSet property returned null for {ProductType}", product.GetType().Name + "s");
                                }
                            }
                            else
                            {
                                _logger.LogError("DbSet property not found for {ProductType}", product.GetType().Name + "s");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue with listing creation
                        _logger.LogError(ex, "Error creating product for listing {ListingId}: {ErrorMessage}", listing.Id, ex.Message);
                    }
                }

                // Process and save images
                if (images != null && images.Count > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "listings");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                        _logger.LogInformation("Created uploads folder: {FolderPath}", uploadsFolder);
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
                            _logger.LogInformation("Image added for listing {ListingId}: {ImagePath}", listing.Id, listingImage.ImagePath);
                            isFirstImage = false;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("No images provided for listing {ListingId}", listing.Id);
                }

                await _context.SaveChangesAsync();

                // Create notifications for all admins
                var admins = await _context.Admins.ToListAsync();
                if (admins.Any())
                {
                    foreach (var admin in admins)
                    {
                        var adminNotification = new Notification
                        {
                            UserId = admin.Id,
                            Message = $"New listing '{model.Title}' needs review",
                            Type = NotificationType.ListingApproved,
                            RelatedEntityId = listing.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Notifications.Add(adminNotification);
                        _logger.LogInformation("Notification created for admin {AdminId}", admin.Id);
                    }

                    await _context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogWarning("No admins found to notify about new listing {ListingId}", listing.Id);
                }

                _logger.LogInformation("Listing process completed successfully for listing {ListingId}", listing.Id);
                TempData["SuccessMessage"] = "Your listing has been created and is pending approval.";
                return RedirectToAction("MyProfilePage", "User");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating listing: {ErrorMessage}", ex.Message);
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

            // Prepare the view model with existing data
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

            // Fetch product-specific properties
            var dynamicProperties = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(listing.SubCategory))
            {
                dynamic product = null;
                // Get the appropriate product type based on subcategory
                switch (listing.SubCategory)
                {
                    // Phones
                    case "IOS Phone":
                        product = await _context.IOSPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Android Phone":
                        product = await _context.AndroidPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Other Phones":
                        product = await _context.OtherPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Phone Accessories":
                        product = await _context.PhoneAccessories.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;

                    // Tablets
                    case "IOS Tablets":
                        product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Android Tablets":
                        product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Other Tablets":
                        product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Tablet Accessories":
                        product = await _context.TabletAccessories.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;

                    // Computers
                    case "Laptops":
                        product = await _context.Laptops.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Desktops":
                        product = await _context.Desktops.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Computer Accessories":
                        product = await _context.ComputerAccessories.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;

                    // White Goods
                    case "Fridges":
                        product = await _context.Fridges.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Washers":
                        product = await _context.Washers.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Dishwashers":
                        product = await _context.Dishwashers.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Ovens":
                        product = await _context.Ovens.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;

                    // Electronics
                    case "Vacuum Cleaner":
                        product = await _context.VacuumCleaners.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Televisions":
                        product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;

                    // Special handling for tablets since they are split across different types
                    case "Tablets":
                        // Try each tablet type in order
                        product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        if (product == null)
                        {
                            product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        }
                        if (product == null)
                        {
                            product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        }
                        break;
                }

                // If product is found, extract properties using reflection
                if (product != null)
                {
                    foreach (var prop in product.GetType().GetProperties())
                    {
                        // Skip navigation or system properties
                        if (prop.Name == "Id" || prop.Name == "ListingId" || prop.Name == "Listing")
                            continue;

                        var value = prop.GetValue(product);
                        if (value != null)
                        {
                            dynamicProperties[prop.Name] = value.ToString();
                        }
                    }
                }
            }

            model.DynamicProperties = dynamicProperties;
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

                // Update product-specific properties if applicable
                if (model.DynamicProperties != null && !string.IsNullOrEmpty(listing.SubCategory))
                {
                    dynamic product = null;

                    // Get the appropriate product based on subcategory
                    switch (listing.SubCategory)
                    {
                        case "IOS Phone":
                            product = await _context.IOSPhones.FirstOrDefaultAsync(p => p.ListingId == model.Id);
                            break;
                        case "Android Phone":
                            product = await _context.AndroidPhones.FirstOrDefaultAsync(p => p.ListingId == model.Id);
                            break;
                        case "Laptops":
                            product = await _context.Laptops.FirstOrDefaultAsync(p => p.ListingId == model.Id);
                            break;
                            // Add more cases for other subcategories
                    }

                    // Update product properties if product exists
                    if (product != null)
                    {
                        foreach (var prop in model.DynamicProperties)
                        {
                            var productProp = product.GetType().GetProperty(prop.Key);
                            if (productProp != null)
                            {
                                try
                                {
                                    // Convert value to appropriate type
                                    if (productProp.PropertyType == typeof(bool))
                                    {
                                        bool.TryParse(prop.Value, out bool boolValue);
                                        productProp.SetValue(product, boolValue);
                                    }
                                    else if (productProp.PropertyType == typeof(int))
                                    {
                                        int.TryParse(prop.Value, out int intValue);
                                        productProp.SetValue(product, intValue);
                                    }
                                    else if (productProp.PropertyType == typeof(decimal))
                                    {
                                        decimal.TryParse(prop.Value, out decimal decimalValue);
                                        productProp.SetValue(product, decimalValue);
                                    }
                                    else
                                    {
                                        productProp.SetValue(product, prop.Value);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Log error but continue
                                    Console.WriteLine($"Error updating property {prop.Key}: {ex.Message}");
                                }
                            }
                        }
                    }
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

                // Create notification for admin about the update
                var adminNotification = new Notification
                {
                    UserId = 1, // Assuming admin ID is 1
                    Message = $"Updated listing '{model.Title}' needs review",
                    Type = NotificationType.ListingApproved, // Reusing this type
                    RelatedEntityId = listing.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(adminNotification);
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

        

        #region Browse and Search

        [HttpGet]
        public async Task<IActionResult> Index(string category = null, string subcategory = null, string searchTerm = null, int page = 1)
        {
            int pageSize = 12; // Number of listings per page

            // Start with all approved listings
            var query = _context.Listings
                .Where(l => l.Status == ListingStatus.Approved)
                .Include(l => l.Images)
                .Include(l => l.Seller)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(l => l.Category == category);
            }

            if (!string.IsNullOrEmpty(subcategory))
            {
                query = query.Where(l => l.SubCategory == subcategory);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(l =>
                    l.Title.Contains(searchTerm) ||
                    l.Description.Contains(searchTerm) ||
                    l.Category.Contains(searchTerm) ||
                    l.SubCategory.Contains(searchTerm)
                );
            }

            // Get total count for pagination
            var totalItems = await query.CountAsync();

            // Calculate pagination
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages));

            // Get page of listings
            var listings = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create view model
            var viewModel = new ListingBrowseViewModel
            {
                Listings = listings,
                CurrentPage = page,
                TotalPages = totalPages,
                Category = category,
                SubCategory = subcategory,
                SearchTerm = searchTerm
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var listing = await _context.Listings
                .Include(l => l.Images)
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null)
            {
                return NotFound();
            }

            // If not approved and current user is not the seller or an admin, return not found
            var userId = HttpContext.Session.GetInt32("UserId");
            var isAdmin = HttpContext.Session.GetInt32("AdminId") != null;

            if (listing.Status != ListingStatus.Approved &&
                userId != listing.SellerId &&
                !isAdmin)
            {
                return NotFound();
            }

            // Get product-specific details
            dynamic product = null;

            if (!string.IsNullOrEmpty(listing.SubCategory))
            {
                switch (listing.SubCategory)
                {
                    case "IOS Phone":
                        product = await _context.IOSPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Android Phone":
                        product = await _context.AndroidPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Laptops":
                        product = await _context.Laptops.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                        // Add cases for other product types
                }
            }

            // Check if user has bookmarked this listing
            bool isBookmarked = false;
            if (userId.HasValue)
            {
                isBookmarked = await _context.Bookmarks
                    .AnyAsync(b => b.UserId == userId.Value && b.ListingId == id);
            }

            // Get related listings (same category)
            var relatedListings = await _context.Listings
                .Where(l => l.Status == ListingStatus.Approved &&
                            l.Id != id &&
                            l.Category == listing.Category)
                .Include(l => l.Images)
                .OrderByDescending(l => l.CreatedAt)
                .Take(4)
                .ToListAsync();

            // Create view model
            var viewModel = new ListingDetailsViewModel
            {
                Listing = listing,
                Product = product,
                IsBookmarked = isBookmarked,
                RelatedListings = relatedListings,
                IsOwner = userId.HasValue && userId.Value == listing.SellerId
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return RedirectToAction("Index");
            }

            var results = await _context.Listings
                .Where(l => l.Status == ListingStatus.Approved &&
                          (l.Title.Contains(query) ||
                           l.Description.Contains(query) ||
                           l.Category.Contains(query) ||
                           l.SubCategory.Contains(query)))
                .Include(l => l.Images)
                .OrderByDescending(l => l.CreatedAt)
                .Take(20) // Limit results
                .ToListAsync();

            return View(results);
        }

        #endregion

        #region User Interactions

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

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> GetOffers(int listingId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Please log in to view offers." });
            }
           


            try
            {
                var listing = await _context.Listings
                    .FirstOrDefaultAsync(l => l.Id == listingId && l.SellerId == userId.Value);

                if (listing == null)
                {
                    return Json(new { success = false, message = "Listing not found or you don't have permission to view these offers." });
                }

                var offers = await _context.Offers
                    .Include(o => o.Buyer)
                    .Where(o => o.ListingId == listingId)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new
                    {
                        Id = o.Id,
                        BuyerId = o.BuyerId,
                        BuyerName = $"{o.Buyer.FirstName} {o.Buyer.LastName}",
                        OfferAmount = o.OfferAmount,
                        Message = o.Message,
                        Status = o.Status.ToString(),
                        CreatedAt = o.CreatedAt
                    })
                    .ToListAsync();

                return Json(new { success = true, offers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error retrieving offers: {ex.Message}" });
            }
        }

        #endregion
    }
}