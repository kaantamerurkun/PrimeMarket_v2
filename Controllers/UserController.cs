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
        public IActionResult VerifyEmailCode([FromForm] string email, [FromForm] string code)
        {
            var verification = _context.EmailVerifications
                .FirstOrDefault(v => v.Email == email && v.Code == code);

            if (verification == null || verification.Expiration < DateTime.UtcNow)
                return BadRequest("Invalid or expired verification code.");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return NotFound("User not found.");

            user.IsEmailVerified = true;
            _context.EmailVerifications.Remove(verification);
            _context.SaveChanges();

            return Ok(new { redirectUrl = Url.Action("Login", "User") });
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
        public IActionResult User_Listing_Details() => View();
        [UserAuthenticationFilter]
        public IActionResult User_MainPage() => View();
        [UserAuthenticationFilter]
        public IActionResult MyProfilePage() => View();
        [UserAuthenticationFilter]
        public IActionResult MyBookmarks() => View();
        [UserAuthenticationFilter]
        public IActionResult EditProfile() => View();
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

    public class SignUpViewModel
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; }

        [Required, MaxLength(100), EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public bool AgreeToTerms { get; set; }
    }
}