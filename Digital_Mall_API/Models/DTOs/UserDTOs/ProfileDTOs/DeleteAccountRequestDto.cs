using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.UserDTOs.ProfileDTOs
{
    // Add to your DTOs namespace
    public class DeleteAccountRequestDto
    {
        [Required]
        public string Password { get; set; }

        [Required]
        public bool ConfirmDeletion { get; set; }
    }
}
