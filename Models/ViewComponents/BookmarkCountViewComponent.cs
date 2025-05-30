using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using System.Threading.Tasks;

namespace PrimeMarket.Models.ViewComponents
{
    public class BookmarkCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public BookmarkCountViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int listingId)
        {
            var count = await _context.Bookmarks.CountAsync(b => b.ListingId == listingId);
            return View(count);
        }
    }
}