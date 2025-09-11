using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.DesignerAdminDTOs
{
    public class DesignerFullUpdateDto
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public IFormFile ProfileImage { get; set; }

        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }


}
