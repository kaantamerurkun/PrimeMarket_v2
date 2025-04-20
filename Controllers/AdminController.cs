using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Models;
using PrimeMarket.Models.Enum;
using System;
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

        // POST: /Admin/AdminLogin
        // POST: /Admin/AdminLogin
        // Improved AdminLogin method with secure password comparison
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

            // Use a secure password hashing method
            if (admin == null)
            {
                ViewBag.ErrorMessage = "Invalid username or password.";
                return View();
            }

            // Either use your existing ComputeSha256Hash method from UserController
            // or implement a proper password verification method
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

        // Add this helper method if not already present
        //private string ComputeSha256Hash(string rawData)
        //{
        //    using (SHA256 sha256Hash = SHA256.Create())
        //    {
        //        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        //        StringBuilder builder = new StringBuilder();
        //        foreach (var b in bytes)
        //            builder.Append(b.ToString("x2"));
        //        return builder.ToString();
        //    }
        //}


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

        // Add these methods to the AdminController class

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
            // This is a simplified approach - you'll need to handle different product types
            dynamic product = null;

            if (listing.SubCategory == "IOS Phone")
                product = await _context.IOSPhones.FirstOrDefaultAsync(p => p.ListingId == id);
            else if (listing.SubCategory == "Android Phone")
                product = await _context.AndroidPhones.FirstOrDefaultAsync(p => p.ListingId == id);
            else if (listing.SubCategory == "Laptops")
                product = await _context.Laptops.FirstOrDefaultAsync(p => p.ListingId == id);
            // Add more conditions for other product types

            ViewBag.Product = product;
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
                listing.Status = ListingStatus.Approved;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectListing(int id, string rejectionReason)
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

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["ErrorMessage"] = "Rejection reason is required.";
                return RedirectToAction(nameof(ListingDetails), new { id });
            }

            try
            {
                listing.Status = ListingStatus.Rejected;
                listing.RejectionReason = rejectionReason;
                listing.UpdatedAt = DateTime.UtcNow;

                // Create an admin action record
                var adminAction = new AdminAction
                {
                    AdminId = adminId.Value,
                    ActionType = "Reject",
                    EntityType = "Listing",
                    EntityId = id,
                    ActionDetails = $"Rejected listing: {listing.Title}, Reason: {rejectionReason}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.AdminActions.Add(adminAction);

                // Create a notification for the seller
                var notification = new Notification
                {
                    UserId = listing.SellerId,
                    Message = $"Your listing '{listing.Title}' has been rejected. Reason: {rejectionReason}",
                    Type = NotificationType.ListingRejected,
                    RelatedEntityId = id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Listing rejected successfully.";
                return RedirectToAction(nameof(PendingListings));
            }
            catch (Exception ex)
            {
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

            return View(verification);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveVerification(int id)
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

            try
            {
                verification.Status = VerificationStatus.Approved;
                verification.UpdatedAt = DateTime.UtcNow;

                // Update user's ID verification status
                verification.User.IsIdVerified = true;
                verification.User.UpdatedAt = DateTime.UtcNow;

                // Create an admin action record
                var adminAction = new AdminAction
                {
                    AdminId = adminId.Value,
                    ActionType = "Approve",
                    EntityType = "Verification",
                    EntityId = id,
                    ActionDetails = $"Approved ID verification for user: {verification.User.FirstName} {verification.User.LastName}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.AdminActions.Add(adminAction);

                // Create a notification for the user
                // Create a notification for the user
                var notification = new Notification
                {
                    UserId = verification.UserId,
                    Message = "Your ID verification has been approved!",
                    Type = NotificationType.VerificationApproved,
                    RelatedEntityId = verification.UserId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "ID verification approved successfully.";
                return RedirectToAction(nameof(PendingVerifications));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error approving verification: {ex.Message}";
                return RedirectToAction(nameof(VerificationDetails), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectVerification(int id, string rejectionReason)
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

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["ErrorMessage"] = "Rejection reason is required.";
                return RedirectToAction(nameof(VerificationDetails), new { id });
            }

            try
            {
                verification.Status = VerificationStatus.Rejected;
                verification.RejectionReason = rejectionReason;
                verification.UpdatedAt = DateTime.UtcNow;

                // Create an admin action record
                var adminAction = new AdminAction
                {
                    AdminId = adminId.Value,
                    ActionType = "Reject",
                    EntityType = "Verification",
                    EntityId = id,
                    ActionDetails = $"Rejected ID verification for user: {verification.User.FirstName} {verification.User.LastName}, Reason: {rejectionReason}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.AdminActions.Add(adminAction);

                // Create a notification for the user
                var notification = new Notification
                {
                    UserId = verification.UserId,
                    Message = $"Your ID verification has been rejected. Reason: {rejectionReason}",
                    Type = NotificationType.VerificationRejected,
                    RelatedEntityId = verification.UserId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "ID verification rejected successfully.";
                return RedirectToAction(nameof(PendingVerifications));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error rejecting verification: {ex.Message}";
                return RedirectToAction(nameof(VerificationDetails), new { id });
            }
        }

        [HttpGet]
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
            var approvedListings = await _context.Listings.CountAsync(l => l.Status == ListingStatus.Approved);
            var soldListings = await _context.Listings.CountAsync(l => l.Status == ListingStatus.Sold);

            // Get category breakdown
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
                SoldListings = soldListings,
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

            // Implement CSV export logic here
            // For simplicity, we'll generate a sample CSV
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


        // ViewModel classes for admin dashboard
        public class UserReportViewModel
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public DateTime SignUpDate { get; set; }
            public int TotalListings { get; set; }
            public int TotalSales { get; set; }
            public int TotalPurchases { get; set; }
            public DateTime LastActivity { get; set; }

            // Calculated property for display
            public int DaysSinceLastActive => (DateTime.Now - LastActivity).Days;
        }

        public class CategoryStatViewModel
        {
            public string Category { get; set; }
            public int Count { get; set; }
            public double Percentage { get; set; }
        }

        public class AdminReportViewModel
        {
            public List<UserReportViewModel> Users { get; set; }
            public int TotalUsers { get; set; }
            public int TotalListings { get; set; }
            public int PendingListings { get; set; }
            public int ApprovedListings { get; set; }
            public int SoldListings { get; set; }
            public List<CategoryStatViewModel> CategoryStats { get; set; }
        }

        public class AdminLoginModel
        {

            public string Username { get; set; }
            public string Password { get; set; }
        }
    
}