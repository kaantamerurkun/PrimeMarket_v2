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
        private readonly ILogger<ReportController> _logger;

        public ReportController(ApplicationDbContext context, ILogger<ReportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Modify the MyProfitLossReport method in ReportController.cs
        // Modify the MyProfitLossReport method in ReportController.cs
        [HttpGet]
        [UserAuthenticationFilter]
        public async Task<IActionResult> MyProfitLossReport(string timeRange = "all")
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            try
            {
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

                // Calculate sales statistics - UPDATED to use Quantity field
                var totalRevenue = filteredSales.Sum(s => s.Amount);
                var totalItems = filteredSales.Sum(s => s.Quantity); // Sum quantities instead of counting sales
                var averagePrice = totalItems > 0 ? totalRevenue / totalItems : 0; // Calculate average per item, not per order

                // Get the top selling categories
                var topCategories = filteredSales
                    .GroupBy(s => s.Listing.Category)
                    .Select(g => new CategoryStatViewModel
                    {
                        Category = g.Key,
                        Count = g.Sum(s => s.Quantity), // Sum quantities per category
                        Revenue = g.Sum(s => s.Amount),
                        Percentage = g.Sum(s => s.Quantity) * 100.0 / (totalItems == 0 ? 1 : totalItems) // Calculate percentage based on quantities
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

                // Ensure we have at least one data point even if there are no sales
                if (!filteredSales.Any())
                {
                    // Add a single data point with zero value for current date
                    chartData.Add(new ChartDataPoint
                    {
                        Label = DateTime.UtcNow.ToString(timeFormat),
                        Value = 0
                    });
                }
                else if (timeRange == "week" || timeRange == "month")
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
                    // For year and all time, use monthly data points

                    // If we have very few months of data but "all time" is selected,
                    // make sure we at least have 2 data points
                    if (timeRange == "all" && filteredSales.Count > 0)
                    {
                        // Calculate the actual date range in months
                        var minDate = filteredSales.Min(s => s.CreatedAt ?? DateTime.UtcNow);
                        var maxDate = filteredSales.Max(s => s.CreatedAt ?? DateTime.UtcNow);
                        var monthsSpan = ((maxDate.Year - minDate.Year) * 12) + maxDate.Month - minDate.Month;

                        // If we have less than 2 months of data, adjust to show at least 2 data points
                        if (monthsSpan < 2)
                        {
                            // Start date should be 1 month before the earliest sale
                            chartStartDate = minDate.AddMonths(-1);
                            // Make sure we show at least 2 months
                            dataPoints = Math.Max(2, monthsSpan + 1);
                        }
                    }

                    // Monthly data points for year and all time
                    for (int i = 0; i < dataPoints; i++)
                    {
                        var monthStart = timeRange == "year"
                            ? chartStartDate.AddMonths(i)
                            : DateTime.UtcNow.AddMonths(-dataPoints + i + 1);

                        // For "all time" with very few data points, use the adjusted range
                        if (timeRange == "all" && dataPoints <= 2)
                        {
                            monthStart = chartStartDate.AddMonths(i);
                        }

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

                // If we still somehow ended up with no data points, add a zero point for the current month
                if (!chartData.Any())
                {
                    chartData.Add(new ChartDataPoint
                    {
                        Label = DateTime.UtcNow.ToString(timeFormat),
                        Value = 0
                    });
                }

                // Create the view model
                var viewModel = new ProfitLossReportViewModel
                {
                    TimeRange = timeRange,
                    PeriodLabel = periodLabel,
                    TotalSales = totalItems, // This now represents total quantity of items
                    TotalRevenue = totalRevenue,
                    AveragePrice = averagePrice, // This now represents the average price per item
                    TopCategories = topCategories,
                    RecentSales = filteredSales.Take(10).ToList(),
                    ChartData = chartData
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating profit/loss report");
                TempData["ErrorMessage"] = "An error occurred while generating the report. Please try again.";

                // Return a simplified error view model
                return View(new ProfitLossReportViewModel
                {
                    TimeRange = timeRange,
                    PeriodLabel = "Error Loading Data",
                    TotalSales = 0,
                    TotalRevenue = 0,
                    AveragePrice = 0,
                    TopCategories = new List<CategoryStatViewModel>(),
                    RecentSales = new List<Purchase>(),
                    ChartData = new List<ChartDataPoint>()
                });
            }
        }
    }
}