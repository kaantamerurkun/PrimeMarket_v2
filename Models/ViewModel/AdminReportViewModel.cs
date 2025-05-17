namespace PrimeMarket.Models.ViewModel
{
    public class AdminReportViewModel
    {
        public List<UserReportViewModel> Users { get; set; }
        public int TotalUsers { get; set; }
        public int TotalListings { get; set; }
        public int PendingListings { get; set; }
        public int ApprovedListings { get; set; }
        public int SoldListings { get; set; }
        public int ArchivedListings { get; set; }
        public List<CategoryStatViewModel> CategoryStats { get; set; }
    }
}
