using PrimeMarket.Models;
using PrimeMarket.Models.Products;
using System;
using System.Collections.Generic;

namespace PrimeMarket.Models.ViewModel
{
    public class ProfitLossReportViewModel
    {
        public string TimeRange { get; set; } = "all";
        public string PeriodLabel { get; set; } = "All Time";
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AveragePrice { get; set; }
        public List<CategoryStatViewModel> TopCategories { get; set; }
        public List<Purchase> RecentSales { get; set; }
        public List<ChartDataPoint> ChartData { get; set; }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
    }
}