using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Models;
using PrimeMarket.Models.Enum;
using PrimeMarket.Models.ViewModel;

namespace PrimeMarket.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context,ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var approvedListings = await _context.Listings
            .Where(l => l.Status == ListingStatus.Approved)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return View(approvedListings);
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    public IActionResult Guest_Listing_Details()
    {
        return View();
    }
    // Add or update this method in your HomeController.cs file
    public IActionResult Search_Page(string query = null)
    {
        ViewBag.SearchTerm = query;

        // If there's a query, redirect to the listing search action
        if (!string.IsNullOrEmpty(query))
        {
            return RedirectToAction("Search", "Listing", new { query });
        }

        // If no query, return an empty list to display "no results" message
        return View(new List<Listing>());
    }
}
