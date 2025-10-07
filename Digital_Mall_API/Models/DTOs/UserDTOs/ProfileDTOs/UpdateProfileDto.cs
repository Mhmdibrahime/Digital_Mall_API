using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.UserDTOs.ProfileDTOs
{
    public class UpdateProfileDto
    {
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [EmailAddress]
        public string Email { get; set; }
    }
}
