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

            if (user.Email != model.Email)
            {
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

                // Redirect to the existing email verification page
                return RedirectToAction("EmailVerification", new { email = model.Email });
            }

            return RedirectToAction("EditProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(EditProfileViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword) || string.IsNullOrEmpty(model.ConfirmNewPassword))
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
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Password updated successfully.";
            return RedirectToAction("EditProfile");
        }

        public IActionResult Login() => View();

        [HttpGet]
        public IActionResult SignUp() => View();

        [HttpPost]
        public IActionResult SignUp(SignUpViewModel model)
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
                IsEmailVerified = false
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // Send verification code 
            try
            {
                SendVerificationCodeInternal(model.Email);
                return RedirectToAction("EmailVerification", "User", new { email = model.Email });
            }
            catch (Exception ex)
            {
                // Log the error (ideally to a proper logging system)
                Console.WriteLine($"Error sending verification email: {ex.Message}");

                // Add a more user-friendly error
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
                // Add better error handling
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
        public IActionResult Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();

            // Redirect to home page
            return RedirectToAction("Index", "Home");
        }

        // Other user views
        // GET: /User/ResetPassword
        public IActionResult ResetPassword() => View();

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

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            // Get the user ID from session
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                // Fetch user data from database
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Create view model for the form
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
                // Log the error
                Console.Error.WriteLine($"Error loading profile: {ex.Message}");

                // Add error to TempData
                TempData["ErrorMessage"] = "Failed to load profile data. Please try again.";

                // Redirect to profile page
                return RedirectToAction("MyProfilePage");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model, IFormFile profileImage)
        {
            try
            {
                // Basic model validation
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Get the user ID from session
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login");
                }

                // Fetch user from database
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    ModelState.AddModelError("", "User not found. Please try logging in again.");
                    return View(model);
                }

                // Update user information
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;

                // Handle phone number - add null check and trimming
                Console.WriteLine($"Phone number from form: '{model.PhoneNumber}'");
                user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
                Console.WriteLine($"Phone number to save: '{user.PhoneNumber}'");

                // Handle profile image upload if provided
                if (profileImage != null && profileImage.Length > 0)
                {
                    try
                    {
                        // Define upload directory path (create if it doesn't exist)
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Generate unique filename
                        var uniqueFileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(profileImage.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Save the file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await profileImage.CopyToAsync(fileStream);
                        }

                        // Update user profile image path
                        user.ProfileImagePath = $"/images/profiles/{uniqueFileName}";
                        Console.WriteLine($"Profile image updated: {user.ProfileImagePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Image upload error: {ex.Message}");
                        // Continue without failing the whole operation
                    }
                }

                // Update email if changed and not already in use by another account
                if (user.Email != model.Email)
                {
                    // Check if the new email is already in use
                    if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId))
                    {
                        ModelState.AddModelError("Email", "This email is already in use by another account.");
                        return View(model);
                    }

                    user.Email = model.Email;
                    user.IsEmailVerified = false; // Require verification for new email
                    Console.WriteLine($"Email updated to: {user.Email}");

                    // TODO: Send verification email to the new address
                    // SendVerificationCodeInternal(model.Email);
                }

                // Update password if provided
                if (!string.IsNullOrEmpty(model.CurrentPassword) && !string.IsNullOrEmpty(model.NewPassword))
                {
                    // Verify current password
                    string hashedCurrentPassword = ComputeSha256Hash(model.CurrentPassword);
                    if (user.PasswordHash != hashedCurrentPassword)
                    {
                        ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                        return View(model);
                    }

                    // Update with new password
                    user.PasswordHash = ComputeSha256Hash(model.NewPassword);
                    Console.WriteLine("Password has been updated");
                }

                // Save changes
                user.UpdatedAt = DateTime.Now;
                Console.WriteLine("About to save changes...");

                try
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine("Changes saved successfully");
                }
                catch (DbUpdateException dbEx)
                {
                    Console.WriteLine($"Database update error: {dbEx.Message}");
                    if (dbEx.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {dbEx.InnerException.Message}");
                    }
                    ModelState.AddModelError("", "A database error occurred while saving your profile. Please try again.");
                    return View(model);
                }

                // Update session data
                HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
                HttpContext.Session.SetString("UserEmail", user.Email);

                // Add success message to temp data so it persists through the redirect
                TempData["SuccessMessage"] = "Your profile has been updated successfully!";

                // Redirect to profile page
                return RedirectToAction("MyProfilePage");
            }
            catch (Exception ex)
            {
                // Detailed error logging
                Console.Error.WriteLine($"Error updating profile: {ex.Message}");
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                // Add error message to model state
                ModelState.AddModelError("", "An error occurred while updating your profile. Please try again.");
                return View(model);
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

                // Create a notification for admins that a new verification is pending

                return Json(new { success = true, message = "ID verification submitted successfully. Our team will review your documents." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while processing your verification: " + ex.Message });
            }
        }

        // Add simple routes for views
        [UserAuthenticationFilter]
        public IActionResult User_Listing_Details() => View();

        [UserAuthenticationFilter]
        public IActionResult User_MainPage() => View();

        [UserAuthenticationFilter]
        public IActionResult MyBookmarks() => View();

        [UserAuthenticationFilter]
        public IActionResult LiveChat() => View();

        [UserAuthenticationFilter]
        public IActionResult OtherUserProfile() => View();

        [UserAuthenticationFilter]
        public IActionResult CreateListing() => View();

        [UserAuthenticationFilter]
        public IActionResult MyShoppingCart() => View();

        [UserAuthenticationFilter]
        public IActionResult PaymentPage() => View();

        [UserAuthenticationFilter]
        public IActionResult MyProfitLossReport() => View();

        [UserAuthenticationFilter]
        public IActionResult AllMessages() => View();

        [UserAuthenticationFilter]
        public IActionResult AllNotifications() => View();

        [UserAuthenticationFilter]
        public IActionResult MyListing() => View();
    }

}