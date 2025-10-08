namespace Digital_Mall_API.Models.DTOs.UserDTOs.ProfileDTOs
{
    public class FollowingBrandDto
    {
        public int FollowingId { get; set; }
        public string BrandId { get; set; }
        public string BrandName { get; set; }
        public string LogoUrl { get; set; }
        public int FollowersCount { get; set; }
        public DateTime FollowedAt { get; set; }
    }
}
