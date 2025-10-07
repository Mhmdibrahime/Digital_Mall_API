namespace Digital_Mall_API.Models.DTOs.UserDTOs.ProfileDTOs
{
    public class ProfileDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string JoiningDate { get; set; }
        public int FollowingBrandsCount { get; set; }
        public int FollowingModelsCount { get; set; }
        public int OrdersCount { get; set; }
    }
}
