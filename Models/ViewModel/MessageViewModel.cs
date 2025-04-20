namespace PrimeMarket.Models.ViewModel
{
    public class MessageViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public bool SentByMe { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
