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
using System.Reflection;
using System.Threading.Tasks;
using System.Globalization;


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
        public async Task<IActionResult> ApproveListing(int id)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing == null) return NotFound();

            listing.Status = ListingStatus.Active;
            await _context.SaveChangesAsync();

            return RedirectToAction("PendingListings", "Admin");
        }

        [HttpPost]
        public async Task<IActionResult> RejectListing(int id, string reason)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing == null) return NotFound();

            listing.RejectionReason = reason;
            _context.Listings.Remove(listing);
            await _context.SaveChangesAsync();

            return RedirectToAction("PendingListings", "Admin");
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> CheckSecondHandOffer(int offerId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                // Get the offer with listing
                var offer = await _context.Offers
                    .Include(o => o.Listing)
                    .FirstOrDefaultAsync(o => o.Id == offerId);

                if (offer == null)
                {
                    return Json(new { success = false, message = "Offer not found" });
                }

                // Check if this is a second-hand listing
                bool isSecondHand = offer.Listing.Condition == "Second-Hand";

                // Get purchase info if it exists
                var purchase = await _context.Purchases
                    .Include(p => p.Confirmation)
                    .FirstOrDefaultAsync(p => p.OfferId == offerId);

                bool shippingConfirmed = false;
                bool receiptConfirmed = false;

                if (purchase != null && purchase.Confirmation != null)
                {
                    shippingConfirmed = purchase.Confirmation.SellerShippedProduct;
                    receiptConfirmed = purchase.Confirmation.BuyerReceivedProduct;
                }

                return Json(new
                {
                    success = true,
                    isSecondHand = isSecondHand,
                    purchaseId = purchase?.Id,
                    sellerId = offer.Listing.SellerId,
                    buyerId = offer.BuyerId,
                    shippingConfirmed = shippingConfirmed,
                    receiptConfirmed = receiptConfirmed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking second-hand offer");
                return Json(new { success = false, message = "An error occurred" });
            }
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

            // Basic validation for required fields
            var isValid = true;

            if (string.IsNullOrEmpty(model.Title))
            {
                ModelState.AddModelError("Title", "Title is required");
                isValid = false;
            }

            if (model.Price <= 0)
            {
                ModelState.AddModelError("Price", "Price must be greater than 0");
                isValid = false;
            }

            if (string.IsNullOrEmpty(model.Description))
            {
                ModelState.AddModelError("Description", "Description is required");
                isValid = false;
            }

            if (string.IsNullOrEmpty(model.Condition))
            {
                ModelState.AddModelError("Condition", "Condition is required");
                isValid = false;
            }

            // Stock validation for first-hand listings
            if (model.Condition == "First-Hand" && (!model.Stock.HasValue || model.Stock.Value <= 0))
            {
                ModelState.AddModelError("Stock", "Stock quantity is required for first-hand listings");
                isValid = false;
            }

            if (string.IsNullOrEmpty(model.Category))
            {
                ModelState.AddModelError("Category", "Category is required");
                isValid = false;
            }

            if (string.IsNullOrEmpty(model.Location))
            {
                ModelState.AddModelError("Location", "Location is required");
                isValid = false;
            }

            // Image validation - FIX: Only validate if both collections are empty
            if ((images == null || images.Count == 0) &&
                (model.Images == null || model.Images.Count == 0))
            {
                ModelState.AddModelError("Images", "At least one image is required");
                isValid = false;
            }

            if (!isValid)
            {
                _logger.LogWarning("Model validation failed: {@ModelState}", ModelState);

                // Pass the selected values back to the view
                ViewBag.SelectedCondition = model.Condition;
                ViewBag.SelectedCategory = model.Category;
                ViewBag.SelectedSubCategory = model.SubCategory;
                ViewBag.SelectedDetailCategory = model.DetailCategory;

                // Return to the create view with the model to retain form data
                return View("~/Views/User/MyProfilePage.cshtml", model);
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
                    // Add stock for first-hand items
                    Stock = model.Condition == "First-Hand" ? model.Stock : null,
                    Category = model.Category,
                    SubCategory = model.SubCategory,
                    // Improved handling of DetailCategory
                    DetailCategory = (model.DetailCategory == null ||
                  model.DetailCategory == "undefined" ||
                  string.IsNullOrEmpty(model.DetailCategory)) ? string.Empty
                  : model.DetailCategory,
                    RejectionReason = null,
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
                        var product = ProductFactory.CreateProduct(model.Category, model.SubCategory, model.DetailCategory);
                        if (product != null)
                        {
                            product.ListingId = listing.Id;

                            // Modify the section in CreateListing method that handles setting dynamic properties
                            // around line 153 in ListingController.cs

                            // Inside the if(product != null) block, replace the existing DynamicProperties section with this:

                            if (model.DynamicProperties != null)
                            {
                                foreach (var prop in model.DynamicProperties)
                                {
                                    // Find property with case-insensitive comparison
                                    PropertyInfo productProp = null;
                                    foreach (var p in product.GetType().GetProperties())
                                    {
                                        if (string.Equals(p.Name, prop.Key, StringComparison.OrdinalIgnoreCase))
                                        {
                                            productProp = p;
                                            break;
                                        }
                                    }

                                    if (productProp != null)
                                    {
                                        try
                                        {
                                            // Skip if value is empty
                                            if (string.IsNullOrWhiteSpace(prop.Value))
                                                continue;

                                            // Handle different property types
                                            if (productProp.PropertyType == typeof(bool))
                                            {
                                                bool boolValue = prop.Value.ToLower() == "yes" || prop.Value.ToLower() == "true";
                                                productProp.SetValue(product, boolValue);
                                                _logger.LogInformation("Set bool property {PropertyName} to {Value}", productProp.Name, boolValue);
                                            }
                                            else if (productProp.PropertyType == typeof(int))
                                            {
                                                if (int.TryParse(prop.Value, out int intValue))
                                                {
                                                    productProp.SetValue(product, intValue);
                                                    _logger.LogInformation("Set int property {PropertyName} to {Value}", productProp.Name, intValue);
                                                }
                                            }
                                            else if (productProp.PropertyType == typeof(decimal))
                                            {
                                                if (decimal.TryParse(prop.Value, out decimal decimalValue))
                                                {
                                                    productProp.SetValue(product, decimalValue);
                                                    _logger.LogInformation("Set decimal property {PropertyName} to {Value}", productProp.Name, decimalValue);
                                                }
                                            }
                                            else
                                            {
                                                productProp.SetValue(product, prop.Value);
                                                _logger.LogInformation("Set string property {PropertyName} to {Value}", productProp.Name, prop.Value);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            // Log the error but continue
                                            _logger.LogError(ex, "Error setting property {PropertyName}: {ErrorMessage}", productProp.Name, ex.Message);
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Property {PropertyName} not found on product type {ProductType}",
                                            prop.Key, product.GetType().Name);
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
                return View("~/Views/User/MyProfilePage.cshtml", model);
            }
        }

        // Rest of the controller methods remain unchanged
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
            // Fetch product-specific properties
            var dynamicProperties = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(listing.SubCategory))
            {
                object product = null;
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
                    case "Spare Parts":
                        product = await _context.SpareParts.FirstOrDefaultAsync(p => p.ListingId == id);
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
                    case "Computer Components":
                        product = await _context.ComputerComponents.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Monitors":
                        product = await _context.Monitors.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;

                    // White goods
                    case "Washers":
                        product = await _context.Washers.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Dishwashers":
                        product = await _context.Dishwashers.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Fridges":
                        product = await _context.Fridges.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Ovens":
                        product = await _context.Ovens.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Stoves":
                        product = await _context.Stoves.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Microwave Ovens":
                        product = await _context.MicrowaveOvens.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;

                    // Small appliances
                    case "Vacuum Cleaner":
                        product = await _context.VacuumCleaners.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Beverage Preparation":
                        product = await _context.BeveragePreparations.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Food Preparation":
                        product = await _context.FoodPreparations.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Iron":
                        product = await _context.Irons.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Sewing Machine":
                        product = await _context.SewingMachines.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;

                    // Electronics & accessories
                    case "Televisions":
                        product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Heating & Cooling":
                        product = await _context.HeatingCoolings.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Cameras":
                        product = await _context.Cameras.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;

                    // Computer‑peripheral sub‑items
                    case "Keyboards":
                        product = await _context.Keyboards.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Speakers":
                        product = await _context.Speakers.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Headphones & Earphones":
                        product = await _context.HeadphonesEarphones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Webcams":
                        product = await _context.Webcams.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Microphones":
                        product = await _context.Microphones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Mouse":
                        product = await _context.Mouses.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Computer Bags":
                        product = await _context.ComputerBags.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;

                    // General fallback (if any final top‑level category exists)

                    // Default – not recognised
                    default:
                        product = null;           // log or handle as needed
                        break;
                }

                // If product is found, extract properties using reflection
                if (product != null)
                {
                    // Log for debugging
                    _logger.LogInformation("Found product of type {ProductType} for listing {ListingId}", product.GetType().Name, id);

                    foreach (var prop in product.GetType().GetProperties())
                    {
                        // Skip navigation or system properties
                        if (prop.Name == "Id" || prop.Name == "ListingId" || prop.Name == "Listing")
                            continue;

                        var value = prop.GetValue(product);
                        if (value != null)
                        {
                            if (prop.PropertyType == typeof(bool))
                            {
                                dynamicProperties[prop.Name] = value.ToString();
                            }
                            else
                            {
                                dynamicProperties[prop.Name] = value.ToString();
                            }

                            _logger.LogInformation("Added property {PropertyName} with value {Value}", prop.Name, value);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("No product found for subcategory '{SubCategory}' and listing ID {ListingId}", listing.SubCategory, id);
                }
            }

            model.DynamicProperties = dynamicProperties;
            return View(model);
        }
        [HttpPost]
        [UserAuthenticationFilter]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                // Find the image and check if it belongs to the user's listing
                var image = await _context.ListingImages
                    .Include(i => i.Listing)
                    .FirstOrDefaultAsync(i => i.Id == imageId);

                if (image == null)
                {
                    return Json(new { success = false, message = "Image not found" });
                }

                if (image.Listing.SellerId != userId)
                {
                    return Json(new { success = false, message = "You don't have permission to delete this image" });
                }

                // Check if this is the only image for the listing
                var imageCount = await _context.ListingImages.CountAsync(i => i.ListingId == image.ListingId);
                if (imageCount <= 1)
                {
                    return Json(new { success = false, message = "Cannot delete the only image. Please upload a new image first." });
                }

                // If this is the main image, set another image as main
                if (image.IsMainImage)
                {
                    var newMainImage = await _context.ListingImages
                        .FirstOrDefaultAsync(i => i.ListingId == image.ListingId && i.Id != imageId);

                    if (newMainImage != null)
                    {
                        newMainImage.IsMainImage = true;
                    }
                }

                // Delete the image file from disk
                string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string imagePath = image.ImagePath.TrimStart('/');
                string fullPath = Path.Combine(webRootPath, imagePath);

                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        System.IO.File.Delete(fullPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting image file: {FilePath}", fullPath);
                        // Continue with DB deletion even if file deletion fails
                    }
                }

                // Remove the image from the database
                _context.ListingImages.Remove(image);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Image deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image: {ErrorMessage}", ex.Message);
                return Json(new { success = false, message = "An error occurred while deleting the image" });
            }
        }

        // Update the EditListing POST method in ListingController.cs
        // Updated EditListing POST method in ListingController.cs

        // Updated EditListing POST method in ListingController.cs
        [HttpPost]
        [UserAuthenticationFilter]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditListing(ListingViewModel model, List<IFormFile> newImages, List<int> DeletedImageIds)
        {
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

            // Ensure DeletedImageIds is not null
            if (DeletedImageIds == null)
            {
                DeletedImageIds = new List<int>();
            }

            // Ensure DetailCategory is never null
            if (model.DetailCategory == null)
            {
                model.DetailCategory = string.Empty;
            }

            // Check if listing is being edited - store original status
            var originalStatus = listing.Status;
            var wasApproved = listing.Status == ListingStatus.Active;

            // Validate images
            int remainingImagesCount = listing.Images.Count(i => !DeletedImageIds.Contains(i.Id));
            bool willHaveImages = remainingImagesCount > 0 || (newImages != null && newImages.Count > 0);

            if (!willHaveImages)
            {
                ModelState.AddModelError("Images", "At least one image is required. Please upload a new image.");
                model.Images = listing.Images.ToList();
                if (!string.IsNullOrEmpty(listing.SubCategory))
                {
                    model.DynamicProperties = await GetDynamicPropertiesAsync(listing.Id, listing.SubCategory);
                }
                return View(model);
            }

            try
            {
                // Update listing properties
                listing.Title = model.Title;
                listing.Price = model.Price;
                listing.Description = model.Description;
                listing.Location = model.Location;
                listing.DetailCategory = model.DetailCategory;
                listing.UpdatedAt = DateTime.UtcNow;

                // If listing was previously approved or rejected, reset to pending for admin review
                if (listing.Status == ListingStatus.Active || listing.Status == ListingStatus.Rejected)
                {
                    listing.Status = ListingStatus.Pending;
                    listing.RejectionReason = null;
                }

                // Handle deleted images
                if (DeletedImageIds.Count > 0)
                {
                    var imagesToDelete = listing.Images.Where(i => DeletedImageIds.Contains(i.Id)).ToList();

                    foreach (var imageToDelete in imagesToDelete)
                    {
                        // If this was the main image, set a new main image
                        if (imageToDelete.IsMainImage && listing.Images.Count > DeletedImageIds.Count)
                        {
                            var newMainImage = listing.Images.FirstOrDefault(i => !DeletedImageIds.Contains(i.Id));
                            if (newMainImage != null)
                            {
                                newMainImage.IsMainImage = true;
                            }
                        }

                        // Delete the image file from disk
                        string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        string imagePath = imageToDelete.ImagePath.TrimStart('/');
                        string fullPath = Path.Combine(webRootPath, imagePath);

                        if (System.IO.File.Exists(fullPath))
                        {
                            try
                            {
                                System.IO.File.Delete(fullPath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error deleting image file: {FilePath}", fullPath);
                            }
                        }

                        _context.ListingImages.Remove(imageToDelete);
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

                    bool needsMainImage = !listing.Images.Any(i => i.IsMainImage && !DeletedImageIds.Contains(i.Id));

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
                                IsMainImage = needsMainImage
                            };

                            _context.ListingImages.Add(listingImage);
                            _logger.LogInformation("New image added for listing {ListingId}: {ImagePath}", listing.Id, listingImage.ImagePath);

                            needsMainImage = false;
                        }
                    }
                }

                // Ensure at least one image is marked as main
                var anyMainImage = await _context.ListingImages
                    .AnyAsync(i => i.ListingId == listing.Id && !DeletedImageIds.Contains(i.Id) && i.IsMainImage);

                if (!anyMainImage)
                {
                    var firstRemainingImage = await _context.ListingImages
                        .FirstOrDefaultAsync(i => i.ListingId == listing.Id && !DeletedImageIds.Contains(i.Id));

                    if (firstRemainingImage != null)
                    {
                        firstRemainingImage.IsMainImage = true;
                    }
                }

                // Update product-specific properties if applicable
                if (model.DynamicProperties != null && !string.IsNullOrEmpty(listing.SubCategory))
                {
                    await UpdateDynamicPropertiesAsync(listing.Id, listing.SubCategory, model.DynamicProperties);
                }

                // Create notifications
                //if (wasApproved)
                //{
                //    // Notify admins that an approved listing was edited and needs review
                //    var adminUserId = await _context.Admins
                //                .Select(a => a.Id)   // Admin row → UserId
                //                .FirstOrDefaultAsync();

                //    if (adminUserId != 0)      // adjust type if Guid/int
                //    {
                //        var adminNotification = new Notification
                //        {
                //            UserId = adminUserId,                   // ✅ guaranteed to exist in Users
                //            Message = $"Updated listing '{model.Title}' needs review",
                //            Type = NotificationType.ListingApproved,
                //            RelatedEntityId = listing.Id,
                //            CreatedAt = DateTime.UtcNow
                //        };

                //        _context.Notifications.Add(adminNotification);
                //    }
                //}

                // Notify user about the status change
                if (originalStatus != listing.Status)
                {
                    var userNotification = new Notification
                    {
                        UserId = userId.Value,
                        Message = $"Your listing '{model.Title}' has been updated and is now pending review.",
                        Type = NotificationType.ListingUpdated,
                        RelatedEntityId = listing.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(userNotification);
                }

                await _context.SaveChangesAsync();

                if (wasApproved)
                {
                    TempData["SuccessMessage"] = "Your listing has been updated and is now pending review by administrators.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Your listing has been updated successfully.";
                }

                return RedirectToAction("MyListings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating listing: {ErrorMessage}", ex.Message);
                ModelState.AddModelError("", $"Error updating listing: {ex.Message}");

                model.Images = listing.Images.ToList();
                if (!string.IsNullOrEmpty(listing.SubCategory))
                {
                    model.DynamicProperties = await GetDynamicPropertiesAsync(listing.Id, listing.SubCategory);
                }

                return View(model);
            }
        }

        // Helper method to get dynamic properties for a specific product
        private async Task<Dictionary<string, string>> GetDynamicPropertiesAsync(int listingId, string subcategory)
        {
            var result = new Dictionary<string, string>();

            dynamic product = null;

            // Get the appropriate product based on subcategory
            switch (subcategory)
            {
                case "IOS Phone":
                    product = await _context.IOSPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Android Phone":
                    product = await _context.AndroidPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Laptops":
                    product = await _context.Laptops.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Desktops":
                    product = await _context.Desktops.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "IOS Tablets":
                case "Android Tablets":
                case "Other Tablets":
                case "Tablets":
                    // Check all tablet types
                    product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    if (product == null)
                        product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    if (product == null)
                        product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Washers":
                    product = await _context.Washers.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Dishwashers":
                    product = await _context.Dishwashers.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Fridges":
                    product = await _context.Fridges.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Ovens":
                    product = await _context.Ovens.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Vacuum Cleaner":
                    product = await _context.VacuumCleaners.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Televisions":
                    product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
            }

            // If product is found, extract properties
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
                        result[prop.Name] = value.ToString();
                    }
                }
            }

            return result;
        }

        // Helper method to update dynamic properties for a specific product
        private async Task UpdateDynamicPropertiesAsync(int listingId, string subcategory, Dictionary<string, string> properties)
        {
            dynamic product = null;

            // Get the appropriate product based on subcategory
            switch (subcategory)
            {
                case "IOS Phone":
                    product = await _context.IOSPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Android Phone":
                    product = await _context.AndroidPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Laptops":
                    product = await _context.Laptops.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Desktops":
                    product = await _context.Desktops.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "IOS Tablets":
                case "Android Tablets":
                case "Other Tablets":
                case "Tablets":
                    // Check all tablet types
                    product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    if (product == null)
                        product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    if (product == null)
                        product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Washers":
                    product = await _context.Washers.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Dishwashers":
                    product = await _context.Dishwashers.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Fridges":
                    product = await _context.Fridges.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Ovens":
                    product = await _context.Ovens.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Vacuum Cleaner":
                    product = await _context.VacuumCleaners.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
                case "Televisions":
                    product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == listingId);
                    break;
            }

            // Update product properties if product exists
            if (product != null)
            {
                foreach (var prop in properties)
                {
                    // Find property with case-insensitive comparison
                    PropertyInfo productProp = null;
                    foreach (var p in product.GetType().GetProperties())
                    {
                        if (string.Equals(p.Name, prop.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            productProp = p;
                            break;
                        }
                    }

                    if (productProp != null)
                    {
                        try
                        {
                            // Skip if value is empty
                            if (string.IsNullOrWhiteSpace(prop.Value))
                                continue;

                            // Convert value to appropriate type
                            if (productProp.PropertyType == typeof(bool))
                            {
                                bool boolValue = prop.Value.ToLower() == "yes" || prop.Value.ToLower() == "true";
                                productProp.SetValue(product, boolValue);
                            }
                            else if (productProp.PropertyType == typeof(int))
                            {
                                if (int.TryParse(prop.Value, out int intValue))
                                {
                                    productProp.SetValue(product, intValue);
                                }
                            }
                            else if (productProp.PropertyType == typeof(decimal))
                            {
                                if (decimal.TryParse(prop.Value, out decimal decimalValue))
                                {
                                    productProp.SetValue(product, decimalValue);
                                }
                            }
                            else
                            {
                                productProp.SetValue(product, prop.Value);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log error but continue
                            _logger.LogError(ex, "Error updating property {PropertyName}: {ErrorMessage}",
                                productProp.Name, ex.Message);
                        }
                    }
                }
            }
        }

        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteListing(int id)
{
    var listing = await _context.Listings.FindAsync(id);
    if (listing == null)
        return Json(new { success = false, message = "Listing not found." });

    _context.Listings.Remove(listing);
    await _context.SaveChangesAsync();

    return Json(new { success = true });
}


        

        #region Browse and Search

        [HttpGet]
        public async Task<IActionResult> Index(string category = null, string subcategory = null, string searchTerm = null, int page = 1)
        {
            int pageSize = 12; // Number of listings per page

            // Start with all approved listings
            var query = _context.Listings
                .Where(l => l.Status == ListingStatus.Active)
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
                    l.SubCategory.Contains(searchTerm) ||
                    l.Condition.Contains(searchTerm)
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
                .Include(l => l.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null)
            {
                return NotFound();
            }

            // If not approved and current user is not the seller or an admin, return not found
            var userId = HttpContext.Session.GetInt32("UserId");
            var isAdmin = HttpContext.Session.GetInt32("AdminId") != null;

            if (listing.Status != ListingStatus.Active &&
                userId != listing.SellerId &&
                !isAdmin)
            {
                return NotFound();
            }

            // Get product-specific details (existing code)
            dynamic product = null;
            if (!string.IsNullOrEmpty(listing.DetailCategory))
            {
                switch (listing.DetailCategory)
                {
                    case "Laptops":
                    case "Laptop":
                        product = await _context.Laptops.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Desktop":
                    case "Desktops":
                        product = await _context.Desktops.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Computer Accessory":
                    case "Computer Accesories":
                        product = await _context.ComputerAccessories.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Computer Component":
                    case "Computer Components":
                        product = await _context.ComputerComponents.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Monitor":
                    case "Monitors":
                        product = await _context.Monitors.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Fridge":
                    case "Fridges":
                        product = await _context.Fridges.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Washer":
                    case "Washers":
                        product = await _context.Washers.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Stove":
                    case "Stoves":
                        product = await _context.Stoves.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Microwave Oven":
                    case "Microwave Ovens":
                        product = await _context.MicrowaveOvens.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Dishwasher":
                    case "Diswashers":
                        product = await _context.Dishwashers.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Oven":
                    case "Ovens":
                        product = await _context.Ovens.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Vacuum Cleaner":
                    case "Vacuum Cleaners":
                        product = await _context.VacuumCleaners.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Beverage Preparation":
                    case "Beverage Preparations":
                        product = await _context.BeveragePreparations.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Food Preparation":
                    case "Food Preparations":
                        product = await _context.FoodPreparations.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Iron":
                    case "Irons":
                        product = await _context.Irons.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Sewing Machine":
                    case "Sewing Machines":
                        product = await _context.SewingMachines.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Keyboard":
                    case "Keyboards":
                        product = await _context.Keyboards.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Speaker":
                    case "Speakers":
                        product = await _context.Speakers.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Headphone & Earphone":
                    case "Headphone & Earphones":
                        product = await _context.HeadphonesEarphones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Webcam":
                    case "Webcams":
                        product = await _context.Webcams.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Microphone":
                    case "Microphones":
                        product = await _context.Microphones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Mouse":
                    case "Mouses":
                        product = await _context.Mouses.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Computer Bag":
                    case "Computer Bags":
                        product = await _context.ComputerBags.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                }
            }
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
                    case "Other Phone":
                        product = await _context.OtherPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Spare Part":
                        product = await _context.SpareParts.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Phone Accessory":
                        product = await _context.PhoneAccessories.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "IOS Tablet":
                        product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Android Tablet":
                        product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Other Tablet":
                        product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Tablet Accessory":
                        product = await _context.TabletAccessories.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Television":
                        product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Heating & Cooling":
                        product = await _context.HeatingCoolings.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Camera":
                        product = await _context.Cameras.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                }
            }

            // Check if user has bookmarked this listing
            bool isBookmarked = false;
            if (userId.HasValue)
            {
                isBookmarked = await _context.Bookmarks
                    .AnyAsync(b => b.UserId == userId.Value && b.ListingId == id);
            }

            // Get related listings (existing code)
            var relatedListings = await _context.Listings
                .Where(l => l.Status == ListingStatus.Active &&
                            l.Id != id &&
                            l.Category == listing.Category)
                .Include(l => l.Images)
                .OrderByDescending(l => l.CreatedAt)
                .Take(4)
                .ToListAsync();

            // Get active offers if this is a second-hand listing (existing code)
            List<Offer> activeOffers = new List<Offer>();
            if (listing.Condition == "Second-Hand" && userId.HasValue && userId.Value == listing.SellerId)
            {
                activeOffers = await _context.Offers
                    .Include(o => o.Buyer)
                    .Where(o => o.ListingId == id && o.Status == OfferStatus.Pending)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }

            // Check if user can review this product
            bool canReview = false;
            bool hasReviewed = false;

            if (userId.HasValue && listing.Condition == "First-Hand")
            {
                // Check if user has purchased and received this product
                var hasPurchased = await _context.Purchases
                    .Include(p => p.Confirmation)
                    .AnyAsync(p => p.BuyerId == userId.Value &&
                                  p.ListingId == id &&
                                  p.PaymentStatus == PaymentStatus.Completed &&
                                  p.Confirmation != null &&
                                  p.Confirmation.BuyerReceivedProduct);

                if (hasPurchased)
                {
                    // Check if user has already reviewed
                    hasReviewed = await _context.ProductReviews
                        .AnyAsync(r => r.UserId == userId.Value && r.ListingId == id);

                    canReview = !hasReviewed;
                }
            }

            // Calculate average rating and total reviews
            var reviews = listing.Reviews?.ToList() ?? new List<ProductReview>();
            double averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            int totalReviews = reviews.Count;

            // Create view model
            var viewModel = new ListingDetailsViewModel
            {
                Listing = listing,
                Product = product,
                IsBookmarked = isBookmarked,
                RelatedListings = relatedListings,
                IsOwner = userId.HasValue && userId.Value == listing.SellerId,
                ActiveOffers = activeOffers,
                Reviews = reviews,
                AverageRating = Math.Round(averageRating, 1),
                TotalReviews = totalReviews,
                CanReview = canReview,
                HasReviewed = hasReviewed
            };

            return View(viewModel);
        }
        // Add this method to your ListingController.cs file
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (string.IsNullOrEmpty(query))
            {
                if (userId == null) { 
                    return RedirectToAction("Index", "Home"); 
                }else
                {
                    return RedirectToAction("User_MainPage", "User");
                }
            }

            ViewBag.SearchTerm = query;

            // Check if search input is just a number without # prefix
            if (System.Text.RegularExpressions.Regex.IsMatch(query, @"^\d+$"))
            {
                // Set ViewBag to show message about using # for ID searches
                ViewBag.ShowIdFormatMessage = true;

                // We'll still search for this number in titles and descriptions
                var numberResults = await _context.Listings
                    .Where(l => l.Status == ListingStatus.Active &&
                          (l.Title.Contains(query) ||
                           l.Description.Contains(query)))
                    .Include(l => l.Images)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToListAsync();

                return View("~/Views/Home/Search_Page.cshtml", numberResults);
            }

            // Check if it's an ID search (starts with #)
            if (query.StartsWith("#") && query.Length > 1)
            {
                // Try to parse the ID
                if (int.TryParse(query.Substring(1), out int listingId))
                {
                    var listing = await _context.Listings
                        .Include(l => l.Images)
                        .FirstOrDefaultAsync(l => l.Id == listingId && l.Status == ListingStatus.Active);

                    if (listing != null)
                    {
                        // Return a list with just one item
                        return View("~/Views/Home/Search_Page.cshtml", new List<Listing> { listing });
                    }
                }
            }

            // For regular search, look for matches in title, category, subcategory, or detail category
            var results = await _context.Listings
                .Where(l => l.Status == ListingStatus.Active &&
                      (l.Title.Contains(query) ||
                       l.Category.Contains(query) ||
                       l.SubCategory.Contains(query) ||
                       l.DetailCategory.Contains(query) ||
                       l.Description.Contains(query)))
                .Include(l => l.Images)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View("~/Views/Home/Search_Page.cshtml", results);
        }
        [HttpGet]
        public async Task<IActionResult> Guest_Listing_Details(int id)
        {
            var listing = await _context.Listings
                .Include(l => l.Images)
                .Include(l => l.Seller)
                .Include(l => l.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null)
            {
                return NotFound();
            }

            // If not approved and current user is not the seller or an admin, return not found
            var userId = HttpContext.Session.GetInt32("UserId");
            var isAdmin = HttpContext.Session.GetInt32("AdminId") != null;

            if (listing.Status != ListingStatus.Active &&
                userId != listing.SellerId &&
                !isAdmin)
            {
                return NotFound();
            }

            // Get product-specific details (existing code)
            dynamic product = null;
            if (!string.IsNullOrEmpty(listing.DetailCategory))
            {
                switch (listing.DetailCategory)
                {
                    case "Laptops":
                    case "Laptop":
                        product = await _context.Laptops.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Desktop":
                    case "Desktops":
                        product = await _context.Desktops.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Computer Accessory":
                    case "Computer Accesories":
                        product = await _context.ComputerAccessories.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Computer Component":
                    case "Computer Components":
                        product = await _context.ComputerComponents.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Monitor":
                    case "Monitors":
                        product = await _context.Monitors.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Fridge":
                    case "Fridges":
                        product = await _context.Fridges.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Washer":
                    case "Washers":
                        product = await _context.Washers.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Stove":
                    case "Stoves":
                        product = await _context.Stoves.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Microwave Oven":
                    case "Microwave Ovens":
                        product = await _context.MicrowaveOvens.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Dishwasher":
                    case "Diswashers":
                        product = await _context.Dishwashers.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Oven":
                    case "Ovens":
                        product = await _context.Ovens.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Vacuum Cleaner":
                    case "Vacuum Cleaners":
                        product = await _context.VacuumCleaners.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Beverage Preparation":
                    case "Beverage Preparations":
                        product = await _context.BeveragePreparations.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Food Preparation":
                    case "Food Preparations":
                        product = await _context.FoodPreparations.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Iron":
                    case "Irons":
                        product = await _context.Irons.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Sewing Machine":
                    case "Sewing Machines":
                        product = await _context.SewingMachines.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Keyboard":
                    case "Keyboards":
                        product = await _context.Keyboards.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Speaker":
                    case "Speakers":
                        product = await _context.Speakers.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Headphone & Earphone":
                    case "Headphone & Earphones":
                        product = await _context.HeadphonesEarphones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Webcam":
                    case "Webcams":
                        product = await _context.Webcams.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Microphone":
                    case "Microphones":
                        product = await _context.Microphones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Mouse":
                    case "Mouses":
                        product = await _context.Mouses.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Computer Bag":
                    case "Computer Bags":
                        product = await _context.ComputerBags.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                }
            }
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
                    case "Other Phone":
                        product = await _context.OtherPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Spare Part":
                        product = await _context.SpareParts.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Phone Accessory":
                        product = await _context.PhoneAccessories.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "IOS Tablet":
                        product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Android Tablet":
                        product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Other Tablet":
                        product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Tablet Accessory":
                        product = await _context.TabletAccessories.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Television":
                        product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Heating & Cooling":
                        product = await _context.HeatingCoolings.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Camera":
                        product = await _context.Cameras.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                }
            }

            // Check if user has bookmarked this listing
            bool isBookmarked = false;
            if (userId.HasValue)
            {
                isBookmarked = await _context.Bookmarks
                    .AnyAsync(b => b.UserId == userId.Value && b.ListingId == id);
            }

            // Get related listings (existing code)
            var relatedListings = await _context.Listings
                .Where(l => l.Status == ListingStatus.Active &&
                            l.Id != id &&
                            l.Category == listing.Category)
                .Include(l => l.Images)
                .OrderByDescending(l => l.CreatedAt)
                .Take(4)
                .ToListAsync();

            // Get active offers if this is a second-hand listing (existing code)
            List<Offer> activeOffers = new List<Offer>();
            if (listing.Condition == "Second-Hand" && userId.HasValue && userId.Value == listing.SellerId)
            {
                activeOffers = await _context.Offers
                    .Include(o => o.Buyer)
                    .Where(o => o.ListingId == id && o.Status == OfferStatus.Pending)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }

            // Check if user can review this product
            bool canReview = false;
            bool hasReviewed = false;

            if (userId.HasValue && listing.Condition == "First-Hand")
            {
                // Check if user has purchased and received this product
                var hasPurchased = await _context.Purchases
                    .Include(p => p.Confirmation)
                    .AnyAsync(p => p.BuyerId == userId.Value &&
                                  p.ListingId == id &&
                                  p.PaymentStatus == PaymentStatus.Completed &&
                                  p.Confirmation != null &&
                                  p.Confirmation.BuyerReceivedProduct);

                if (hasPurchased)
                {
                    // Check if user has already reviewed
                    hasReviewed = await _context.ProductReviews
                        .AnyAsync(r => r.UserId == userId.Value && r.ListingId == id);

                    canReview = !hasReviewed;
                }
            }

            // Calculate average rating and total reviews
            var reviews = listing.Reviews?.ToList() ?? new List<ProductReview>();
            double averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            int totalReviews = reviews.Count;

            // Create view model
            var viewModel = new ListingDetailsViewModel
            {
                Listing = listing,
                Product = product,
                IsBookmarked = isBookmarked,
                RelatedListings = relatedListings,
                IsOwner = userId.HasValue && userId.Value == listing.SellerId,
                ActiveOffers = activeOffers,
                Reviews = reviews,
                AverageRating = Math.Round(averageRating, 1),
                TotalReviews = totalReviews,
                CanReview = canReview,
                HasReviewed = hasReviewed
            };

            return View(viewModel);
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
                    return Json(new { success = true, isBookmarked = false, message = "Item removed from bookmarks." });
                }
                else
                {
                    // Check if listing exists and is available
                    var listing = await _context.Listings
                        .FirstOrDefaultAsync(l => l.Id == listingId && l.Status == Models.Enum.ListingStatus.Active);

                    if (listing == null)
                    {
                        return Json(new { success = false, message = "This listing is not available for bookmarking." });
                    }

                    // Add bookmark
                    var bookmark = new Bookmark
                    {
                        UserId = userId.Value,
                        ListingId = listingId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Bookmarks.Add(bookmark);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, isBookmarked = true, message = "Item added to bookmarks." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling bookmark for user {UserId} on listing {ListingId}", userId, listingId);
                return Json(new { success = false, message = "An error occurred while processing your request." });
            }
        }

        [HttpPost]
        [UserAuthenticationFilter]
        public async Task<IActionResult> MakeOffer([FromBody] OfferViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false, message = "Please log in to make an offer." });

            /* ---------- robust parsing of OfferAmount ---------- */
            decimal offerAmount = model.OfferAmount;

            // If normal model-binding produced 0 (or the property is 0 by default),
            // try to parse any raw string the client might have sent.
            if (offerAmount <= 0 && !string.IsNullOrWhiteSpace(model.OfferAmountRaw))
            {
                // 1) try invariant "dot" culture, 2) try current culture (comma)
                if (!decimal.TryParse(model.OfferAmountRaw, NumberStyles.Any,
                                      CultureInfo.InvariantCulture, out offerAmount))
                {
                    decimal.TryParse(model.OfferAmountRaw, NumberStyles.Any,
                                     CultureInfo.CurrentCulture, out offerAmount);
                }
            }

            if (offerAmount <= 0)
                return Json(new { success = false, message = "Offer amount must be greater than zero." });

            /* ---------- listing & ownership checks ---------- */
            var listing = await _context.Listings.FindAsync(model.ListingId);
            if (listing == null)
                return Json(new { success = false, message = "Listing not found." });

            // Verify this is a second-hand listing
            if (listing.Condition != "Second-Hand")
                return Json(new { success = false, message = "You can only make offers on second-hand listings." });

            // Verify listing is active and available
            if (listing.Status != ListingStatus.Active)
                return Json(new { success = false, message = "This listing is not available for offers." });

            if (listing.SellerId == userId.Value)
                return Json(new { success = false, message = "You cannot make an offer on your own listing." });

            /* ---------- create Offer + Notification ---------- */
            var message = new Message
            {
                SenderId = userId.Value,
                ReceiverId = listing.SellerId,
                ListingId = model.ListingId,
                Content = $"I made an offer of {offerAmount:C}" + (!string.IsNullOrEmpty(model.Message) ? $". Message: {model.Message}" : ""),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Messages.Add(message);

            await _context.SaveChangesAsync();
            var offer = new Offer
            {
                BuyerId = userId.Value,
                ListingId = model.ListingId,
                OfferAmount = offerAmount,
                Message = model.Message ?? string.Empty, // Ensure the string Message field is not null
                Status = OfferStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            _context.Offers.Add(offer);

            var notification = new Notification
            {
                UserId = listing.SellerId,
                Message = $"You have received a new offer of {offerAmount:C} for your listing '{listing.Title}'",
                Type = NotificationType.NewOffer,
                RelatedEntityId = model.ListingId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);

            // Create a message for the conversation between buyer and seller
            offer.MessageId = message.Id;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Your offer has been sent to the seller." });
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

                if (offer.Status != OfferStatus.Pending)
                {
                    return Json(new { success = false, message = "This offer has already been processed." });
                }

                if (accept)
                {
                    // Accept the offer
                    offer.Status = OfferStatus.Accepted;

                    // Create notification for the buyer
                    var acceptNotification = new Notification
                    {
                        UserId = offer.BuyerId,
                        Message = $"Your offer of {offer.OfferAmount:C} for '{offer.Listing.Title}' has been accepted. You can now proceed with the purchase.",
                        Type = NotificationType.OfferAccepted,
                        RelatedEntityId = offer.ListingId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(acceptNotification);

                    // Create a message for the conversation
                    // In the 'if (accept)' block of RespondToOffer method
                    // Create a message for the conversation
                    var acceptMessage = new Message
                    {
                        SenderId = userId.Value,
                        ReceiverId = offer.BuyerId,
                        ListingId = offer.ListingId,
                        Content = $"I've accepted your offer of {offer.OfferAmount:C}. You can now proceed with the <a href='/Payment/ProcessOfferPurchase?offerId={offer.Id}'>purchase</a>.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Messages.Add(acceptMessage);
                }
                else
                {
                    // Reject the offer
                    offer.Status = OfferStatus.Rejected;

                    // Create notification for the buyer
                    var rejectNotification = new Notification
                    {
                        UserId = offer.BuyerId,
                        Message = $"Your offer of {offer.OfferAmount:C} for '{offer.Listing.Title}' has been rejected.",
                        Type = NotificationType.OfferRejected,
                        RelatedEntityId = offer.ListingId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(rejectNotification);

                    // Create a message for the conversation
                    var rejectMessage = new Message
                    {
                        SenderId = userId.Value,
                        ReceiverId = offer.BuyerId,
                        ListingId = offer.ListingId,
                        Content = $"I've rejected your offer of {offer.OfferAmount:C}.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Messages.Add(rejectMessage);
                }

                offer.ResponseDate = DateTime.UtcNow;
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
                _logger.LogError(ex, "Error responding to offer");
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