namespace PrimeMarket.Models.ViewModel
{
    public class UserReportViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime SignUpDate { get; set; }
        public int TotalListings { get; set; }
        public int TotalSales { get; set; }
        public int TotalPurchases { get; set; }
        public DateTime LastActivity { get; set; }

        public int DaysSinceLastActive => (DateTime.Now - LastActivity).Days;
    }
}
