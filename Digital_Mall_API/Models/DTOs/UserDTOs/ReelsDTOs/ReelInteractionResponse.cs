namespace Digital_Mall_API.Models.DTOs.UserDTOs.ReelsDTOs
{
    public class ReelInteractionResponse
    {
        public int ReelId { get; set; }
        public int LikesCount { get; set; }
        public int SharesCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
    }
}
