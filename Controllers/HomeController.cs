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
        // Retrieve all active listings (not archived, and in stock if applicable)
        var listings = _context.Listings
            .Where(l =>
                l.Status == ListingStatus.Active &&          // Only active listings      
                l.Status != ListingStatus.Archived &&        // Exclude archived    
                (!l.Stock.HasValue || l.Stock > 0))          // Exclude out-of-stock if stock tracking exists    
                   .Include(l => l.Images)                   // Include images for display
            .OrderByDescending(l => l.CreatedAt)             // Newest listings first
            .ToList();

        // Attach average ratings to listings using ViewBag
        ViewBag.ListingRatings = GetListingRatings(listings.Select(l => l.Id).ToList());

        return View(listings);
    }

    private Dictionary<int, double> GetListingRatings(List<int> listingIds)
    {
        // Calculate average ratings for given listing IDs
        var ratings = _context.ProductReviews
            .Where(r => listingIds.Contains(r.ListingId))
            .GroupBy(r => r.ListingId)
            .Select(g => new
            {
                ListingId = g.Key,
                AverageRating = Math.Round(g.Average(r => r.Rating), 1) // Round to 1 decimal place
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
    public IActionResult Search_Page(string query = null)
    {
        // Store query in ViewBag for optional display in the view
        ViewBag.SearchTerm = query;

        if (!string.IsNullOrEmpty(query))
        {
            // Redirect to main search handler in ListingController
            return RedirectToAction("Search", "Listing", new { query });
        }
        // If query is empty, return empty result view
        return View(new List<Listing>());
    }
}
