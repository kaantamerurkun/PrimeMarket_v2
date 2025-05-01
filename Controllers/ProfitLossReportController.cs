using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Filters;
using PrimeMarket.Models;
using PrimeMarket.Models.Enum;
using PrimeMarket.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrimeMarket.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> MyProfitLossReport(string timeRange = "all")
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Get all completed sales for this user
            var sales = await _context.Purchases
                .Include(p => p.Listing)
                .Include(p => p.Buyer)
                .Where(p => p.Listing.SellerId == userId && p.PaymentStatus == PaymentStatus.Completed)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Calculate date range based on selected timeRange
            DateTime startDate = DateTime.MinValue;
            string periodLabel = "All Time";

            switch (timeRange)
            {
                case "week":
                    startDate = DateTime.UtcNow.AddDays(-7);
                    periodLabel = "Last 7 Days";
                    break;
                case "month":
                    startDate = DateTime.UtcNow.AddMonths(-1);
                    periodLabel = "Last 30 Days";
                    break;
                case "quarter":
                    startDate = DateTime.UtcNow.AddMonths(-3);
                    periodLabel = "Last 3 Months";
                    break;
                case "year":
                    startDate = DateTime.UtcNow.AddYears(-1);
                    periodLabel = "Last 12 Months";
                    break;
                default:
                    timeRange = "all"; // Reset to 'all' if an invalid value is provided
                    break;
            }

            // Filter sales by date range if needed
            var filteredSales = timeRange == "all"
                ? sales
                : sales.Where(s => s.CreatedAt >= startDate).ToList();

            // Calculate sales statistics
            var totalRevenue = filteredSales.Sum(s => s.Amount);
            var totalItems = filteredSales.Count;
            var averagePrice = totalItems > 0 ? totalRevenue / totalItems : 0;

            // Get the top selling categories
            var topCategories = filteredSales
                .GroupBy(s => s.Listing.Category)
                .Select(g => new CategoryStatViewModel
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(s => s.Amount),
                    Percentage = g.Count() * 100.0 / (totalItems == 0 ? 1 : totalItems)
                })
                .OrderByDescending(c => c.Count)
                .Take(5)
                .ToList();

            // Prepare monthly revenue data for the chart
            var revenueByMonth = new Dictionary<string, decimal>();

            // Determine the appropriate start date for chart data based on timeRange
            DateTime chartStartDate;
            int dataPoints;
            string timeFormat;

            switch (timeRange)
            {
                case "week":
                    chartStartDate = DateTime.UtcNow.AddDays(-7);
                    dataPoints = 7;
                    timeFormat = "MM/dd"; // Month/Day
                    break;
                case "month":
                    chartStartDate = DateTime.UtcNow.AddDays(-30);
                    dataPoints = 30;
                    timeFormat = "MM/dd"; // Month/Day
                    break;
                case "quarter":
                    chartStartDate = DateTime.UtcNow.AddMonths(-3);
                    dataPoints = 12; // Weekly data points
                    timeFormat = "MM/dd"; // Month/Day
                    break;
                case "year":
                    chartStartDate = DateTime.UtcNow.AddYears(-1);
                    dataPoints = 12; // Monthly data points
                    timeFormat = "MMM yyyy"; // Month Year
                    break;
                default:
                    // For 'all', show up to the last 24 months of data
                    chartStartDate = sales.Any()
                        ? sales.Min(s => s.CreatedAt ?? DateTime.UtcNow.AddYears(-2))
                        : DateTime.UtcNow.AddYears(-2);
                    dataPoints = 24; // Monthly data points
                    timeFormat = "MMM yyyy"; // Month Year
                    break;
            }

            // Generate the data points for the chart
            var chartData = new List<ChartDataPoint>();

            if (timeRange == "week" || timeRange == "month")
            {
                // Daily data points
                for (int i = 0; i < dataPoints; i++)
                {
                    var date = chartStartDate.AddDays(i);
                    var dayRevenue = filteredSales
                        .Where(s => s.CreatedAt?.Date == date.Date)
                        .Sum(s => s.Amount);

                    chartData.Add(new ChartDataPoint
                    {
                        Label = date.ToString(timeFormat),
                        Value = dayRevenue
                    });
                }
            }
            else if (timeRange == "quarter")
            {
                // Weekly data points
                for (int i = 0; i < dataPoints; i++)
                {
                    var weekStart = chartStartDate.AddDays(i * 7);
                    var weekEnd = weekStart.AddDays(6);
                    var weekRevenue = filteredSales
                        .Where(s => s.CreatedAt >= weekStart && s.CreatedAt <= weekEnd)
                        .Sum(s => s.Amount);

                    chartData.Add(new ChartDataPoint
                    {
                        Label = $"{weekStart.ToString("MM/dd")}-{weekEnd.ToString("MM/dd")}",
                        Value = weekRevenue
                    });
                }
            }
            else
            {
                // Monthly data points
                for (int i = 0; i < dataPoints; i++)
                {
                    var monthStart = timeRange == "year"
                        ? chartStartDate.AddMonths(i)
                        : DateTime.UtcNow.AddMonths(-dataPoints + i + 1);

                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                    var monthRevenue = filteredSales
                        .Where(s => s.CreatedAt >= monthStart && s.CreatedAt <= monthEnd)
                        .Sum(s => s.Amount);

                    chartData.Add(new ChartDataPoint
                    {
                        Label = monthStart.ToString("MMM yyyy"),
                        Value = monthRevenue
                    });
                }
            }

            // Create the view model
            var viewModel = new ProfitLossReportViewModel
            {
                TimeRange = timeRange,
                PeriodLabel = periodLabel,
                TotalSales = totalItems,
                TotalRevenue = totalRevenue,
                AveragePrice = averagePrice,
                TopCategories = topCategories,
                RecentSales = filteredSales.Take(10).ToList(),
                ChartData = chartData
            };

            return View(viewModel);
        }
    }
}