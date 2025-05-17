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
using PrimeMarket.Models.Enum;

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

// Enhanced UpdateEmail Method
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

                // Send verification code to the new email using the enhanced method
                SendVerificationCodeForEmailUpdate(model.Email);

                // Redirect to the dedicated email update verification page
                return RedirectToAction("EmailUpdateVerification", new { email = model.Email });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to send verification email: " + ex.Message;
                return RedirectToAction("EditProfile");
            }
        }

        // New EmailUpdateVerification action
        [HttpGet]
        public IActionResult EmailUpdateVerification(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        // Enhanced method specifically for email updates
        private void SendVerificationCodeForEmailUpdate(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            // Remove any existing verification code for this email
            var existingCode = _context.EmailVerifications.FirstOrDefault(v => v.Email == email);
            if (existingCode != null)
                _context.EmailVerifications.Remove(existingCode);

            // Generate a new code
            var code = new Random().Next(100000, 999999).ToString();

            // Save the code
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
                        Subject = "PrimeMarket Email Address Update Verification",
                        IsBodyHtml = true,
                        Body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ width: 100%; max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #0066cc; color: white; padding: 15px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f8f9fa; border-radius: 0 0 5px 5px; }}
                        .code {{ font-size: 28px; font-weight: bold; text-align: center; 
                                 margin: 30px 0; color: #0066cc; letter-spacing: 5px; padding: 15px;
                                 background-color: #e9f0f8; border-radius: 10px; }}
                        .footer {{ font-size: 12px; text-align: center; margin-top: 30px; color: #666; }}
                        .note {{ font-size: 14px; color: #555; font-style: italic; margin-top: 20px; }}
                        .button {{ display: inline-block; background-color: #0066cc; color: white; 
                                  padding: 10px 20px; text-decoration: none; border-radius: 5px; 
                                  margin-top: 20px; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Email Update Verification</h1>
                        </div>
                        <div class='content'>
                            <p>Hello,</p>
                            <p>You have requested to update your email address on your PrimeMarket account. To complete this change, please use the verification code below:</p>
                            <div class='code'>{code}</div>
                            <p>This code will expire in 10 minutes for security reasons.</p>
                            <p class='note'>If you did not request to change your email address, please ignore this message or contact customer support immediately.</p>
                            <p>Thank you,<br>The PrimeMarket Team</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message, please do not reply to this email.</p>
                            <p>PrimeMarket &copy; 2024. All rights reserved.</p>
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
                throw new Exception($"Email sending error: {ex.Message}. Please check your email configuration.", ex);
            }
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

                // Set the verification status in ViewBag to be used in the view
                ViewBag.IsVerified = user.IsIdVerified;

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to load profile data. Please try again.";
                return RedirectToAction("MyProfilePage");
            }
        }
        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> MyOffers()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                // Get all offers made by this user
                var offers = await _context.Offers
                    .Include(o => o.Listing)
                    .ThenInclude(l => l.Images)
                    .Include(o => o.Listing.Seller)
                    .Where(o => o.BuyerId == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                var model = offers.Select(o => new OfferViewModel
                {
                    OfferId = o.Id,
                    ListingId = o.ListingId,
                    ListingTitle = o.Listing.Title,
                    ListingImage = o.Listing.Images?.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                                o.Listing.Images?.FirstOrDefault()?.ImagePath ??
                                "/images/placeholder.png",
                    SellerName = $"{o.Listing.Seller.FirstName} {o.Listing.Seller.LastName}",
                    OriginalPrice = o.Listing.Price,
                    OfferAmount = o.OfferAmount,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt ?? DateTime.MinValue,
                    UpdatedAt = o.UpdatedAt
                }).ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error getting user offers");
                TempData["ErrorMessage"] = "An error occurred while loading your offers.";
                return View(new List<OfferViewModel>());
            }
        }
        [HttpPost]
        [UserAuthenticationFilter]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOffer(int offerId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                // Get the offer and check it belongs to the user
                var offer = await _context.Offers
                    .Include(o => o.Listing)
                    .FirstOrDefaultAsync(o => o.Id == offerId && o.BuyerId == userId);

                if (offer == null)
                {
                    TempData["ErrorMessage"] = "Offer not found.";
                    return RedirectToAction("MyOffers");
                }

                // Check if the offer is in a state that can be canceled
                if (offer.Status != OfferStatus.Pending)
                {
                    TempData["ErrorMessage"] = "Only pending offers can be canceled.";
                    return RedirectToAction("MyOffers");
                }

                // Update the offer status
                offer.Status = OfferStatus.Cancelled;
                offer.UpdatedAt = DateTime.UtcNow;

                // Create notification for the seller
                var notification = new Notification
                {
                    UserId = offer.Listing.SellerId,
                    Message = $"A buyer has canceled their offer of {offer.OfferAmount:C} for your listing '{offer.Listing.Title}'.",
                    Type = NotificationType.OfferCancelled,
                    RelatedEntityId = offerId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);

                // Add a message to the conversation about the cancellation
                var message = new Message
                {
                    SenderId = userId.Value,
                    ReceiverId = offer.Listing.SellerId,
                    ListingId = offer.ListingId,
                    Content = $"I have canceled my offer of {offer.OfferAmount:C} for this item.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Messages.Add(message);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your offer has been canceled successfully.";
                return RedirectToAction("MyOffers");
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error canceling offer");
                TempData["ErrorMessage"] = "An error occurred while canceling your offer: " + ex.Message;
                return RedirectToAction("MyOffers");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitIdVerification(IFormFile idFront, IFormFile idBack, IFormFile facePhoto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "You must be logged in to verify your ID" });
            }

            if (idFront == null || idBack == null || facePhoto == null)
            {
                return Json(new { success = false, message = "Front ID, back ID, and face photo are all required" });
            }

            try
            {
                // Save the ID images and face photo
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "verification");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filenames
                string frontImageFileName = $"front_{userId}_{Guid.NewGuid()}{Path.GetExtension(idFront.FileName)}";
                string backImageFileName = $"back_{userId}_{Guid.NewGuid()}{Path.GetExtension(idBack.FileName)}";
                string faceImageFileName = $"face_{userId}_{Guid.NewGuid()}{Path.GetExtension(facePhoto.FileName)}";

                string frontImagePath = Path.Combine(uploadsFolder, frontImageFileName);
                string backImagePath = Path.Combine(uploadsFolder, backImageFileName);
                string faceImagePath = Path.Combine(uploadsFolder, faceImageFileName);

                // Save files to disk
                using (var frontStream = new FileStream(frontImagePath, FileMode.Create))
                {
                    await idFront.CopyToAsync(frontStream);
                }

                using (var backStream = new FileStream(backImagePath, FileMode.Create))
                {
                    await idBack.CopyToAsync(backStream);
                }

                using (var faceStream = new FileStream(faceImagePath, FileMode.Create))
                {
                    await facePhoto.CopyToAsync(faceStream);
                }

                // Check if user already has a verification document
                var existingVerification = await _context.VerificationDocuments.FirstOrDefaultAsync(v => v.UserId == userId);

                if (existingVerification != null)
                {
                    // Update existing document
                    existingVerification.FrontImagePath = $"/images/verification/{frontImageFileName}";
                    existingVerification.BackImagePath = $"/images/verification/{backImageFileName}";
                    existingVerification.FaceImagePath = $"/images/verification/{faceImageFileName}";
                    existingVerification.Status = PrimeMarket.Models.Enum.VerificationStatus.Pending;
                    existingVerification.RejectionReason = string.Empty;
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
                        FaceImagePath = $"/images/verification/{faceImageFileName}",
                        Status = PrimeMarket.Models.Enum.VerificationStatus.Pending,
                        RejectionReason = string.Empty,
                        CreatedAt = DateTime.Now
                    };

                    _context.VerificationDocuments.Add(verificationDocument);
                }

                await _context.SaveChangesAsync();

                // Create a notification for the user
                var userNotification = new Notification
                {
                    UserId = userId.Value,
                    Message = "Your ID verification has been submitted and is pending review",
                    Type = PrimeMarket.Models.Enum.NotificationType.VerificationApproved,
                    RelatedEntityId = userId.Value,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(userNotification);
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

                // Add a flag to indicate we've already sent a verification code
                TempData["VerificationSent"] = true;

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

            // Store verification status in session
            HttpContext.Session.SetString("IsUserVerified", user.IsIdVerified.ToString().ToLower());

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

        // Update the MyProfilePage action in UserController.cs:

        [HttpGet]
        [UserAuthenticationFilter]
        [Route("User/MyProfilePage")]
        public async Task<IActionResult> MyProfilePage()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound("User not found. Please try logging in again.");
            }

            var listings = await _context.Listings
                .Where(l => l.SellerId == userId.Value)
                .Include(l => l.Images)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var ratings = await _context.SellerRatings
                .Where(r => r.SellerId == userId.Value)
                .ToListAsync();

            double averageRating = 0;
            int totalRatings = 0;

            if (ratings.Any())
            {
                averageRating = ratings.Average(r => r.Rating);
                totalRatings = ratings.Count;
            }

            var viewModel = new UserProfileViewModel
            {
                User = user,
                Listings = listings,
                AverageRating = Math.Round(averageRating, 1),
                TotalRatings = totalRatings
            };

            return View(viewModel);
        }


        // Simple route methods for views
        [UserAuthenticationFilter]
        public IActionResult User_Listing_Details(int id)
        {
            ViewBag.ListingId = id;
            return View();
        }
        // UserController.cs - Updated User_MainPageAsync Method
        [UserAuthenticationFilter]
        public async Task<IActionResult> User_MainPageAsync()
        {
            var approvedListings = await _context.Listings
                .Where(l => l.Status == ListingStatus.Active &&
                           l.Status != ListingStatus.Archived &&
                           (!l.Stock.HasValue || l.Stock > 0))
                .Include(l => l.Images)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // Calculate average ratings for all listings
            ViewBag.ListingRatings = GetListingRatings(approvedListings.Select(l => l.Id).ToList());

            return View(approvedListings);
        }

        // Helper method to get average ratings for a list of listing IDs
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
                .Where(b => b.UserId == userId &&
                           b.Listing.Status == Models.Enum.ListingStatus.Active &&
                           b.Listing.Status != Models.Enum.ListingStatus.Archived)
                .ToListAsync();

            return View(bookmarks);
        }

        [UserAuthenticationFilter]
        public IActionResult LiveChat()
        {
            return View();
        }

        // Add this method to UserController.cs, replacing the existing OtherUserProfile method

        [UserAuthenticationFilter]
        public async Task<IActionResult> OtherUserProfile(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Get user's listings
            var listings = await _context.Listings
                .Where(l => l.SellerId == id && l.Status == ListingStatus.Active)
                .Include(l => l.Images)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // Get rating information
            var ratings = await _context.SellerRatings
                .Where(r => r.SellerId == id)
                .ToListAsync();

            double averageRating = 0;
            int totalRatings = 0;

            if (ratings.Any())
            {
                averageRating = ratings.Average(r => r.Rating);
                totalRatings = ratings.Count;
            }

            // Check if current user can rate this seller
            bool canRateUser = false;
            int userRating = 0;

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId != null)
            {
                var existingRating = ratings.FirstOrDefault(r => r.BuyerId == currentUserId);
                userRating = existingRating?.Rating ?? 0;

                var hasCompletedPurchase = await _context.Purchases
                    .Include(p => p.Listing)
                    .Include(p => p.Confirmation)
                    .AnyAsync(p => p.BuyerId == currentUserId &&
                                  p.Listing.SellerId == id &&
                                  p.PaymentStatus == PaymentStatus.Completed &&
                                  p.Confirmation != null &&
                                  p.Confirmation.BuyerReceivedProduct);

                canRateUser = hasCompletedPurchase;
            }

            var viewModel = new OtherUserProfileViewModel
            {
                User = user,
                Listings = listings,
                AverageRating = Math.Round(averageRating, 1),
                TotalRatings = totalRatings,
                CanRateUser = canRateUser,
                UserRating = userRating
            };

            return View(viewModel);
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
            return RedirectToAction("MyProfitLossReport", "Report");
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
            if (userId != listing.SellerId)
            {
                // Increment view count
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
            await _context.SaveChangesAsync();
            // Get the specific product details based on the category
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
                    case "Other Phones":
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
                    case "Other Tablets":
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

            ViewBag.Product = product;
            ViewBag.BookmarkCount = await _context.Bookmarks.CountAsync(b => b.ListingId == id);

            return View(listing);
        }

        #endregion
    }
}