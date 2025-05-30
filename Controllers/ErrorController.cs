using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Models;
using PrimeMarket.Models.Enum;
using PrimeMarket.Models.ViewModel;

namespace PrimeMarket.Controllers
{
    public class ErrorController : Controller
    {
        [Route("404")]
        public IActionResult NotFoundPage()
        {
            // Check if a user is logged in by verifying session UserId
            bool isUserLoggedIn = HttpContext.Session.GetInt32("UserId") != null;
            // Pass login status to the view (e.g., to conditionally show header options)
            ViewBag.IsUserLoggedIn = isUserLoggedIn;
            // Return custom 404 view
            return View("404");
        }
    }
}