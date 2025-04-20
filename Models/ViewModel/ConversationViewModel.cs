namespace PrimeMarket.Models.ViewModel
{
    public class ConversationViewModel
    {
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; }
        public string OtherUserProfileImage { get; set; }
        public int ListingId { get; set; }
        public string ListingTitle { get; set; }
        public string ListingImage { get; set; }
        public DateTime LastMessageTime { get; set; }
        public string LastMessageContent { get; set; }
        public int UnreadCount { get; set; }
    }
}
