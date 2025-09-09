using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.TShirtDesignersManagementDTOs
{
    public class AddDesignerRequest
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        public string PhoneNumber { get; set; }
        public string Status { get; set; } = "Active";
    }
}
