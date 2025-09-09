using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.TShirtDesignersManagementDTOs
{
    public class UpdateStatusRequest
    {
        [Required]
        public string Status { get; set; }
    }
}
