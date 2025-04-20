using PrimeMarket.Controllers;

namespace PrimeMarket.Models.ViewModel
{
    public class DetailedConversationViewModel
    {
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; }
        public string OtherUserProfileImage { get; set; }
        public int ListingId { get; set; }
        public string ListingTitle { get; set; }
        public decimal ListingPrice { get; set; }
        public string ListingImage { get; set; }
        public int CurrentUserId { get; set; }
        public List<MessageViewModel> Messages { get; set; }
    }
}
