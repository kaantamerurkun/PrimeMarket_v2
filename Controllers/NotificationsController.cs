using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Filters;
using PrimeMarket.Models;
using PrimeMarket.Models.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrimeMarket.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                var unreadCount = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                var notificationsList = notifications.Select(n => new
                {
                    id = n.Id,
                    message = n.Message,
                    type = n.Type.ToString(),
                    relatedEntityId = n.RelatedEntityId,
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt
                }).ToList();

                return Json(new { success = true, notifications = notificationsList, unreadCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error retrieving notifications: {ex.Message}" });
            }
        }

        [HttpPost]
        [UserAuthenticationFilter]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

                if (notification == null)
                {
                    return Json(new { success = false, message = "Notification not found" });
                }

                notification.IsRead = true;
                notification.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error marking notification as read: {ex.Message}" });
            }
        }

        [HttpPost]
        [UserAuthenticationFilter]
        
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Not logged in" });
                }

                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                if (unreadNotifications.Count == 0)
                {
                    return Json(new { success = true, message = "No unread notifications found" });
                }

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "All notifications marked as read", count = unreadNotifications.Count });
            }
            catch (Exception ex)
            {
                // Log the exception details
                Console.WriteLine($"Exception in MarkAllAsRead: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Return a more detailed error
                return Json(new
                {
                    success = false,
                    message = $"Error marking notifications as read: {ex.Message}",
                    details = ex.StackTrace
                });
            }
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                var unreadCount = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                return Json(new { success = true, unreadCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error getting unread count: {ex.Message}" });
            }
        }

        [HttpPost]
        [UserAuthenticationFilter]
        public async Task<IActionResult> DeleteNotification(int notificationId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

                if (notification == null)
                {
                    return Json(new { success = false, message = "Notification not found" });
                }

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting notification: {ex.Message}" });
            }
        }

        [HttpPost]
        [UserAuthenticationFilter]
        public async Task<IActionResult> ClearAll()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .ToListAsync();

                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error clearing notifications: {ex.Message}" });
            }
        }
        public async Task<IActionResult> List()
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var notifications = await _context.Notifications
                                          .Where(n => n.UserId == userId)
                                          .OrderByDescending(n => n.CreatedAt)
                                          .ToListAsync();
            return View(notifications);
        }
        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound();
            }

            // Mark as read
            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Redirect based on notification type
            switch (notification.Type)
            {
                case NotificationType.ListingApproved:
                case NotificationType.ListingRejected:
                    return RedirectToAction("MyListing", "User", new { id = notification.RelatedEntityId });
                case NotificationType.NewMessage:
                    // Need to get the conversation details first
                    var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == notification.RelatedEntityId);
                    if (message != null)
                    {
                        int otherUserId = message.SenderId == userId ? message.ReceiverId : message.SenderId;
                        return RedirectToAction("Conversation", "Message", new { userId = otherUserId, listingId = message.ListingId });
                    }
                    break;
                case NotificationType.NewOffer:
                case NotificationType.OfferAccepted:
                case NotificationType.OfferRejected:
                    var offer = await _context.Offers.FirstOrDefaultAsync(o => o.Id == notification.RelatedEntityId);
                    return RedirectToAction("Details", "Listing", new { id = offer?.ListingId ?? notification.RelatedEntityId });
                case NotificationType.VerificationApproved:
                case NotificationType.VerificationRejected:
                    // Update session verification status when user clicks on a verification notification
                    if (notification.Type == NotificationType.VerificationApproved)
                    {
                        var user = await _context.Users.FindAsync(userId);
                        if (user != null && user.IsIdVerified)
                        {
                            HttpContext.Session.SetString("IsUserVerified", "true");
                        }
                    }
                    return RedirectToAction("MyProfilePage", "User");
                case NotificationType.PurchaseCompleted:
                    var purchase = await _context.Purchases.FindAsync(notification.RelatedEntityId);
                    if (purchase != null)
                    {
                        if (purchase.BuyerId == userId)
                        {
                            return RedirectToAction("PurchaseComplete", "Payment", new { purchaseId = purchase.Id });
                        }
                        else
                        {
                            return RedirectToAction("MySales", "Payment");
                        }
                    }
                    break;
            }

            // Default fallback
            return RedirectToAction("Index");
        }
    }
}