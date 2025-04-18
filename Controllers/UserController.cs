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

        // Other user views
        public IActionResult ResetPassword() => View();
        public IActionResult User_Listing_Details() => View();
        public IActionResult User_MainPage() => View();
        public IActionResult MyProfilePage() => View();
        public IActionResult MyBookmarks() => View();
        public IActionResult EditProfile() => View();
        public IActionResult LiveChat() => View();
        public IActionResult OtherUserProfile() => View();
        public IActionResult CreateListing() => View();
        public IActionResult MyShoppingCart() => View();
        public IActionResult PaymentPage() => View();
        public IActionResult MyProfitLossReport() => View();
        public IActionResult AllMessages() => View();
        public IActionResult AllNotifications() => View();
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