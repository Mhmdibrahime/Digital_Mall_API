using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.UserDTOs.ProfileDTOs
{
    public class FollowModelRequestDto
    {
        [Required]
        public string ModelId { get; set; }
    }
}
