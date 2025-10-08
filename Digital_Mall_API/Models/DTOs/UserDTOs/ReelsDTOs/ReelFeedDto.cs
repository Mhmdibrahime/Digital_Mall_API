using Digital_Mall_API.Controllers.Reels;

namespace Digital_Mall_API.Models.DTOs.UserDTOs.ReelsDTOs
{
    public class ReelFeedDto
    {
        public int Id { get; set; }
        public string? Caption { get; set; }
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public DateTime PostedDate { get; set; }
        public int DurationInSeconds { get; set; }
        public int LikesCount { get; set; }
        public int SharesCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public string PostedByUserType { get; set; }
        public string PostedByName { get; set; }
        public string? PostedByImage { get; set; }
        public List<ReelProductDto> LinkedProducts { get; set; } = new();
    }
}
