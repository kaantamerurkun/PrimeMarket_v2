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
            return View("404"); // Views/Error/404.cshtml dosyasını döner
        }
    }
}