using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Models;
using PrimeMarket.Models.Enum;
using PrimeMarket.Models.ViewModel;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PrimeMarket.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/AdminLogin
        public IActionResult AdminLogin()
        {
            // If already logged in, redirect to dashboard
            if (HttpContext.Session.GetInt32("AdminId") != null)
            {
                return RedirectToAction(nameof(AdminDashboard));
            }
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetUserDetails(int userId)
        {
            // Check if admin is logged in
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return Json(new { success = false, message = "Not authorized" });
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            // Return user details as JSON
            return Json(new
            {
                id = user.Id,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                profileImagePath = user.ProfileImagePath,
                isIdVerified = user.IsIdVerified,
                createdAt = user.CreatedAt,
                updatedAt = user.UpdatedAt
            });
        }

        // GET: /Admin/GetUserListings
        [HttpGet]
        public async Task<IActionResult> GetUserListings(int userId)
        {
            // Check if admin is logged in
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return Json(new { success = false, message = "Not authorized" });
            }

            var listings = await _context.Listings
                .Include(l => l.Images)
                .Where(l => l.SellerId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            if (listings == null || !listings.Any())
            {
                return Json(new List<object>());
            }

            // Return listings as JSON
            var result = listings.Select(l => new
            {
                id = l.Id,
                title = l.Title,
                price = l.Price,
                description = l.Description,
                condition = l.Condition,
                category = l.Category,
                subCategory = l.SubCategory,
                detailCategory = l.DetailCategory,
                location = l.Location,
                status = l.Status.ToString(),
                rejectionReason = l.RejectionReason,
                createdAt = l.CreatedAt,
                updatedAt = l.UpdatedAt,
                images = l.Images.Select(i => new
                {
                    id = i.Id,
                    imagePath = i.ImagePath,
                    isMainImage = i.IsMainImage
                }).ToList()
            }).ToList();

            return Json(result);
        }

        // POST: /Admin/AdminLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(AdminLoginModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Please enter both username and password.";
                return View();
            }

            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == model.Username);

            if (admin == null)
            {
                ViewBag.ErrorMessage = "Invalid username or password.";
                return View();
            }

            // Use secure password comparison
            //string hashedPassword = ComputeSha256Hash(model.Password);
            if (admin.Password != model.Password)
            {
                ViewBag.ErrorMessage = "Invalid username or password.";
                return View();
            }

            // Store admin ID in session
            HttpContext.Session.SetInt32("AdminId", admin.Id);
            HttpContext.Session.SetString("AdminUsername", admin.Username);

            // Log the admin action
            var adminAction = new AdminAction
            {
                AdminId = admin.Id,
                ActionType = "Login",
                EntityType = "Admin",
                EntityId = admin.Id,
                ActionDetails = "Admin logged in",
                CreatedAt = DateTime.UtcNow
            };
            _context.AdminActions.Add(adminAction);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(AdminDashboard));
        }


        // GET: /Admin/AdminDashboard
        public IActionResult AdminDashboard()
        {
            // Check if admin is logged in
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction(nameof(AdminLogin));
            }

            return View();
        }

        // GET: /Admin/Logout
        public async Task<IActionResult> Logout()
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId != null)
            {
                // Log the admin action
                var adminAction = new AdminAction
                {
                    AdminId = adminId.Value,
                    ActionType = "Logout",
                    EntityType = "Admin",
                    EntityId = adminId.Value,
                    ActionDetails = "Admin logged out",
                    CreatedAt = DateTime.UtcNow
                };
                _context.AdminActions.Add(adminAction);
                await _context.SaveChangesAsync();
            }

            // Clear session
            HttpContext.Session.Clear();

            return RedirectToAction(nameof(AdminLogin));
        }

        [HttpGet]
        public async Task<IActionResult> PendingListings()
        {
            // Check if admin is logged in
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction(nameof(AdminLogin));
            }

            var pendingListings = await _context.Listings
                .Include(l => l.Images)
                .Include(l => l.Seller)
                .Where(l => l.Status == ListingStatus.Pending)
                .OrderBy(l => l.CreatedAt)
                .ToListAsync();

            return View(pendingListings);
        }

        [HttpGet]
        public async Task<IActionResult> ListingDetails(int id)
        {
            // Check if admin is logged in
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction(nameof(AdminLogin));
            }

            var listing = await _context.Listings
                .Include(l => l.Images)
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null)
            {
                return NotFound();
            }

            // Get the specific product details based on the category
            dynamic? product = null;

            // Special handling for "Others" category
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
                        product = await _context.PhoneAccessories.FirstOrDefaultAsync(p => p.ListingId == id);
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
                        product = await _context.TabletAccessories.FirstOrDefaultAsync(p => p.ListingId == id);
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
                        product = await _context.PhoneAccessories.FirstOrDefaultAsync(p => p.ListingId == id);
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
                        product = await _context.TabletAccessories.FirstOrDefaultAsync(p => p.ListingId == id);
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

            // Get seller verification status
            var sellerVerification = await _context.VerificationDocuments
                .FirstOrDefaultAsync(v => v.UserId == listing.SellerId);

            ViewBag.Product = product;
            ViewBag.SellerVerification = sellerVerification;
            ViewBag.IsSellerVerified = listing.Seller?.IsIdVerified ?? false;

            // Check if this is an updated listing
            ViewBag.IsUpdatedListing = listing.UpdatedAt.HasValue &&
                                       listing.UpdatedAt.Value > listing.CreatedAt.Value.AddMinutes(5);

            // Get any previous versions/history of the listing if needed
            ViewBag.ListingHistory = await _context.AdminActions
                .Where(a => a.EntityType == "Listing" && a.EntityId == id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(listing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveListing(int id)
        {
            // Check if admin is logged in
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction(nameof(AdminLogin));
            }

            var listing = await _context.Listings.FindAsync(id);
            if (listing == null)
            {
                return NotFound();
            }

            try
            {
                listing.Status = ListingStatus.Active;
                listing.UpdatedAt = DateTime.UtcNow;

                // Create an admin action record
                var adminAction = new AdminAction
                {
                    AdminId = adminId.Value,
                    ActionType = "Approve",
                    EntityType = "Listing",
                    EntityId = id,
                    ActionDetails = $"Approved listing: {listing.Title}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.AdminActions.Add(adminAction);

                // Create a notification for the seller
                var notification = new Notification
                {
                    UserId = listing.SellerId,
                    Message = $"Your listing '{listing.Title}' has been approved",
                    Type = NotificationType.ListingApproved,
                    RelatedEntityId = id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Listing approved successfully.";
                return RedirectToAction(nameof(PendingListings));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error approving listing: {ex.Message}";
                return RedirectToAction(nameof(ListingDetails), new { id });
            }
        }

        // Updated RejectListing method to handle checkbox rejection reasons
        // --------------  LISTING REJECT -------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectListing(int id, string rejectionReason)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
                return Json(new { success = false, message = "You are not signed-in as an admin." });

            if (string.IsNullOrWhiteSpace(rejectionReason))
                return Json(new { success = false, message = "Rejection reason is required." });

            var listing = await _context.Listings.FindAsync(id);
            if (listing == null)
                return Json(new { success = false, message = "Listing not found." });

            try
            {
                listing.Status = ListingStatus.Rejected;
                listing.RejectionReason = rejectionReason;
                listing.UpdatedAt = DateTime.UtcNow;

                // audit + notification (unchanged)
                _context.AdminActions.Add(new AdminAction
                {
                    AdminId = adminId.Value,
                    ActionType = "Reject",
                    EntityType = "Listing",
                    EntityId = id,
                    ActionDetails = $"Rejected listing: {listing.Title}, Reason: {rejectionReason}",
                    CreatedAt = DateTime.UtcNow
                });

                _context.Notifications.Add(new Notification
                {
                    UserId = listing.SellerId,
                    Message = $"Your listing '{listing.Title}' has been rejected. Reason: {rejectionReason}",
                    Type = NotificationType.ListingRejected,
                    RelatedEntityId = id,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                // --- key point --------------------------------------------------
                // If this was an AJAX request (fetch with `Accept: application/json`)
                // return JSON; otherwise fall back to the old redirect/TempData flow.
                // ----------------------------------------------------------------
                if (Request.Headers["Accept"].Any(h => h.Contains("application/json")))
                    return Json(new { success = true });

                TempData["SuccessMessage"] = "Listing rejected successfully.";
                return RedirectToAction(nameof(PendingListings));
            }
            catch (Exception ex)
            {
                if (Request.Headers["Accept"].Any(h => h.Contains("application/json")))
                    return Json(new { success = false, message = ex.Message });

                TempData["ErrorMessage"] = $"Error rejecting listing: {ex.Message}";
                return RedirectToAction(nameof(ListingDetails), new { id });
            }
        }


        [HttpGet]
        public async Task<IActionResult> PendingVerifications()
        {
            // Check if admin is logged in
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction(nameof(AdminLogin));
            }

            var pendingVerifications = await _context.VerificationDocuments
                .Include(v => v.User)
                .Where(v => v.Status == VerificationStatus.Pending)
                .OrderBy(v => v.CreatedAt)
                .ToListAsync();

            return View(pendingVerifications);
        }

        [HttpGet]
        public async Task<IActionResult> VerificationDetails(int id)
        {
            // Check if admin is logged in
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction(nameof(AdminLogin));
            }

            var verification = await _context.VerificationDocuments
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (verification == null)
            {
                return NotFound();
            }

            // Ensure face image property is available in the view
            ViewBag.FaceImagePath = verification.FaceImagePath ?? "/images/user-placeholder.png";

            return View(verification);
        }

        // --------------  helper --------------
        private IActionResult Fail(string msg) =>
            Json(new { success = false, message = msg });

        // --------------  APPROVE -------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveVerification(int id)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null) return Fail("You are not signed-in as an admin.");

            var verification = await _context.VerificationDocuments
                                             .Include(v => v.User)
                                             .FirstOrDefaultAsync(v => v.Id == id);
            if (verification == null) return Fail("Verification not found.");

            try
            {
                verification.Status = VerificationStatus.Approved;
                verification.UpdatedAt = DateTime.UtcNow;
                verification.User.IsIdVerified = true;
                verification.User.UpdatedAt = DateTime.UtcNow;

                _context.AdminActions.Add(new AdminAction
                {
                    AdminId = adminId.Value,
                    ActionType = "Approve",
                    EntityType = "Verification",
                    EntityId = id,
                    ActionDetails = $"Approved ID verification for user {verification.User.FirstName} {verification.User.LastName}",
                    CreatedAt = DateTime.UtcNow
                });

                _context.Notifications.Add(new Notification
                {
                    UserId = verification.UserId,
                    Message = "Your ID verification has been approved!",
                    Type = NotificationType.VerificationApproved,
                    RelatedEntityId = verification.UserId,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        // --------------  REJECT -------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectVerification(int id, string rejectionReason)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null) return Fail("You are not signed-in as an admin.");

            if (string.IsNullOrWhiteSpace(rejectionReason))
                return Fail("Rejection reason is required.");

            var verification = await _context.VerificationDocuments
                                             .Include(v => v.User)
                                             .FirstOrDefaultAsync(v => v.Id == id);
            if (verification == null) return Fail("Verification not found.");

            try
            {
                verification.Status = VerificationStatus.Rejected;
                verification.RejectionReason = rejectionReason;
                verification.UpdatedAt = DateTime.UtcNow;

                _context.AdminActions.Add(new AdminAction
                {
                    AdminId = adminId.Value,
                    ActionType = "Reject",
                    EntityType = "Verification",
                    EntityId = id,
                    ActionDetails = $"Rejected ID verification for user {verification.User.FirstName} {verification.User.LastName}. Reason: {rejectionReason}",
                    CreatedAt = DateTime.UtcNow
                });

                _context.Notifications.Add(new Notification
                {
                    UserId = verification.UserId,
                    Message = $"Your ID verification has been rejected. Reason: {rejectionReason}",
                    Type = NotificationType.VerificationRejected,
                    RelatedEntityId = verification.UserId,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }


        public async Task<IActionResult> UsageReport()
        {
            // Check if admin is logged in
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction(nameof(AdminLogin));
            }

            // Get user statistics
            var users = await _context.Users
                .Select(u => new UserReportViewModel
                {
                    Id = u.Id,
                    FullName = $"{u.FirstName} {u.LastName}",
                    Email = u.Email,
                    SignUpDate = u.CreatedAt ?? DateTime.MinValue,
                    TotalListings = _context.Listings.Count(l => l.SellerId == u.Id),
                    TotalSales = _context.Purchases.Count(p => _context.Listings.Any(l => l.Id == p.ListingId && l.SellerId == u.Id)),
                    TotalPurchases = _context.Purchases.Count(p => p.BuyerId == u.Id),
                    LastActivity = _context.Listings
                        .Where(l => l.SellerId == u.Id)
                        .OrderByDescending(l => l.UpdatedAt ?? l.CreatedAt)
                        .Select(l => l.UpdatedAt ?? l.CreatedAt)
                        .FirstOrDefault() ?? u.UpdatedAt ?? u.CreatedAt ?? DateTime.MinValue
                })
                .OrderByDescending(u => u.LastActivity)
                .ToListAsync();

            // Get listing statistics
            var totalListings = await _context.Listings.CountAsync();
            var pendingListings = await _context.Listings.CountAsync(l => l.Status == ListingStatus.Pending);
            var approvedListings = await _context.Listings.CountAsync(l => l.Status == ListingStatus.Active);

            // Count listings with Sold status
            var soldListingsCount = await _context.Listings.CountAsync(l => l.Status == ListingStatus.Sold);

            // Count listings with Archived status
            var archivedListings = await _context.Listings.CountAsync(l => l.Status == ListingStatus.Archived);

            // Calculate total items sold by taking into account purchase quantities
            // Include purchases from listings with any status (including Archived)
            var totalItemsSold = await _context.Purchases
                .Where(p => p.PaymentStatus == PaymentStatus.Completed || p.PaymentStatus == PaymentStatus.Authorized)
                .SumAsync(p => p.Quantity);

            // Get category breakdown (include all listings in all statuses)
            var categoryStats = await _context.Listings
                .GroupBy(l => l.Category)
                .Select(g => new CategoryStatViewModel
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / (totalListings > 0 ? totalListings : 1) * 100
                })
                .OrderByDescending(c => c.Count)
                .ToListAsync();

            var reportViewModel = new AdminReportViewModel
            {
                Users = users,
                TotalUsers = users.Count,
                TotalListings = totalListings,
                PendingListings = pendingListings,
                ApprovedListings = approvedListings,
                SoldListings = totalItemsSold, // Using the total quantity sold
                ArchivedListings = archivedListings, // Added Archived listings count
                CategoryStats = categoryStats
            };

            return View(reportViewModel);
        }

        [HttpGet]
        public IActionResult ExportUsageReport()
        {
            // Check if admin is logged in
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction(nameof(AdminLogin));
            }

            // Generate CSV content
            var users = _context.Users
                .Select(u => new
                {
                    u.Id,
                    FullName = $"{u.FirstName} {u.LastName}",
                    u.Email,
                    SignUpDate = u.CreatedAt,
                    TotalListings = _context.Listings.Count(l => l.SellerId == u.Id),
                    TotalSales = _context.Purchases.Count(p => _context.Listings.Any(l => l.Id == p.ListingId && l.SellerId == u.Id)),
                    TotalPurchases = _context.Purchases.Count(p => p.BuyerId == u.Id),
                    LastActivity = _context.Listings
                        .Where(l => l.SellerId == u.Id)
                        .OrderByDescending(l => l.UpdatedAt ?? l.CreatedAt)
                        .Select(l => l.UpdatedAt ?? l.CreatedAt)
                        .FirstOrDefault() ?? u.UpdatedAt ?? u.CreatedAt
                })
                .ToList();

            // Build CSV content
            var csv = new StringBuilder();
            csv.AppendLine("User ID,Full Name,Email,Sign Up Date,Total Listings,Total Sales,Total Purchases,Last Activity");

            foreach (var user in users)
            {
                csv.AppendLine($"{user.Id},\"{user.FullName}\",{user.Email},{user.SignUpDate?.ToString("yyyy-MM-dd") ?? "N/A"},{user.TotalListings},{user.TotalSales},{user.TotalPurchases},{user.LastActivity?.ToString("yyyy-MM-dd") ?? "N/A"}");
            }

            byte[] bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"PrimeMarket_UserReport_{DateTime.Now.ToString("yyyyMMdd")}.csv");
        }
    }

}