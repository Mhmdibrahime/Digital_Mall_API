namespace Digital_Mall_API.Models.DTOs.UserDTOs
{
    public class RandomReelFeedDto
    {
        public int Id { get; set; }
        public string Caption { get; set; }
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public DateTime PostedDate { get; set; }
        public int DurationInSeconds { get; set; }
        public int LikesCount { get; set; }
        public int SharesCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public string PostedByUserType { get; set; }
        public string PostedByUserId { get; set; }
        public string PostedByName { get; set; }
        public string PostedByImage { get; set; }
    }
}
