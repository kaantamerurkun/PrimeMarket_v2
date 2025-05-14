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

    public IActionResult Index()
    {
        var listings = _context.Listings
            .Where(l =>
                l.Status == ListingStatus.Active &&                // still for sale
                (!l.Stock.HasValue || l.Stock > 0))                // second-hand OR stock-positive
                   .Include(l => l.Images)
            .OrderByDescending(l => l.CreatedAt)
            .ToList();

        // Calculate average ratings for all listings
        ViewBag.ListingRatings = GetListingRatings(listings.Select(l => l.Id).ToList());

        return View(listings);
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
