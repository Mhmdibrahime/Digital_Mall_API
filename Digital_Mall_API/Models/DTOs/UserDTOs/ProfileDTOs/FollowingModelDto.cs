namespace Digital_Mall_API.Models.DTOs.UserDTOs.ProfileDTOs
{
    public class FollowingModelDto
    {
        public int FollowingId { get; set; }
        public string ModelId { get; set; }
        public string ModelName { get; set; }
        public string ImageUrl { get; set; }
        public int FollowersCount { get; set; }
        public DateTime FollowedAt { get; set; }
    }
}
