using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Models;
using System;
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

            // 🔴 Plain-text comparison (NOT recommended for production)
            if (admin == null || admin.Password != model.Password)
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
    }

    public class AdminLoginModel
    {
        
        public string Username { get; set; }
        public string Password { get; set; }
    }
}