namespace Digital_Mall_API.Models.DTOs.UserDTOs.BrandProfileDTOs
{
    public class FollowBrandResponseDto
    {
        public string Message { get; set; }
        public int FollowersCount { get; set; }
        public bool IsFollowing { get; set; }
    }
}
