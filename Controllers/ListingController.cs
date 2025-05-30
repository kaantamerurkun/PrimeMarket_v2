﻿using Microsoft.AspNetCore.Mvc;
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
using Microsoft.Build.Tasks;
using Message = PrimeMarket.Models.Message;


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
                var offer = await _context.Offers
                    .Include(o => o.Listing)
                    .FirstOrDefaultAsync(o => o.Id == offerId);

                if (offer == null)
                {
                    return Json(new { success = false, message = "Offer not found" });
                }

                bool isSecondHand = offer.Listing.Condition == "Second-Hand";

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
        [UserAuthenticationFilter] 
        public async Task<IActionResult> CreateListing(ListingViewModel model, List<IFormFile> images)
        {
            _logger.LogInformation("CreateListing called with model: {@Model}", model);

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                _logger.LogWarning("User not authenticated");
                return RedirectToAction("Login", "User");
            }

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

            if ((images == null || images.Count == 0) &&
                (model.Images == null || model.Images.Count == 0))
            {
                ModelState.AddModelError("Images", "At least one image is required");
                isValid = false;
            }

            if (!isValid)
            {
                _logger.LogWarning("Model validation failed: {@ModelState}", ModelState);

                ViewBag.SelectedCondition = model.Condition;
                ViewBag.SelectedCategory = model.Category;
                ViewBag.SelectedSubCategory = model.SubCategory;
                ViewBag.SelectedDetailCategory = model.DetailCategory;

                return View("~/Views/User/MyProfilePage.cshtml", model);
            }

            try
            {
                var listing = new Listing
                {
                    SellerId = userId.Value,
                    Title = model.Title,
                    Price = model.Price,
                    Description = model.Description,
                    Condition = model.Condition,
                    Stock = model.Condition == "First-Hand" ? model.Stock : null,
                    Category = model.Category,
                    SubCategory = (model.SubCategory == null ||
                      model.SubCategory == "undefined" ||
                      string.IsNullOrEmpty(model.SubCategory)) ? string.Empty
                      : model.SubCategory,
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


                if (model.Category == "Others" || (model.Category != null && model.Category.Equals("Others", StringComparison.OrdinalIgnoreCase)))
                {

                    var othersProduct = new Models.Products.Other
                    {
                        ListingId = listing.Id
                    };

                    _context.Others.Add(othersProduct);
                    _logger.LogInformation("Others product added for listing {ListingId}", listing.Id);
                }

                else if (!string.IsNullOrEmpty(model.Category) && !string.IsNullOrEmpty(model.SubCategory))
                {
                    try
                    {
                        var product = ProductFactory.CreateProduct(model.Category, model.SubCategory, model.DetailCategory);
                        if (product != null)
                        {
                            product.ListingId = listing.Id;


                            if (model.DynamicProperties != null)
                            {
                                foreach (var prop in model.DynamicProperties)
                                {

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

                                            if (string.IsNullOrWhiteSpace(prop.Value))
                                                continue;


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

                        _logger.LogError(ex, "Error creating product for listing {ListingId}: {ErrorMessage}", listing.Id, ex.Message);
                    }
                }


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
                                IsMainImage = isFirstImage 
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
           .Include(l => l.Seller)
           .Include(l => l.Reviews)
           .ThenInclude(r => r.User)
           .FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null)
            {
                return NotFound();
            }

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


            var dynamicProperties = new Dictionary<string, string>();


            dynamic product = null;

            if (listing.Category == "Others" || string.Equals(listing.Category, "Others", StringComparison.OrdinalIgnoreCase))
            {
                product = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == id);
            }
            else if (!string.IsNullOrEmpty(listing.DetailCategory))
            {
                switch (listing.DetailCategory)
                {
                    case "Television":
                    case "Televisions":
                        product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Laptops":
                    case "Laptop":
                        product = await _context.Laptops.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Desktop":
                    case "Desktops":
                        product = await _context.Desktops.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Computer Accessory":
                    case "Computer Accessories":
                    case "Computer Accessorys":
                        product = await _context.ComputerAccessorys.FirstOrDefaultAsync(p => p.ListingId == id);
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
                    case "Dishwashers":
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
                    case "Headphones & Earphones":
                        product = await _context.HeadphoneEarphones.FirstOrDefaultAsync(p => p.ListingId == id);
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
                    case "Others":
                        product = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "IOS Phone":
                        product = await _context.IOSPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Android Phone":
                        product = await _context.AndroidPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Other Phone":
                    case "Other Phones":
                        product = await _context.OtherPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Spare Part":
                    case "Spare Parts":
                        product = await _context.SpareParts.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Phone Accessory":
                    case "Phone Accessories":
                    case "Phone Accessorys":
                        product = await _context.PhoneAccessorys.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "IOS Tablet":
                    case "IOS Tablets":
                        product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Android Tablet":
                    case "Android Tablets":
                        product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Other Tablet":
                    case "Other Tablets":
                        product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Tablet Accessory":
                    case "Tablet Accessories":
                    case "Tablet Accessorys":
                        product = await _context.TabletAccessorys.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Heating & Cooling":
                        product = await _context.HeatingCoolings.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Camera":
                    case "Cameras":
                        product = await _context.Cameras.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                }
            }
            else if (!string.IsNullOrEmpty(listing.SubCategory))
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
                    case "Other Phones":
                        product = await _context.OtherPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Spare Part":
                    case "Spare Parts":
                        product = await _context.SpareParts.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Phone Accessory":
                    case "Phone Accessories":
                    case "Phone Accessorys":
                        product = await _context.PhoneAccessorys.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "IOS Tablet":
                    case "IOS Tablets":
                        product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Android Tablet":
                    case "Android Tablets":
                        product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Other Tablet":
                    case "Other Tablets":
                        product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Tablet Accessory":
                    case "Tablet Accessories":
                    case "Tablet Accessorys":
                        product = await _context.TabletAccessorys.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Television":
                    case "Televisions":
                        product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Heating & Cooling":
                        product = await _context.HeatingCoolings.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Camera":
                    case "Cameras":
                        product = await _context.Cameras.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Others":
                        product = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                }
            }


            if (product != null)
                {


                    foreach (var prop in product.GetType().GetProperties())
                    {
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

                        }
                    }
                }
                else
                {
                    _logger.LogWarning("No product found for subcategory '{SubCategory}' and listing ID {ListingId}", listing.SubCategory, id);
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

                var imageCount = await _context.ListingImages.CountAsync(i => i.ListingId == image.ListingId);
                if (imageCount <= 1)
                {
                    return Json(new { success = false, message = "Cannot delete the only image. Please upload a new image first." });
                }

                if (image.IsMainImage)
                {
                    var newMainImage = await _context.ListingImages
                        .FirstOrDefaultAsync(i => i.ListingId == image.ListingId && i.Id != imageId);

                    if (newMainImage != null)
                    {
                        newMainImage.IsMainImage = true;
                    }
                }

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
                    }
                }


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

            if (DeletedImageIds == null)
            {
                DeletedImageIds = new List<int>();
            }

            if (model.DetailCategory == null)
            {
                model.DetailCategory = string.Empty;
            }

            var originalStatus = listing.Status;
            var wasApproved = listing.Status == ListingStatus.Active;

            int remainingImagesCount = listing.Images.Count(i => !DeletedImageIds.Contains(i.Id));
            bool willHaveImages = remainingImagesCount > 0 || (newImages != null && newImages.Count > 0);

            if (!willHaveImages)
            {
                ModelState.AddModelError("Images", "At least one image is required. Please upload a new image.");
                model.Images = listing.Images.ToList();
                if (!string.IsNullOrEmpty(listing.SubCategory))
                {
                    model.DynamicProperties = await GetDynamicPropertiesAsync(listing.Id, listing.SubCategory, listing.DetailCategory);
                }
                return View(model);
            }

            try
            {
                listing.Title = model.Title;
                listing.Price = model.Price;
                listing.Description = model.Description;
                listing.Location = model.Location;
                listing.DetailCategory = model.DetailCategory;
                listing.UpdatedAt = DateTime.UtcNow;


                if (listing.Status == ListingStatus.Active || listing.Status == ListingStatus.Rejected)
                {
                    listing.Status = ListingStatus.Pending;
                    listing.RejectionReason = null;
                }

                if (DeletedImageIds.Count > 0)
                {
                    var imagesToDelete = listing.Images.Where(i => DeletedImageIds.Contains(i.Id)).ToList();

                    foreach (var imageToDelete in imagesToDelete)
                    {

                        if (imageToDelete.IsMainImage && listing.Images.Count > DeletedImageIds.Count)
                        {
                            var newMainImage = listing.Images.FirstOrDefault(i => !DeletedImageIds.Contains(i.Id));
                            if (newMainImage != null)
                            {
                                newMainImage.IsMainImage = true;
                            }
                        }


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


                if (model.Category == "Others" || string.Equals(model.Category, "Others", StringComparison.OrdinalIgnoreCase))
                {

                    var othersProduct = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == listing.Id);


                    if (othersProduct == null)
                    {
                        othersProduct = new Models.Products.Other
                        {
                            ListingId = listing.Id
                        };
                        _context.Others.Add(othersProduct);
                        _logger.LogInformation("Created new Others product for listing {ListingId}", listing.Id);
                    }


                    if (model.DynamicProperties != null)
                    {
                        foreach (var prop in model.DynamicProperties)
                        {

                            var properties = othersProduct.GetType().GetProperties();
                            var productProp = properties.FirstOrDefault(p =>
                                string.Equals(p.Name, prop.Key, StringComparison.OrdinalIgnoreCase));

                            if (productProp != null && productProp.CanWrite)
                            {
                                try
                                {

                                    if (string.IsNullOrWhiteSpace(prop.Value))
                                        continue;


                                    if (productProp.PropertyType == typeof(bool))
                                    {
                                        bool boolValue = prop.Value.ToLower() == "yes" || prop.Value.ToLower() == "true";
                                        productProp.SetValue(othersProduct, boolValue);
                                    }
                                    else if (productProp.PropertyType == typeof(int))
                                    {
                                        if (int.TryParse(prop.Value, out int intValue))
                                        {
                                            productProp.SetValue(othersProduct, intValue);
                                        }
                                    }
                                    else if (productProp.PropertyType == typeof(decimal))
                                    {
                                        if (decimal.TryParse(prop.Value, out decimal decimalValue))
                                        {
                                            productProp.SetValue(othersProduct, decimalValue);
                                        }
                                    }
                                    else
                                    {
                                        productProp.SetValue(othersProduct, prop.Value);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error setting property {PropertyName}: {ErrorMessage}",
                                        productProp.Name, ex.Message);
                                }
                            }
                        }
                    }
                }
                else if (model.DynamicProperties != null && !string.IsNullOrEmpty(listing.SubCategory))
                {
                    await UpdateDynamicPropertiesAsync(listing.Id, listing.SubCategory, model.DynamicProperties, listing.DetailCategory);
                }


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

                return RedirectToAction("MyProfilePage", "User");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating listing: {ErrorMessage}", ex.Message);
                ModelState.AddModelError("", $"Error updating listing: {ex.Message}");

                model.Images = listing.Images.ToList();
                if (!string.IsNullOrEmpty(listing.SubCategory))
                {
                    model.DynamicProperties = await GetDynamicPropertiesAsync(listing.Id, listing.SubCategory, listing.DetailCategory);
                }

                return View(model);
            }
        }

        private async Task<Dictionary<string, string>> GetDynamicPropertiesAsync(int listingId, string subcategory, string detailcategory)
        {
            var result = new Dictionary<string, string>();
            var listing = await _context.Listings
            .Include(l => l.Images)
            .Include(l => l.Seller)
            .Include(l => l.Reviews)
            .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(l => l.Id == listingId);

            dynamic product = null;

            if (listing.Category == "Others" || string.Equals(listing.Category, "Others", StringComparison.OrdinalIgnoreCase))
            {
                product = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == listingId);
            }
            else if (!string.IsNullOrEmpty(listing.DetailCategory))
            {
                switch (listing.DetailCategory)
                {
                    case "Television":
                    case "Televisions":
                        product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Laptops":
                    case "Laptop":
                        product = await _context.Laptops.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Desktop":
                    case "Desktops":
                        product = await _context.Desktops.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Computer Accessory":
                    case "Computer Accessories":
                    case "Computer Accessorys":
                        product = await _context.ComputerAccessorys.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Computer Component":
                    case "Computer Components":
                        product = await _context.ComputerComponents.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Monitor":
                    case "Monitors":
                        product = await _context.Monitors.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Fridge":
                    case "Fridges":
                        product = await _context.Fridges.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Washer":
                    case "Washers":
                        product = await _context.Washers.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Stove":
                    case "Stoves":
                        product = await _context.Stoves.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Microwave Oven":
                    case "Microwave Ovens":
                        product = await _context.MicrowaveOvens.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Dishwasher":
                    case "Dishwashers":
                        product = await _context.Dishwashers.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Oven":
                    case "Ovens":
                        product = await _context.Ovens.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Vacuum Cleaner":
                    case "Vacuum Cleaners":
                        product = await _context.VacuumCleaners.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Beverage Preparation":
                    case "Beverage Preparations":
                        product = await _context.BeveragePreparations.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Food Preparation":
                    case "Food Preparations":
                        product = await _context.FoodPreparations.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Iron":
                    case "Irons":
                        product = await _context.Irons.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Sewing Machine":
                    case "Sewing Machines":
                        product = await _context.SewingMachines.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Keyboard":
                    case "Keyboards":
                        product = await _context.Keyboards.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Speaker":
                    case "Speakers":
                        product = await _context.Speakers.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Headphone & Earphone":
                    case "Headphones & Earphones":
                        product = await _context.HeadphoneEarphones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Webcam":
                    case "Webcams":
                        product = await _context.Webcams.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Microphone":
                    case "Microphones":
                        product = await _context.Microphones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Mouse":
                    case "Mouses":
                        product = await _context.Mouses.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Computer Bag":
                    case "Computer Bags":
                        product = await _context.ComputerBags.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Others":
                        product = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "IOS Phone":
                        product = await _context.IOSPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Android Phone":
                        product = await _context.AndroidPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Other Phone":
                    case "Other Phones":
                        product = await _context.OtherPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Spare Part":
                    case "Spare Parts":
                        product = await _context.SpareParts.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Phone Accessory":
                    case "Phone Accessories":
                    case "Phone Accessorys":
                        product = await _context.PhoneAccessorys.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "IOS Tablet":
                    case "IOS Tablets":
                        product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Android Tablet":
                    case "Android Tablets":
                        product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Other Tablet":
                    case "Other Tablets":
                        product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Tablet Accessory":
                    case "Tablet Accessories":
                    case "Tablet Accessorys":
                        product = await _context.TabletAccessorys.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Heating & Cooling":
                        product = await _context.HeatingCoolings.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Camera":
                    case "Cameras":
                        product = await _context.Cameras.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                }
            }
            else if (!string.IsNullOrEmpty(listing.SubCategory))
            {
                switch (listing.SubCategory)
                {
                    case "IOS Phone":
                        product = await _context.IOSPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Android Phone":
                        product = await _context.AndroidPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Other Phone":
                    case "Other Phones":
                        product = await _context.OtherPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Spare Part":
                    case "Spare Parts":
                        product = await _context.SpareParts.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Phone Accessory":
                    case "Phone Accessories":
                    case "Phone Accessorys":
                        product = await _context.PhoneAccessorys.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "IOS Tablet":
                    case "IOS Tablets":
                        product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Android Tablet":
                    case "Android Tablets":
                        product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Other Tablet":
                    case "Other Tablets":
                        product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Tablet Accessory":
                    case "Tablet Accessories":
                    case "Tablet Accessorys":
                        product = await _context.TabletAccessorys.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Television":
                    case "Televisions":
                        product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Heating & Cooling":
                        product = await _context.HeatingCoolings.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Camera":
                    case "Cameras":
                        product = await _context.Cameras.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Others":
                        product = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                }
            }

            if (product != null)
            {
                foreach (var prop in product.GetType().GetProperties())
                {

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



        private async Task UpdateDynamicPropertiesAsync(int listingId, string subcategory, Dictionary<string, string> properties, string detailcategory)
        {
            var listing = await _context.Listings
            .Include(l => l.Images)
            .Include(l => l.Seller)
            .Include(l => l.Reviews)
            .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(l => l.Id == listingId);

            dynamic product = null;

            if (listing.Category == "Others" || string.Equals(listing.Category, "Others", StringComparison.OrdinalIgnoreCase))
            {
                product = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == listingId);
            }
            else if (!string.IsNullOrEmpty(listing.DetailCategory))
            {
                switch (listing.DetailCategory)
                {
                    case "Television":
                    case "Televisions":
                        product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Laptops":
                    case "Laptop":
                        product = await _context.Laptops.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Desktop":
                    case "Desktops":
                        product = await _context.Desktops.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Computer Accessory":
                    case "Computer Accessories":
                    case "Computer Accessorys":
                        product = await _context.ComputerAccessorys.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Computer Component":
                    case "Computer Components":
                        product = await _context.ComputerComponents.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Monitor":
                    case "Monitors":
                        product = await _context.Monitors.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Fridge":
                    case "Fridges":
                        product = await _context.Fridges.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Washer":
                    case "Washers":
                        product = await _context.Washers.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Stove":
                    case "Stoves":
                        product = await _context.Stoves.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Microwave Oven":
                    case "Microwave Ovens":
                        product = await _context.MicrowaveOvens.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Dishwasher":
                    case "Dishwashers":
                        product = await _context.Dishwashers.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Oven":
                    case "Ovens":
                        product = await _context.Ovens.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Vacuum Cleaner":
                    case "Vacuum Cleaners":
                        product = await _context.VacuumCleaners.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Beverage Preparation":
                    case "Beverage Preparations":
                        product = await _context.BeveragePreparations.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Food Preparation":
                    case "Food Preparations":
                        product = await _context.FoodPreparations.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Iron":
                    case "Irons":
                        product = await _context.Irons.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Sewing Machine":
                    case "Sewing Machines":
                        product = await _context.SewingMachines.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Keyboard":
                    case "Keyboards":
                        product = await _context.Keyboards.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Speaker":
                    case "Speakers":
                        product = await _context.Speakers.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Headphone & Earphone":
                    case "Headphones & Earphones":
                        product = await _context.HeadphoneEarphones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Webcam":
                    case "Webcams":
                        product = await _context.Webcams.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Microphone":
                    case "Microphones":
                        product = await _context.Microphones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Mouse":
                    case "Mouses":
                        product = await _context.Mouses.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Computer Bag":
                    case "Computer Bags":
                        product = await _context.ComputerBags.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Others":
                        product = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "IOS Phone":
                        product = await _context.IOSPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Android Phone":
                        product = await _context.AndroidPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Other Phone":
                    case "Other Phones":
                        product = await _context.OtherPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Spare Part":
                    case "Spare Parts":
                        product = await _context.SpareParts.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Phone Accessory":
                    case "Phone Accessories":
                    case "Phone Accessorys":
                        product = await _context.PhoneAccessorys.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "IOS Tablet":
                    case "IOS Tablets":
                        product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Android Tablet":
                    case "Android Tablets":
                        product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Other Tablet":
                    case "Other Tablets":
                        product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Tablet Accessory":
                    case "Tablet Accessories":
                    case "Tablet Accessorys":
                        product = await _context.TabletAccessorys.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Heating & Cooling":
                        product = await _context.HeatingCoolings.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Camera":
                    case "Cameras":
                        product = await _context.Cameras.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                }
            }
            else if (!string.IsNullOrEmpty(listing.SubCategory))
            {
                switch (listing.SubCategory)
                {
                    case "IOS Phone":
                        product = await _context.IOSPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Android Phone":
                        product = await _context.AndroidPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Other Phone":
                    case "Other Phones":
                        product = await _context.OtherPhones.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Spare Part":
                    case "Spare Parts":
                        product = await _context.SpareParts.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Phone Accessory":
                    case "Phone Accessories":
                    case "Phone Accessorys":
                        product = await _context.PhoneAccessorys.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "IOS Tablet":
                    case "IOS Tablets":
                        product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Android Tablet":
                    case "Android Tablets":
                        product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Other Tablet":
                    case "Other Tablets":
                        product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Tablet Accessory":
                    case "Tablet Accessories":
                    case "Tablet Accessorys":
                        product = await _context.TabletAccessorys.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Television":
                    case "Televisions":
                        product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Heating & Cooling":
                        product = await _context.HeatingCoolings.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Camera":
                    case "Cameras":
                        product = await _context.Cameras.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                    case "Others":
                        product = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == listingId);
                        break;
                }
            }


            if (product != null)
            {
                foreach (var prop in properties)
                {

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

                            if (string.IsNullOrWhiteSpace(prop.Value))
                                continue;


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
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "You must be logged in to delete listings." });
            }

            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == id && l.SellerId == userId);

            if (listing == null)
            {
                return Json(new { success = false, message = "Listing not found or you don't have permission to delete it." });
            }

            try
            {
                listing.Status = ListingStatus.Archived;
                listing.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation($"Listing {listing.Id} '{listing.Title}' was archived by the seller");

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Listing has been archived.",
                    RedirectToAction = Url.Action("MyProfilePage", "User"),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving listing");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        #region Browse and Search

        [HttpGet]
        public async Task<IActionResult> Index(string category = null, string subcategory = null, string searchTerm = null, int page = 1)
        {
            int pageSize = 12;

            var query = _context.Listings
                .Where(l => l.Status == ListingStatus.Active)
                .Include(l => l.Images)
                .Include(l => l.Seller)
                .AsQueryable();


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

            var totalItems = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages));

            var listings = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

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

            var userId = HttpContext.Session.GetInt32("UserId");
            var isAdmin = HttpContext.Session.GetInt32("AdminId") != null;

            if ((listing.Status != ListingStatus.Active || listing.Status == ListingStatus.Archived) &&
                userId != listing.SellerId &&
                !isAdmin)
            {
                return NotFound();
            }

            if (userId != listing.SellerId)
            {
                if (listing.ViewCount == null)
                {
                    listing.ViewCount = 1;
                }
                else
                {
                    listing.ViewCount++;
                }
                await _context.SaveChangesAsync();
            }

            dynamic product = null;

            if (listing.Category == "Others" || string.Equals(listing.Category, "Others", StringComparison.OrdinalIgnoreCase))
            {
                product = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == id);
            }
            else if (!string.IsNullOrEmpty(listing.DetailCategory))
            {
                switch (listing.DetailCategory)
                {
                    case "Television":
                    case "Televisions":
                        product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Laptops":
                    case "Laptop":
                        product = await _context.Laptops.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Desktop":
                    case "Desktops":
                        product = await _context.Desktops.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Computer Accessory":
                    case "Computer Accessories":
                    case "Computer Accessorys":
                        product = await _context.ComputerAccessorys.FirstOrDefaultAsync(p => p.ListingId == id);
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
                    case "Dishwashers":
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
                    case "Headphones & Earphones":
                        product = await _context.HeadphoneEarphones.FirstOrDefaultAsync(p => p.ListingId == id);
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
                    case "Others":
                        product = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "IOS Phone":
                        product = await _context.IOSPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Android Phone":
                        product = await _context.AndroidPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Other Phone":
                    case "Other Phones":
                        product = await _context.OtherPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Spare Part":
                    case "Spare Parts":
                        product = await _context.SpareParts.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Phone Accessory":
                    case "Phone Accessories":
                    case "Phone Accessorys":
                        product = await _context.PhoneAccessorys.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "IOS Tablet":
                    case "IOS Tablets":
                        product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Android Tablet":
                    case "Android Tablets":
                        product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Other Tablet":
                    case "Other Tablets":
                        product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Tablet Accessory":
                    case "Tablet Accessories":
                    case "Tablet Accessorys":
                        product = await _context.TabletAccessorys.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Heating & Cooling":
                        product = await _context.HeatingCoolings.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Camera":
                    case "Cameras":
                        product = await _context.Cameras.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                }
            }
            else if (!string.IsNullOrEmpty(listing.SubCategory))
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
                    case "Other Phones":
                        product = await _context.OtherPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Spare Part":
                    case "Spare Parts":
                        product = await _context.SpareParts.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Phone Accessory":
                    case "Phone Accessories":
                    case "Phone Accessorys":
                        product = await _context.PhoneAccessorys.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "IOS Tablet":
                    case "IOS Tablets":
                        product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Android Tablet":
                    case "Android Tablets":
                        product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Other Tablet":
                    case "Other Tablets":
                        product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Tablet Accessory":
                    case "Tablet Accessories":
                    case "Tablet Accessorys":
                        product = await _context.TabletAccessorys.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Television":
                    case "Televisions":
                        product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Heating & Cooling":
                        product = await _context.HeatingCoolings.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Camera":
                    case "Cameras":
                        product = await _context.Cameras.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Others":
                        product = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                }
            }

            bool isBookmarked = false;
            if (userId.HasValue)
            {
                isBookmarked = await _context.Bookmarks
                    .AnyAsync(b => b.UserId == userId.Value && b.ListingId == id);
            }

            var relatedListings = await _context.Listings
                .Where(l => l.Status == ListingStatus.Active &&
                            l.Id != id &&
                            l.Category == listing.Category)
                .Include(l => l.Images)
                .OrderByDescending(l => l.CreatedAt)
                .Take(4)
                .ToListAsync();

            List<Offer> activeOffers = new List<Offer>();
            if (listing.Condition == "Second-Hand" && userId.HasValue && userId.Value == listing.SellerId)
            {
                activeOffers = await _context.Offers
                    .Include(o => o.Buyer)
                    .Where(o => o.ListingId == id && o.Status == OfferStatus.Pending)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }


            bool canReview = false;
            bool hasReviewed = false;

            if (userId.HasValue && listing.Condition == "First-Hand")
            {

                var hasPurchased = await _context.Purchases
                    .Include(p => p.Confirmation)
                    .AnyAsync(p => p.BuyerId == userId.Value &&
                                  p.ListingId == id &&
                                  p.PaymentStatus == PaymentStatus.Completed &&
                                  p.Confirmation != null &&
                                  p.Confirmation.BuyerReceivedProduct);

                if (hasPurchased)
                {

                    hasReviewed = await _context.ProductReviews
                        .AnyAsync(r => r.UserId == userId.Value && r.ListingId == id);

                    canReview = !hasReviewed;
                }
            }


            var reviews = listing.Reviews?.ToList() ?? new List<ProductReview>();
            double averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            int totalReviews = reviews.Count;


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
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (string.IsNullOrEmpty(query))
            {
                if (userId == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return RedirectToAction("User_MainPage", "User");
                }
            }

            ViewBag.SearchTerm = query;


            if (System.Text.RegularExpressions.Regex.IsMatch(query, @"^\d+$"))
            {
                ViewBag.ShowIdFormatMessage = true;

                var numberResults = await _context.Listings
                    .Where(l => l.Status == ListingStatus.Active &&
                             (l.Title.Contains(query) ||
                              l.Description.Contains(query)))
                    .Include(l => l.Images)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToListAsync();

                ViewBag.ListingRatings = GetListingRatings(numberResults.Select(l => l.Id).ToList());

                return View("~/Views/Home/Search_Page.cshtml", numberResults);
            }

            if (query.StartsWith("#") && query.Length > 1)
            {
                if (int.TryParse(query.Substring(1), out int listingId))
                {
                    var listing = await _context.Listings
                        .Include(l => l.Images)
                        .FirstOrDefaultAsync(l => l.Id == listingId &&
                                            l.Status == ListingStatus.Active);

                    if (listing != null)
                    {
                        var singleListing = new List<Listing> { listing };

                        ViewBag.ListingRatings = GetListingRatings(new List<int> { listing.Id });

                        return View("~/Views/Home/Search_Page.cshtml", singleListing);
                    }
                }
            }


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


            ViewBag.ListingRatings = GetListingRatings(results.Select(l => l.Id).ToList());

            return View("~/Views/Home/Search_Page.cshtml", results);
        }


        private Dictionary<int, double> GetListingRatings(List<int> listingIds)
        {
            var ratings = _context.ProductReviews
                .Where(r => listingIds.Contains(r.ListingId))
                .GroupBy(r => r.ListingId)
                .Select(g => new
                {
                    ListingId = g.Key,
                    AverageRating = Math.Round(g.Average(r => r.Rating), 1)
                })
                .ToDictionary(x => x.ListingId, x => x.AverageRating);

            return ratings;
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

            var userId = HttpContext.Session.GetInt32("UserId");
            var isAdmin = HttpContext.Session.GetInt32("AdminId") != null;

            if ((listing.Status != ListingStatus.Active || listing.Status == ListingStatus.Archived) &&
                userId != listing.SellerId &&
                !isAdmin)
            {
                return NotFound();
            }

            if (userId != listing.SellerId)
            {
                if (listing.ViewCount == null)
                {
                    listing.ViewCount = 1;
                }
                else
                {
                    listing.ViewCount++;
                }
                await _context.SaveChangesAsync();
            }
            dynamic product = null;
            if (listing.Category == "Others" || string.Equals(listing.Category, "Others", StringComparison.OrdinalIgnoreCase))
            {
                product = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == id);
            }
            else if (!string.IsNullOrEmpty(listing.DetailCategory))
            {
                switch (listing.DetailCategory)
                {
                    case "Television":
                    case "Televisions":
                        product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Laptops":
                    case "Laptop":
                        product = await _context.Laptops.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Desktop":
                    case "Desktops":
                        product = await _context.Desktops.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Computer Accessory":
                    case "Computer Accessories":
                        product = await _context.ComputerAccessorys.FirstOrDefaultAsync(p => p.ListingId == id);
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
                    case "Dishwashers":
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
                    case "Headphones & Earphones":
                        product = await _context.HeadphoneEarphones.FirstOrDefaultAsync(p => p.ListingId == id);
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
                    case "Others":
                        product = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "IOS Phone":
                        product = await _context.IOSPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Android Phone":
                        product = await _context.AndroidPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Other Phone":
                    case "Other Phones":
                        product = await _context.OtherPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Spare Part":
                    case "Spare Parts":
                        product = await _context.SpareParts.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Phone Accessory":
                    case "Phone Accessories":
                        product = await _context.PhoneAccessorys.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "IOS Tablet":
                    case "IOS Tablets":
                        product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Android Tablet":
                    case "Android Tablets":
                        product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Other Tablet":
                    case "Other Tablets":
                        product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Tablet Accessory":
                    case "Tablet Accessories":
                        product = await _context.TabletAccessorys.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Heating & Cooling":
                        product = await _context.HeatingCoolings.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Camera":
                    case "Cameras":
                        product = await _context.Cameras.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                }
            }
            else if (!string.IsNullOrEmpty(listing.SubCategory))
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
                    case "Other Phones":
                        product = await _context.OtherPhones.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Spare Part":
                    case "Spare Parts":
                        product = await _context.SpareParts.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Phone Accessory":
                    case "Phone Accessories":
                        product = await _context.PhoneAccessorys.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "IOS Tablet":
                    case "IOS Tablets":
                        product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Android Tablet":
                    case "Android Tablets":
                        product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Other Tablet":
                    case "Other Tablets":
                        product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Tablet Accessory":
                    case "Tablet Accessories":
                        product = await _context.TabletAccessorys.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Television":
                    case "Televisions":
                        product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Heating & Cooling":
                        product = await _context.HeatingCoolings.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Camera":
                    case "Cameras":
                        product = await _context.Cameras.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                    case "Others":
                        product = await _context.Others.FirstOrDefaultAsync(p => p.ListingId == id);
                        break;
                }
            }

            bool isBookmarked = false;
            if (userId.HasValue)
            {
                isBookmarked = await _context.Bookmarks
                    .AnyAsync(b => b.UserId == userId.Value && b.ListingId == id);
            }

            var relatedListings = await _context.Listings
                .Where(l => l.Status == ListingStatus.Active &&
                            l.Id != id &&
                            l.Category == listing.Category)
                .Include(l => l.Images)
                .OrderByDescending(l => l.CreatedAt)
                .Take(4)
                .ToListAsync();

            List<Offer> activeOffers = new List<Offer>();
            if (listing.Condition == "Second-Hand" && userId.HasValue && userId.Value == listing.SellerId)
            {
                activeOffers = await _context.Offers
                    .Include(o => o.Buyer)
                    .Where(o => o.ListingId == id && o.Status == OfferStatus.Pending)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }

            bool canReview = false;
            bool hasReviewed = false;

            if (userId.HasValue && listing.Condition == "First-Hand")
            {
                var hasPurchased = await _context.Purchases
                    .Include(p => p.Confirmation)
                    .AnyAsync(p => p.BuyerId == userId.Value &&
                                  p.ListingId == id &&
                                  p.PaymentStatus == PaymentStatus.Completed &&
                                  p.Confirmation != null &&
                                  p.Confirmation.BuyerReceivedProduct);

                if (hasPurchased)
                {
                    hasReviewed = await _context.ProductReviews
                        .AnyAsync(r => r.UserId == userId.Value && r.ListingId == id);

                    canReview = !hasReviewed;
                }
            }

            var reviews = listing.Reviews?.ToList() ?? new List<ProductReview>();
            double averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            int totalReviews = reviews.Count;

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
                    _context.Bookmarks.Remove(existingBookmark);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, isBookmarked = false, message = "Item removed from bookmarks." });
                }
                else
                {
                    var listing = await _context.Listings
                        .FirstOrDefaultAsync(l => l.Id == listingId && l.Status == Models.Enum.ListingStatus.Active);

                    if (listing == null)
                    {
                        return Json(new { success = false, message = "This listing is not available for bookmarking." });
                    }

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

            decimal offerAmount = model.OfferAmount;


            if (offerAmount <= 0 && !string.IsNullOrWhiteSpace(model.OfferAmountRaw))
            {

                if (!decimal.TryParse(model.OfferAmountRaw, NumberStyles.Any,
                                      CultureInfo.InvariantCulture, out offerAmount))
                {
                    decimal.TryParse(model.OfferAmountRaw, NumberStyles.Any,
                                     CultureInfo.CurrentCulture, out offerAmount);
                }
            }

            if (offerAmount <= 0)
                return Json(new { success = false, message = "Offer amount must be greater than zero." });

  
            var listing = await _context.Listings.FindAsync(model.ListingId);
            if (listing == null)
                return Json(new { success = false, message = "Listing not found." });


            if (listing.Condition != "Second-Hand")
                return Json(new { success = false, message = "You can only make offers on second-hand listings." });


            if (listing.Status != ListingStatus.Active)
                return Json(new { success = false, message = "This listing is not available for offers." });


            if (listing.Status == ListingStatus.Archived)
                return Json(new { success = false, message = "This listing has been archived by the seller and is no longer available for offers." });

            if (listing.SellerId == userId.Value)
                return Json(new { success = false, message = "You cannot make an offer on your own listing." });

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
                Message = model.Message ?? string.Empty, 
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
                    offer.Status = OfferStatus.Accepted;

                    var acceptNotification = new Notification
                    {
                        UserId = offer.BuyerId,
                        Message = $"Your offer of {offer.OfferAmount:C} for '{offer.Listing.Title}' has been accepted. You can now proceed with the purchase.",
                        Type = NotificationType.OfferAccepted,
                        RelatedEntityId = offer.ListingId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(acceptNotification);


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
                    offer.Status = OfferStatus.Rejected;

                    var rejectNotification = new Notification
                    {
                        UserId = offer.BuyerId,
                        Message = $"Your offer of {offer.OfferAmount:C} for '{offer.Listing.Title}' has been rejected.",
                        Type = NotificationType.OfferRejected,
                        RelatedEntityId = offer.ListingId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(rejectNotification);

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