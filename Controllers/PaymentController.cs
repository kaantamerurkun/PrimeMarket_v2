using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Filters;
using PrimeMarket.Models;
using PrimeMarket.Models.Enum;
using PrimeMarket.Models.Products;
using PrimeMarket.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrimeMarket.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> Checkout(int listingId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var listing = await _context.Listings
                .Include(l => l.Images)
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null)
            {
                return NotFound();
            }

            if (listing.Status != ListingStatus.Approved)
            {
                TempData["ErrorMessage"] = "This listing is not available for purchase.";
                return RedirectToAction("Details", "Listing", new { id = listingId });
            }

            if (listing.SellerId == userId)
            {
                TempData["ErrorMessage"] = "You cannot purchase your own listing.";
                return RedirectToAction("Details", "Listing", new { id = listingId });
            }

            var checkoutViewModel = new CheckoutViewModel
            {
                ListingId = listing.Id,
                ListingTitle = listing.Title,
                ListingPrice = listing.Price,
                SellerName = $"{listing.Seller.FirstName} {listing.Seller.LastName}",
                ListingImage = listing.Images.FirstOrDefault(i => i.IsMainImage)?.ImagePath ??
                              listing.Images.FirstOrDefault()?.ImagePath ?? "/images/placeholder.png"
            };

            return View(checkoutViewModel);
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> Cart()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)