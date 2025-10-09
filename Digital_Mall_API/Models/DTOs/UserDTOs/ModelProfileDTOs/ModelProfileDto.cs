namespace Digital_Mall_API.Models.DTOs.UserDTOs.ModelProfileDTOs
{
    public class ModelProfileDto
    {
        public string ModelId { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }
        public string ImageUrl { get; set; }
        public int FollowersCount { get; set; }
        public int ReelsCount { get; set; }
        public int TotalLikes { get; set; }
        public bool IsFollowing { get; set; }
        public ModelSocialMediaDto SocialMedia { get; set; }
    }
}
