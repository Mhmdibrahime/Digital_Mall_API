namespace Digital_Mall_API.Models.DTOs.UserDTOs.BrandProfileDTOs
{
    public class BrandProfileDto
    {
        public string BrandId { get; set; }
        public string BrandName { get; set; }
        public string Description { get; set; }
        public string LogoUrl { get; set; }
        public string Location { get; set; }
        public int FollowersCount { get; set; }
        public int ProductsCount { get; set; }
        public int ReelsCount { get; set; }
        public int TotalLikes { get; set; }
        public bool IsFollowing { get; set; }
        public BrandSocialMediaDto SocialMedia { get; set; }
    }
}
