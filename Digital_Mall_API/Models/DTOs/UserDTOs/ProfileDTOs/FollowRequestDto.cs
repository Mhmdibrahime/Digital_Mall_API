using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.UserDTOs.ProfileDTOs
{
    public class FollowRequestDto
    {
        [Required]
        public string BrandId { get; set; }
    }
}
