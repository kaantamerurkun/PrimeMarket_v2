using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Mail;
using PrimeMarket.Filters;
using PrimeMarket.Models.ViewModel;

namespace PrimeMarket.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailSettings _emailSettings;

        public UserController(ApplicationDbContext context, IOptions<EmailSettings> emailOptions)
        {
            _context = context;
            _emailSettings = emailOptions.Value;
        }

        #region Profile Management

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(EditProfileViewModel model, IFormFile profileImage)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

            if (profileImage != null && profileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(profileImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await profileImage.CopyToAsync(stream);
                user.ProfileImagePath = $"/images/profiles/{uniqueFileName}";
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("EditProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmail(EditProfileViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Email) || user.Email == model.Email)
            {
                return RedirectToAction("EditProfile");
            }

            var emailInUse = await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId);
            if (emailInUse)
            {
                ModelState.AddModelError("Email", "This email is already in use.");
                TempData["ErrorMessage"] = "Email is already in use by another account.";
                return RedirectToAction("EditProfile");
            }

            try
            {
                // Store the new email in TempData so we can access it after verification
                TempData["NewEmail"] = model.Email;
                TempData["IsEmailUpdate"] = true;

                // Send verification code to the new email
                SendVerificationCodeInternal(model.Email);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to send verification email: " + ex.Message;
                return RedirectToAction("EditProfile");
            }

            // Redirect to the email verification page
            return RedirectToAction("EmailVerification", new { email = model.Email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(EditProfileViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(model.CurrentPassword) ||
                string.IsNullOrEmpty(model.NewPassword) ||
                string.IsNullOrEmpty(model.ConfirmNewPassword))
            {
                TempData["ErrorMessage"] = "All password fields are required.";
                return RedirectToAction("EditProfile");
            }

            var currentHash = ComputeSha256Hash(model.CurrentPassword);
            if (user.PasswordHash != currentHash)
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToAction("EditProfile");
            }

            if (model.NewPassword != model.ConfirmNewPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation do not match.";
                return RedirectToAction("EditProfile");
            }

            user.PasswordHash = ComputeSha256Hash(model.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Password updated successfully.";
            return RedirectToAction("EditProfile");
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                var model = new EditProfileViewModel
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email,
                    ProfileImagePath = user.ProfileImagePath
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to load profile data. Please try again.";
                return RedirectToAction("MyProfilePage");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitIdVerification(IFormFile idFront, IFormFile idBack)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "You must be logged in to verify your ID" });
            }

            if (idFront == null || idBack == null)
            {
                return Json(new { success = false, message = "Both front and back ID images are required" });
            }

            try
            {
                // Save the ID images
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "verification");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filenames
                string frontImageFileName = $"front_{userId}_{Guid.NewGuid()}{Path.GetExtension(idFront.FileName)}";
                string backImageFileName = $"back_{userId}_{Guid.NewGuid()}{Path.GetExtension(idBack.FileName)}";

                string frontImagePath = Path.Combine(uploadsFolder, frontImageFileName);
                string backImagePath = Path.Combine(uploadsFolder, backImageFileName);

                // Save files to disk
                using (var frontStream = new FileStream(frontImagePath, FileMode.Create))
                {
                    await idFront.CopyToAsync(frontStream);
                }

                using (var backStream = new FileStream(backImagePath, FileMode.Create))
                {
                    await idBack.CopyToAsync(backStream);
                }

                // Check if user already has a verification document
                var existingVerification = await _context.VerificationDocuments.FirstOrDefaultAsync(v => v.UserId == userId);

                if (existingVerification != null)
                {
                    // Update existing document
                    existingVerification.FrontImagePath = $"/images/verification/{frontImageFileName}";
                    existingVerification.BackImagePath = $"/images/verification/{backImageFileName}";
                    existingVerification.Status = PrimeMarket.Models.Enum.VerificationStatus.Pending;
                    existingVerification.RejectionReason = null;
                    existingVerification.UpdatedAt = DateTime.Now;
                }
                else
                {
                    // Create new verification document
                    var verificationDocument = new VerificationDocument
                    {
                        UserId = userId.Value,
                        FrontImagePath = $"/images/verification/{frontImageFileName}",
                        BackImagePath = $"/images/verification/{backImageFileName}",
                        Status = PrimeMarket.Models.Enum.VerificationStatus.Pending,
                        CreatedAt = DateTime.Now
                    };

                    _context.VerificationDocuments.Add(verificationDocument);
                }

                await _context.SaveChangesAsync();

                // Create a notification for admins
                var adminIds = await _context.Admins.Select(a => a.Id).ToListAsync();
                foreach (var adminId in adminIds)
                {
                    var notification = new Notification
                    {
                        UserId = adminId,
                        Message = "New ID verification submitted",
                        Type = PrimeMarket.Models.Enum.NotificationType.ListingApproved, // Using this for now as a generic notification
                        RelatedEntityId = userId.Value,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);
                }
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "ID verification submitted successfully. Our team will review your documents." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while processing your verification: " + ex.Message });
            }
        }

        #endregion

        #region Authentication

        public IActionResult Login()
        {
            // If already logged in, redirect to main page
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("User_MainPage");
            }

            return View();
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            // If already logged in, redirect to main page
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("User_MainPage");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (_context.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email is already registered.");
                return View(model);
            }

            string hashedPassword = ComputeSha256Hash(model.Password);

            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PasswordHash = hashedPassword,
                IsAdmin = false,
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send verification code 
            try
            {
                SendVerificationCodeInternal(model.Email);
                return RedirectToAction("EmailVerification", "User", new { email = model.Email });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "We encountered an issue sending the verification email. Please try again later.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult EmailVerification(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public IActionResult SendVerificationCode([FromForm] string email)
        {
            try
            {
                SendVerificationCodeInternal(email);
                return Ok(new { message = "Verification code sent." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to send verification code: {ex.Message}");
            }
        }

        private void SendVerificationCodeInternal(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                throw new Exception("User not found.");

            var existingCode = _context.EmailVerifications.FirstOrDefault(v => v.Email == email);
            if (existingCode != null)
                _context.EmailVerifications.Remove(existingCode);

            var code = new Random().Next(100000, 999999).ToString();

            _context.EmailVerifications.Add(new EmailVerification
            {
                Email = email,
                Code = code,
                Expiration = DateTime.UtcNow.AddMinutes(10)
            });

            _context.SaveChanges();

            try
            {
                using (var smtpClient = new SmtpClient(_emailSettings.Host))
                {
                    smtpClient.Port = _emailSettings.Port;
                    smtpClient.Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword);
                    smtpClient.EnableSsl = _emailSettings.EnableSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                        Subject = "Your PrimeMarket Verification Code",
                        IsBodyHtml = true,
                        Body = $@"
                        <html>
                        <head>
                            <style>
                                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                                .container {{ width: 100%; max-width: 600px; margin: 0 auto; padding: 20px; }}
                                .header {{ background-color: #0066cc; color: white; padding: 10px; text-align: center; }}
                                .content {{ padding: 20px; }}
                                .code {{ font-size: 24px; font-weight: bold; text-align: center; 
                                         margin: 30px 0; color: #0066cc; letter-spacing: 3px; }}
                                .footer {{ font-size: 12px; text-align: center; margin-top: 30px; color: #666; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>
                                    <h1>PrimeMarket Email Verification</h1>
                                </div>
                                <div class='content'>
                                    <p>Hello,</p>
                                    <p>Thank you for registering with PrimeMarket. To complete your registration, please use the verification code below:</p>
                                    <div class='code'>{code}</div>
                                    <p>This code will expire in 10 minutes for security reasons.</p>
                                    <p>If you did not request this code, please ignore this email.</p>
                                    <p>Thank you,<br>The PrimeMarket Team</p>
                                </div>
                                <div class='footer'>
                                    <p>This is an automated message, please do not reply to this email.</p>
                                </div>
                            </div>
                        </body>
                        </html>"
                    };

                    mailMessage.To.Add(email);
                    smtpClient.Send(mailMessage);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"SMTP Error: {ex.Message}. Please check your email configuration.", ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmailCode([FromForm] string email, [FromForm] string code)
        {
            var verification = await _context.EmailVerifications
                .FirstOrDefaultAsync(v => v.Email == email && v.Code == code);

            if (verification == null || verification.Expiration < DateTime.UtcNow)
                return BadRequest("Invalid or expired verification code.");

            // Check if this is an email update or new account verification
            bool isEmailUpdate = TempData["IsEmailUpdate"] != null && (bool)TempData["IsEmailUpdate"];

            if (isEmailUpdate)
            {
                // This is an email change verification
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return NotFound("User not found.");

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound("User not found.");

                // Update the user's email
                user.Email = email;
                user.IsEmailVerified = true;
                user.UpdatedAt = DateTime.UtcNow;

                // Update session with new email
                HttpContext.Session.SetString("UserEmail", user.Email);

                // Set success message
                TempData["SuccessMessage"] = "Your email has been successfully updated and verified.";
            }
            else
            {
                // This is a new account verification
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                    return NotFound("User not found.");

                user.IsEmailVerified = true;
                user.UpdatedAt = DateTime.UtcNow;
            }

            // Remove the verification code from database
            _context.EmailVerifications.Remove(verification);
            await _context.SaveChangesAsync();

            // Determine redirect URL based on type of verification
            string redirectUrl = isEmailUpdate
                ? Url.Action("EditProfile", "User")
                : Url.Action("Login", "User");

            return Ok(new { redirectUrl });
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.ErrorMessage = "Please enter both email and password.";
                return View();
            }

            // Find the user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Check if user exists and password is correct
            if (user == null)
            {
                ViewBag.ErrorMessage = "Invalid email or password.";
                return View();
            }

            // Compute SHA256 hash of the provided password
            string hashedPassword = ComputeSha256Hash(password);

            // Verify the password
            if (user.PasswordHash != hashedPassword)
            {
                ViewBag.ErrorMessage = "Invalid email or password.";
                return View();
            }

            // Check if email is verified
            if (!user.IsEmailVerified)
            {
                ViewBag.ErrorMessage = "Please verify your email before logging in.";
                return RedirectToAction("EmailVerification", new { email = user.Email });
            }

            // Store user information in session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
            HttpContext.Session.SetString("UserEmail", user.Email);

            return RedirectToAction("User_MainPage");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();

            // Redirect to home page
            return RedirectToAction("Index", "Home");
        }

        // GET: /User/ResetPassword
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendResetCode([FromForm] string email)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                    return Json(new { success = false, message = "No account exists with this email address." });

                // Check for existing reset code
                var existingCode = await _context.EmailVerifications.FirstOrDefaultAsync(v => v.Email == email);
                if (existingCode != null)
                    _context.EmailVerifications.Remove(existingCode);

                // Generate a random 6-digit code
                var code = new Random().Next(100000, 999999).ToString();

                // Save new verification code to database
                _context.EmailVerifications.Add(new EmailVerification
                {
                    Email = email,
                    Code = code,
                    Expiration = DateTime.UtcNow.AddMinutes(10)
                });
                await _context.SaveChangesAsync();

                // Send email with verification code
                using (var smtpClient = new SmtpClient(_emailSettings.Host))
                {
                    smtpClient.Port = _emailSettings.Port;
                    smtpClient.Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword);
                    smtpClient.EnableSsl = _emailSettings.EnableSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                        Subject = "Your PrimeMarket Password Reset Code",
                        IsBodyHtml = true,
                        Body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ width: 100%; max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #0066cc; color: white; padding: 10px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .code {{ font-size: 24px; font-weight: bold; text-align: center; 
                                 margin: 30px 0; color: #0066cc; letter-spacing: 3px; }}
                        .footer {{ font-size: 12px; text-align: center; margin-top: 30px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>PrimeMarket Password Reset</h1>
                        </div>
                        <div class='content'>
                            <p>Hello,</p>
                            <p>You requested to reset your password. Please use the verification code below:</p>
                            <div class='code'>{code}</div>
                            <p>This code will expire in 10 minutes for security reasons.</p>
                            <p>If you did not request this code, please ignore this email.</p>
                            <p>Thank you,<br>The PrimeMarket Team</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message, please do not reply to this email.</p>
                        </div>
                    </div>
                </body>
                </html>"
                    };

                    mailMessage.To.Add(email);
                    smtpClient.Send(mailMessage);
                }

                return Json(new { success = true, message = "Verification code sent to your email." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error sending verification code: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromForm] string email, [FromForm] string code, [FromForm] string newPassword)
        {
            try
            {
                // Verify the code
                var verification = await _context.EmailVerifications
                    .FirstOrDefaultAsync(v => v.Email == email && v.Code == code);

                if (verification == null || verification.Expiration < DateTime.UtcNow)
                    return Json(new { success = false, message = "Invalid or expired verification code." });

                // Get the user
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                    return Json(new { success = false, message = "User not found." });

                // Update password
                string hashedPassword = ComputeSha256Hash(newPassword);
                user.PasswordHash = hashedPassword;
                user.UpdatedAt = DateTime.UtcNow;

                // Remove the verification code
                _context.EmailVerifications.Remove(verification);

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Password reset successfully.", redirectUrl = Url.Action("Login", "User") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error resetting password: {ex.Message}" });
            }
        }

        #endregion

        #region User Pages

        [UserAuthenticationFilter]
        public async Task<IActionResult> MyProfilePage()
        {
            // Get the user ID from session
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            // Always fetch the fresh user data from database
            var user = await _context.Users
                .AsNoTracking() // To ensure we get fresh data
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound("User not found. Please try logging in again.");
            }

            // Pass the user object to the view
            return View(user);
        }

        // Simple route methods for views
        [UserAuthenticationFilter]
        public IActionResult User_Listing_Details(int id)
        {
            ViewBag.ListingId = id;
            return View();
        }

        [UserAuthenticationFilter]
        public IActionResult User_MainPage()
        {
            return View();
        }

        [UserAuthenticationFilter]
        public async Task<IActionResult> MyBookmarks()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var bookmarks = await _context.Bookmarks
                .Include(b => b.Listing)
                .ThenInclude(l => l.Images)
                .Where(b => b.UserId == userId)
                .ToListAsync();

            return View(bookmarks);
        }

        [UserAuthenticationFilter]
        public IActionResult LiveChat()
        {
            return View();
        }

        [UserAuthenticationFilter]
        public async Task<IActionResult> OtherUserProfile(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [UserAuthenticationFilter]
        public IActionResult CreateListing()
        {
            // Check if user is authenticated
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [UserAuthenticationFilter]
        public IActionResult MyShoppingCart()
        {
            return View();
        }

        [UserAuthenticationFilter]
        public IActionResult PaymentPage()
        {
            return View();
        }

        [UserAuthenticationFilter]
        public IActionResult MyProfitLossReport()
        {
            return View();
        }

        [UserAuthenticationFilter]
        public IActionResult AllMessages()
        {
            return View();
        }

        [UserAuthenticationFilter]
        public IActionResult AllNotifications()
        {
            return View();
        }

        [UserAuthenticationFilter]
        public async Task<IActionResult> MyListing(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
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
            dynamic product = null;

            if (listing.SubCategory == "IOS Phone")
                product = await _context.IOSPhones.FirstOrDefaultAsync(p => p.ListingId == id);
            else if (listing.SubCategory == "Android Phone")
                product = await _context.AndroidPhones.FirstOrDefaultAsync(p => p.ListingId == id);
            else if (listing.SubCategory == "Laptops")
                product = await _context.Laptops.FirstOrDefaultAsync(p => p.ListingId == id);
            else if (listing.SubCategory == "Desktops")
                product = await _context.Desktops.FirstOrDefaultAsync(p => p.ListingId == id);
            else if (listing.SubCategory == "Tablets")
            {
                product = await _context.IOSTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                if (product == null)
                {
                    product = await _context.AndroidTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                }
                if (product == null)
                {
                    product = await _context.OtherTablets.FirstOrDefaultAsync(p => p.ListingId == id);
                }
            }
            else if (listing.SubCategory == "Washers")
                product = await _context.Washers.FirstOrDefaultAsync(p => p.ListingId == id);
            else if (listing.SubCategory == "Dishwashers")
                product = await _context.Dishwashers.FirstOrDefaultAsync(p => p.ListingId == id);
            else if (listing.SubCategory == "Fridges")
                product = await _context.Fridges.FirstOrDefaultAsync(p => p.ListingId == id);
            else if (listing.SubCategory == "Ovens")
                product = await _context.Ovens.FirstOrDefaultAsync(p => p.ListingId == id);
            else if (listing.SubCategory == "Vacuum Cleaner")
                product = await _context.VacuumCleaners.FirstOrDefaultAsync(p => p.ListingId == id);
            else if (listing.SubCategory == "Televisions")
                product = await _context.Televisions.FirstOrDefaultAsync(p => p.ListingId == id);

            ViewBag.Product = product;

            return View(listing);
        }

        #endregion
    }
}