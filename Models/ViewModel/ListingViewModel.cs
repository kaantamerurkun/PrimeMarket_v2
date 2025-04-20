namespace PrimeMarket.Models.ViewModel
{
    public class ListingViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string Condition { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string DetailCategory { get; set; }
        public string Location { get; set; }
        public Dictionary<string, string> DynamicProperties { get; set; }
        public List<ListingImage> Images { get; set; }
    }
}
