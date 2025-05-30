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
                if (timeRange != "week" && timeRange != "month" && timeRange != "quarter" && timeRange != "year" && timeRange != "all")
                {
                    timeRange = "all";
                }

                var sales = await _context.Purchases
                    .Include(p => p.Listing)
                    .Include(p => p.Buyer)
                    .Where(p => p.Listing.SellerId == userId && p.PaymentStatus == PaymentStatus.Completed)
                    .OrderByDescending(p => p.CreatedAt)
                    .AsNoTracking() 
                    .ToListAsync();

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
                        timeRange = "all"; 
                        break;
                }

                var filteredSales = timeRange == "all"
                    ? sales
                    : sales.Where(s => s.CreatedAt >= startDate).ToList();

                var totalRevenue = filteredSales.Sum(s => s.Amount);
                var totalItems = filteredSales.Sum(s => s.Quantity); 
                var averagePrice = totalItems > 0 ? totalRevenue / totalItems : 0; 

                var topCategories = filteredSales
                    .GroupBy(s => s.Listing.Category)
                    .Select(g => new CategoryStatViewModel
                    {
                        Category = g.Key,
                        Count = g.Sum(s => s.Quantity), 
                        Revenue = g.Sum(s => s.Amount),
                        Percentage = g.Sum(s => s.Quantity) * 100.0 / (totalItems == 0 ? 1 : totalItems) 
                    })
                    .OrderByDescending(c => c.Count)
                    .Take(5)
                    .ToList();

                var chartData = new List<ChartDataPoint>();

                DateTime chartStartDate;
                int dataPoints;
                string timeFormat;

                switch (timeRange)
                {
                    case "week":
                        chartStartDate = DateTime.UtcNow.AddDays(-7);
                        dataPoints = 7;
                        timeFormat = "MM/dd"; 

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
                        break;

                    case "month":
                        chartStartDate = DateTime.UtcNow.AddDays(-30);
                        dataPoints = 30;
                        timeFormat = "MM/dd"; 

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
                        break;

                    case "quarter":
                        chartStartDate = DateTime.UtcNow.AddMonths(-3);
                        dataPoints = 12;
                        timeFormat = "MM/dd"; 

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
                        break;

                    case "year":
                        chartStartDate = DateTime.UtcNow.AddYears(-1);
                        dataPoints = 12; 
                        timeFormat = "MMM yyyy";

                        for (int i = 0; i < dataPoints; i++)
                        {
                            var monthStart = DateTime.UtcNow.AddMonths(-12 + i);
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
                        break;

                    default: 
                        chartStartDate = sales.Any()
                            ? sales.Min(s => s.CreatedAt ?? DateTime.UtcNow.AddYears(-2))
                            : DateTime.UtcNow.AddYears(-2);
                        dataPoints = 24; 
                        timeFormat = "MMM yyyy"; 

                        var minDate = sales.Any()
                            ? sales.Min(s => s.CreatedAt ?? DateTime.UtcNow)
                            : DateTime.UtcNow.AddYears(-2);
                        var maxDate = sales.Any()
                            ? sales.Max(s => s.CreatedAt ?? DateTime.UtcNow)
                            : DateTime.UtcNow;
                        var monthsSpan = ((maxDate.Year - minDate.Year) * 12) + maxDate.Month - minDate.Month;

                        if (monthsSpan < 2)
                        {
                            chartStartDate = minDate.AddMonths(-1);
                            dataPoints = Math.Max(2, monthsSpan + 1);
                        }

                        for (int i = 0; i < dataPoints; i++)
                        {
                            var monthStart = DateTime.UtcNow.AddMonths(-dataPoints + i + 1);

                            if (dataPoints <= 2)
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
                        break;
                }

                if (!chartData.Any())
                {
                    chartData.Add(new ChartDataPoint
                    {
                        Label = DateTime.UtcNow.ToString(timeFormat),
                        Value = 0
                    });
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating profit/loss report");
                TempData["ErrorMessage"] = "An error occurred while generating the report. Please try again.";

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