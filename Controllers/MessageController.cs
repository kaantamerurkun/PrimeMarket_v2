using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Models.ViewModel;
using PrimeMarket.Filters;
using PrimeMarket.Models;
using PrimeMarket.Models.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrimeMarket.Controllers
{
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MessageController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> Conversations()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Get all unique conversations (grouped by other user and listing)
            var sentMessages = await _context.Messages
                .Include(m => m.Receiver)
                .Include(m => m.Listing)
                .ThenInclude(l => l.Images)
                .Where(m => m.SenderId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var receivedMessages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Listing)
                .ThenInclude(l => l.Images)
                .Where(m => m.ReceiverId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            // Combine and group by conversation
            var conversationGroups = new Dictionary<string, ConversationViewModel>();

            foreach (var message in sentMessages)
            {
                var key = $"{message.ReceiverId}_{message.ListingId}";
                if (!conversationGroups.ContainsKey(key))
                {
                    conversationGroups[key] = new ConversationViewModel
                    {
                        OtherUserId = message.ReceiverId,
                        OtherUserName = $"{message.Receiver.FirstName} {message.Receiver.LastName}",
                        OtherUserProfileImage = message.Receiver.ProfileImagePath,
                        ListingId = message.ListingId,
                        ListingTitle = message.Listing.Title,
                        ListingImage = message.Listing.Images.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                                      message.Listing.Images.FirstOrDefault()?.ImagePath,
                        LastMessageTime = message.CreatedAt ?? DateTime.MinValue,
                        LastMessageContent = message.Content,
                        UnreadCount = 0 // You sent this message, so it's read for you
                    };
                }
                else if (message.CreatedAt > conversationGroups[key].LastMessageTime)
                {
                    conversationGroups[key].LastMessageTime = message.CreatedAt ?? DateTime.MinValue;
                    conversationGroups[key].LastMessageContent = message.Content;
                }
            }

            foreach (var message in receivedMessages)
            {
                var key = $"{message.SenderId}_{message.ListingId}";
                if (!conversationGroups.ContainsKey(key))
                {
                    conversationGroups[key] = new ConversationViewModel
                    {
                        OtherUserId = message.SenderId,
                        OtherUserName = $"{message.Sender.FirstName} {message.Sender.LastName}",
                        OtherUserProfileImage = message.Sender.ProfileImagePath,
                        ListingId = message.ListingId,
                        ListingTitle = message.Listing.Title,
                        ListingImage = message.Listing.Images.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                                      message.Listing.Images.FirstOrDefault()?.ImagePath,
                        LastMessageTime = message.CreatedAt ?? DateTime.MinValue,
                        LastMessageContent = message.Content,
                        UnreadCount = message.IsRead ? 0 : 1
                    };
                }
                else
                {
                    if (message.CreatedAt > conversationGroups[key].LastMessageTime)
                    {
                        conversationGroups[key].LastMessageTime = message.CreatedAt ?? DateTime.MinValue;
                        conversationGroups[key].LastMessageContent = message.Content;
                    }

                    if (!message.IsRead)
                    {
                        conversationGroups[key].UnreadCount++;
                    }
                }
            }

            var conversations = conversationGroups.Values.OrderByDescending(c => c.LastMessageTime).ToList();
            return View(conversations);
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> Conversation(int userId, int listingId)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Get the other user
            var otherUser = await _context.Users.FindAsync(userId);
            if (otherUser == null)
            {
                return NotFound();
            }

            // Get the listing
            var listing = await _context.Listings
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null)
            {
                return NotFound();
            }

            // Get all messages between these users about this listing
            var messages = await _context.Messages
                .Where(m => ((m.SenderId == currentUserId && m.ReceiverId == userId) ||
                             (m.SenderId == userId && m.ReceiverId == currentUserId)) &&
                            m.ListingId == listingId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // Mark unread messages as read
            var unreadMessages = messages.Where(m => m.ReceiverId == currentUserId && !m.IsRead).ToList();
            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }
            await _context.SaveChangesAsync();

            // Get current user profile image
            var currentUser = await _context.Users.FindAsync(currentUserId);
            ViewBag.CurrentUserImage = currentUser?.ProfileImagePath;

            var conversationViewModel = new DetailedConversationViewModel
            {
                OtherUserId = userId,
                OtherUserName = $"{otherUser.FirstName} {otherUser.LastName}",
                OtherUserProfileImage = otherUser.ProfileImagePath,
                ListingId = listingId,
                ListingTitle = listing.Title,
                ListingPrice = listing.Price,
                ListingImage = listing.Images.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              listing.Images.FirstOrDefault()?.ImagePath,
                CurrentUserId = currentUserId.Value,
                Messages = messages.Select(m => new MessageViewModel
                {
                    Id = m.Id,
                    Content = m.Content,
                    SentByMe = m.SenderId == currentUserId,
                    Timestamp = m.CreatedAt ?? DateTime.MinValue
                }).ToList()
            };

            return View(conversationViewModel);
        }

        [HttpPost]
        [UserAuthenticationFilter]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(SendMessageViewModel model)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (string.IsNullOrWhiteSpace(model.Content))
            {
                ModelState.AddModelError("", "Message content cannot be empty.");
                return RedirectToAction(nameof(Conversation), new { userId = model.ReceiverId, listingId = model.ListingId });
            }

            try
            {
                var message = new Message
                {
                    SenderId = currentUserId.Value,
                    ReceiverId = model.ReceiverId,
                    ListingId = model.ListingId,
                    Content = model.Content,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);

                // Create notification for the receiver
                var notification = new Notification
                {
                    UserId = model.ReceiverId,
                    Message = "You have received a new message",
                    Type = NotificationType.NewMessage,
                    RelatedEntityId = model.ListingId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Conversation), new { userId = model.ReceiverId, listingId = model.ListingId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error sending message: {ex.Message}");
                return RedirectToAction(nameof(Conversation), new { userId = model.ReceiverId, listingId = model.ListingId });
            }
        }

        [HttpPost]
        [UserAuthenticationFilter]
        public async Task<IActionResult> SendMessageAjax([FromBody] SendMessageViewModel model)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            if (string.IsNullOrWhiteSpace(model.Content))
            {
                return Json(new { success = false, message = "Message content cannot be empty" });
            }

            try
            {
                var message = new Message
                {
                    SenderId = currentUserId.Value,
                    ReceiverId = model.ReceiverId,
                    ListingId = model.ListingId,
                    Content = model.Content,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);

                // Create notification for the receiver
                var notification = new Notification
                {
                    UserId = model.ReceiverId,
                    Message = "You have received a new message",
                    Type = NotificationType.NewMessage,
                    RelatedEntityId = model.ListingId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = new
                    {
                        id = message.Id,
                        content = message.Content,
                        sentByMe = true,
                        timestamp = message.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error sending message: {ex.Message}" });
            }
        }

        [HttpPost]
        [UserAuthenticationFilter]
        public async Task<IActionResult> MarkAsRead(int messageId)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                {
                    return Json(new { success = false, message = "Message not found" });
                }

                if (message.ReceiverId != currentUserId)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                message.IsRead = true;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error marking message as read: {ex.Message}" });
            }
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                var unreadCount = await _context.Messages
                    .CountAsync(m => m.ReceiverId == currentUserId && !m.IsRead);

                return Json(new { success = true, unreadCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error getting unread count: {ex.Message}" });
            }
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> StartConversation(int userId, int listingId)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Check if the listing exists
            var listing = await _context.Listings
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null)
            {
                return NotFound();
            }

            // If userId is 0, set it to the seller's ID
            if (userId == 0)
            {
                userId = listing.SellerId;
            }

            // Check if the user exists
            var otherUser = await _context.Users.FindAsync(userId);
            if (otherUser == null)
            {
                return NotFound();
            }

            // Check if trying to message yourself
            if (userId == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot message yourself.";
                return RedirectToAction("Details", "Listing", new { id = listingId });
            }

            // Check if a conversation already exists
            var existingConversation = await _context.Messages
                .AnyAsync(m => (
                    (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                    (m.SenderId == userId && m.ReceiverId == currentUserId)
                ) && m.ListingId == listingId);

            // If conversation exists, redirect to it
            if (existingConversation)
            {
                return RedirectToAction("Conversation", new { userId, listingId });
            }

            // Otherwise create a view model for a new conversation
            var currentUser = await _context.Users.FindAsync(currentUserId);
            ViewBag.CurrentUserImage = currentUser?.ProfileImagePath;

            var conversationViewModel = new DetailedConversationViewModel
            {
                OtherUserId = userId,
                OtherUserName = $"{otherUser.FirstName} {otherUser.LastName}",
                OtherUserProfileImage = otherUser.ProfileImagePath,
                ListingId = listingId,
                ListingTitle = listing.Title,
                ListingPrice = listing.Price,
                ListingImage = listing.Images?.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              listing.Images?.FirstOrDefault()?.ImagePath,
                CurrentUserId = currentUserId.Value,
                Messages = new List<MessageViewModel>()
            };

            return View("Conversation", conversationViewModel);
        }
    }
}