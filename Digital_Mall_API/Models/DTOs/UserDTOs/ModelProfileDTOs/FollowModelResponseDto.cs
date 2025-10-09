namespace Digital_Mall_API.Models.DTOs.UserDTOs.ModelProfileDTOs
{
    public class FollowModelResponseDto
    {
        public string Message { get; set; }
        public int FollowersCount { get; set; }
        public bool IsFollowing { get; set; }
    }
}
