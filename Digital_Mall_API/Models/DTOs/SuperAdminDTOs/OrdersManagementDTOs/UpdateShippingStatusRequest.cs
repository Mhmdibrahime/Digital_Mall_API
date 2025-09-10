using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.OrdersManagementDTOs
{
    public class UpdateShippingStatusRequest
    {
        [Required]
        public string Status { get; set; }

    }
}
